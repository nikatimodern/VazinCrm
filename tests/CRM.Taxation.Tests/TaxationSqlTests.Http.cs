using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoiceBatch;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed partial class TaxationSqlTests
{
    [TaxationSqlFact]
    public async Task BrowserRoutesPersistMockInvoiceReferenceAndPaymentAcrossHostRestart()
    {
        await using var db = Open();
        var (factor, _) = await SeedAsync(db, FactorStatus.تایید_شده);
        var second = await AddCandidateAsync(db, factor, 1, null);
        using var mock = new TaxationMockAuthority();
        var keyRing = Directory.CreateTempSubdirectory("vazin-tax-http-keys-");
        try
        {
            var cancellation = await SendThroughBrowserRoutesAsync(keyRing, mock, factor, second);
            await using var restarted = await TaxationHttpTestHost.StartAsync(options, keyRing, mock, Ct);
            using var browser = TaxationHttpTestHost.Connect(restarted);
            var page = await GetPageAsync(browser, $"/Taxation/dispatches/{cancellation:D}");
            Assert.Contains("نتیجه ارسال نامشخص", page);
            var recovered = await PostAsync<TaxDispatchState>(browser, $"/api/taxation/dispatches/{cancellation:D}/inquire", null);
            Assert.Equal(TaxDispatchStatus.Accepted, recovered.Status);
            Assert.Equal(2, mock.InvoicePosts);
            Assert.Contains("پذیرفته‌شده", await GetPageAsync(browser, $"/Taxation/dispatches/{cancellation:D}"));
            var stored = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == cancellation, Ct);
            Assert.Equal(TaxDispatchStatus.Accepted, stored.Status);
            Assert.Equal(recovered.AttemptId, stored.AttemptId);
        }
        finally
        {
            keyRing.Delete(recursive: true);
        }
    }

    private async Task<Guid> SendThroughBrowserRoutesAsync(DirectoryInfo keyRing, TaxationMockAuthority mock,
        Guid factor, Guid second)
    {
        using var audit = new TaxationAuditCapture();
        await using var app = await TaxationHttpTestHost.StartAsync(options, keyRing, mock, Ct, audit);
        using var browser = TaxationHttpTestHost.Connect(app);
        using var unauthorized = TaxationHttpTestHost.Connect(app, "sales");
        using var forbidden = await unauthorized.GetAsync("/Taxation", Ct);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var missingToken = await browser.PostAsJsonAsync("/api/taxation/invoices/batch/queue", new[] { factor }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);

        var list = await GetPageAsync(browser, "/Taxation");
        Assert.Contains("taxationBatch", list);
        var previewPage = await GetPageAsync(browser, $"/Taxation/invoices/{factor:D}/preview");
        Assert.Contains("id=\"taxationPreview\"", previewPage);
        var preview = await browser.GetFromJsonAsync<InvoicePreview>($"/api/taxation/invoices/{factor:D}/preview", Ct);
        Assert.Empty(preview.Errors);
        var body = new { version = preview.Version };
        var snapshot = await PostAsync<Guid>(browser, $"/api/taxation/invoices/{factor:D}/snapshots", body);
        Assert.NotEqual(Guid.Empty, snapshot);
        using var forbiddenSnapshot = await unauthorized.GetAsync($"/api/taxation/snapshots/{snapshot:D}", Ct);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenSnapshot.StatusCode);
        var queued = await PostAsync<Guid>(browser, $"/api/taxation/invoices/{factor:D}/queue", body);
        var batch = await PostAsync<TaxInvoiceBatchResult[]>(browser, "/api/taxation/invoices/batch/queue", new[] { factor, second });
        Assert.Equal(queued, batch[0].DispatchId);
        Assert.NotNull(batch[1].DispatchId);

        var api = $"/api/taxation/dispatches/{queued:D}";
        Assert.Contains("ارسال دستی به Sandbox", await GetPageAsync(browser, $"/Taxation/dispatches/{queued:D}"));
        var submitted = await PostAsync<TaxDispatchState>(browser, api + "/submit", Filing());
        Assert.Equal(TaxDispatchStatus.Submitted, submitted.Status);
        Assert.NotNull(submitted.ReferenceNumber);
        var accepted = await PostAsync<TaxDispatchState>(browser, api + "/inquire", null);
        Assert.Equal(TaxDispatchStatus.Accepted, accepted.Status);
        await PostAsync<object>(browser, api + "/work-folder", null);
        var acceptedPage = await GetPageAsync(browser, $"/Taxation/dispatches/{queued:D}");
        Assert.Contains(submitted.ReferenceNumber.Value.ToString("D"), acceptedPage);
        Assert.Contains("AWAITING_REACTION", acceptedPage);
        Assert.Contains("پذیرفته‌شده", acceptedPage);

        var receiptId = await AddCashReceiptAsync(factor);
        var payment = await PostAsync<Guid>(browser, api + "/payments",
            new TaxPaymentDetails(receiptId, TaxReceiptMethod.Transfer, Filing().IssuedAt.AddMinutes(1), "http-1", null));
        var paymentApi = $"/api/taxation/payments/{payment:D}";
        Assert.Equal(TaxPaymentStatus.Accepted, (await PostAsync<TaxPaymentState>(browser, paymentApi + "/submit", null)).Status);
        Assert.Equal(TaxPaymentStatus.Accepted, (await browser.GetFromJsonAsync<TaxPaymentState>(paymentApi, Ct)).Status);
        Assert.Equal(1, mock.PaymentPosts);

        var cancellation = await PostAsync<Guid>(browser, api + "/references",
            new TaxReferenceRequest(null, TaxDispatchKind.Cancellation, 2, Filing().IssuedAt, null, false,
                TaxCancellationReason.IncorrectTransactionDate));
        var cancelApi = $"/api/taxation/dispatches/{cancellation:D}";
        Assert.Equal(TaxDispatchStatus.Unknown, (await PostAsync<TaxDispatchState>(browser, cancelApi + "/submit", null)).Status);
        Assert.Equal(TaxDispatchStatus.Unknown, (await PostAsync<TaxDispatchState>(browser, cancelApi + "/submit", null)).Status);
        Assert.Equal(2, mock.InvoicePosts);
        Assert.Contains("داشبورد سامانه مؤدیان", await GetPageAsync(browser, "/Taxation/reports?scope=Sandbox"));
        Assert.Contains(audit.Records, message => message.Contains($"Taxation.Submit actor admin target {queued:D} HTTP 200"));
        Assert.Contains(audit.Records, message => message.Contains($"Taxation.EnqueuePayment actor admin target {queued:D} HTTP 200"));
        Assert.DoesNotContain(audit.Records, message => message.Contains(preview.Version) || message.Contains("http-1") ||
            message.Contains(preview.BuyerEconomicCode) || message.Contains(submitted.TaxId));
        return cancellation;
    }

    private async Task<Guid> AddCashReceiptAsync(Guid factor)
    {
        await using var db = Open();
        var receipt = new Accountancy
        {
            Id = Guid.NewGuid(), fid = factor, InvoiceType = InvoiceTypeEnum.فاکتور,
            AccType = AcEnum.واریزی, Status = AccStatusEnum.تایید_شده, PayType = PayTypeEnum.نقدی,
            CurrencyType = CurrencyTypeEnum.ریال, date = Filing().IssuedAt.Date, credit = 40
        };
        db.Accountancies.Add(receipt);
        await db.SaveChangesAsync(Ct);
        return receipt.Id;
    }

    private static async Task<string> GetPageAsync(HttpClient browser, string url)
    {
        using var response = await browser.GetAsync(url, Ct);
        var body = await response.Content.ReadAsStringAsync(Ct);
        Assert.True(response.IsSuccessStatusCode, $"{url} returned {response.StatusCode}: {body}");
        var page = WebUtility.HtmlDecode(body);
        var token = Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (token.Success)
        {
            browser.DefaultRequestHeaders.Remove("RequestVerificationToken");
            browser.DefaultRequestHeaders.Add("RequestVerificationToken", token.Groups[1].Value);
        }
        return page;
    }

    private static async Task<T> PostAsync<T>(HttpClient browser, string url, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        using var response = await browser.SendAsync(request, Ct);
        Assert.True(response.IsSuccessStatusCode, $"{url} returned {response.StatusCode}: {await response.Content.ReadAsStringAsync(Ct)}");
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: Ct);
    }

    private sealed class TaxationAuditCapture : ILoggerProvider
    {
        public ConcurrentQueue<string> Records { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(Records);
        public void Dispose() { }

        private sealed class CaptureLogger(ConcurrentQueue<string> records) : ILogger
        {
            public bool IsEnabled(LogLevel level) => level == LogLevel.Information;
            public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception exception, Func<TState, Exception, string> format)
            {
                if (id.Name == "TaxationAudit") records.Enqueue(format(state, exception));
            }
        }
    }
}
