using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Commands.SetTaxProductMapping;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.Infrastructures.Sql.Taxation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed partial class TaxationSqlTests
{
    [TaxationSqlFact]
    public async Task TaxationFollowupPagesHistoryAndGuidesFullReturnThroughCancellation()
    {
        var keyRing = Directory.CreateTempSubdirectory("vazin-tax-followup-keys-");
        try
        {
            await using var db = Open();
            var protector = new TaxPreviewProtector(DataProtectionProvider.Create(keyRing,
                builder => builder.SetApplicationName("Vazin.Taxation.HttpTests")));
            var reference = await SetupReferenceAsync(db, "APPROVED", protector);
            var candidate = await AddCandidateAsync(db, reference.FactorId, 1, reference.FactorId);
            db.TaxDispatchEvents.AddRange(Enumerable.Range(1, 55).Select(_ => new TaxDispatchEvent
            {
                DispatchId = reference.Id, AttemptId = reference.AttemptId!.Value, Kind = TaxDispatchEventKind.Processing,
                Result = "NoResult", OccurredAt = DateTimeOffset.UtcNow
            }));
            await db.SaveChangesAsync(Ct);

            var listing = new TaxInvoiceListing(db, protector);
            var latest = await listing.ReadDispatchAsync(reference.Id, Ct);
            Assert.Equal(50, latest.History.Count);
            Assert.Equal(latest.History[^1].Id, latest.OlderHistoryBefore);
            var older = await listing.ReadDispatchAsync(reference.Id, Ct, latest.OlderHistoryBefore);
            Assert.NotEmpty(older.History);
            Assert.Null(older.OlderHistoryBefore);
            Assert.All(older.History, item => Assert.True(item.Id < latest.History[^1].Id));

            using var mock = new TaxationMockAuthority();
            await using var host = await TaxationHttpTestHost.StartAsync(options, keyRing, mock, Ct);
            using var browser = TaxationHttpTestHost.Connect(host);
            var details = $"/Taxation/dispatches/{reference.Id:D}";
            var history = await GetPageAsync(browser, details + "?historyBefore=" + latest.OlderHistoryBefore);
            Assert.Contains("جدیدترین رویدادها", history);
            Assert.Contains("ثبت در صف", history);

            var api = $"/api/taxation/dispatches/{reference.Id:D}/references";
            var request = new TaxReferenceRequest(candidate, TaxDispatchKind.Return, 2,
                Filing().IssuedAt, null, false, null);
            using var blocked = await browser.PostAsJsonAsync(api, request, Ct);
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
            var guidance = await blocked.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: Ct);
            Assert.Equal("full_return_requires_cancellation", guidance.GetProperty("code").GetString());
            Assert.Single(await db.TaxDispatches.AsNoTracking().ToListAsync(Ct));

            var cancellationUrl = guidance.GetProperty("cancellationUrl").GetString();
            Assert.StartsWith(details + "?", cancellationUrl);
            Assert.Contains("ثبت سند ابطالی در صف", await GetPageAsync(browser, cancellationUrl));
            var cancellation = request with
            {
                FactorId = null, Kind = TaxDispatchKind.Cancellation, CancellationReason = TaxCancellationReason.FullReturn
            };
            using var invalidSerial = await browser.PostAsJsonAsync(api, cancellation with { Serial = 1 }, Ct);
            Assert.Equal(HttpStatusCode.BadRequest, invalidSerial.StatusCode);
            var id = await PostAsync<Guid>(browser, api, cancellation);
            var queued = await db.TaxDispatches.AsNoTracking().SingleAsync(x => x.Id == id, Ct);
            Assert.Equal(TaxDispatchKind.Cancellation, queued.Kind);
            Assert.Equal(TaxDispatchStatus.Pending, queued.Status);
            Assert.Equal(reference.Id, queued.ReferencedDispatchId);
            Assert.Equal(0, mock.InvoicePosts);

            await db.TaxProductMappings.ExecuteUpdateAsync(s => s.SetProperty(x => x.TaxUnit, "17"), Ct);
            var products = await GetPageAsync(browser, "/Taxation/products?unmapped=true");
            Assert.Contains("واحد مالیاتی هم‌ارز با واحد فروش", products);
            Assert.Contains("نفر-ماه (16134)", products);
            Assert.Contains("در فهرست رسمی نیست", products);
        }
        finally
        {
            keyRing.Delete(recursive: true);
        }
    }

    [TaxationSqlFact]
    public async Task TaxationFollowupFlagsStoredInvalidMappingsWithoutChangingThem()
    {
        await using var db = Open();
        var (factor, _) = await SeedAsync(db, CRM.Core.Domain.Factors.Enum.FactorStatus.تایید_شده);
        var mapping = await db.TaxProductMappings.AsNoTracking().SingleAsync(Ct);
        var repo = new TaxationRepository(db, protection);
        var update = new SetTaxProductMappingCommandHandler(repo);
        foreach (var code in new[] { "17", "164 ", "１６４" })
        {
            await db.TaxProductMappings.Where(x => x.Id == mapping.Id).ExecuteUpdateAsync(
                s => s.SetProperty(x => x.TaxUnit, code), Ct);
            var result = await repo.ListProductsAsync("", true, 1, 25, Ct);
            Assert.Equal(code, Assert.Single(Assert.Single(result.Items).Units).TaxUnit);
            var preview = InvoicePreview.Build(await repo.GetPreviewSourceAsync(factor, Ct));
            Assert.Contains(preview.Errors, error => error.Contains("فهرست رسمی"));
            await Assert.ThrowsAsync<ArgumentException>(() => update.HandleAsync(
                new(mapping.ProductId, mapping.Unit, mapping.GoodsId, code), Ct));
            Assert.Equal(code, await db.TaxProductMappings.Where(x => x.Id == mapping.Id)
                .Select(x => x.TaxUnit).SingleAsync(Ct));
        }

        db.ChangeTracker.Clear();
        Assert.True(await update.HandleAsync(new(mapping.ProductId, mapping.Unit, mapping.GoodsId, "164"), Ct));
        Assert.Empty((await repo.ListProductsAsync("", true, 1, 25, Ct)).Items);
        Assert.Empty(InvoicePreview.Build(await repo.GetPreviewSourceAsync(factor, Ct)).Errors);
    }
}
