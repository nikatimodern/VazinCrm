using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxPayment;

public enum TaxReceiptMethod { Cash = 1, Cheque = 2, Pos = 3, Internet = 4, Card = 5, Transfer = 6 }

public sealed record TaxPaymentDetails(Guid AccountancyId, TaxReceiptMethod Method, DateTimeOffset PaidAt,
    string ReferenceNumber, string? TerminalNumber);

public sealed record EnqueueTaxPaymentCommand(Guid InvoiceDispatchId, string ActorId, TaxPaymentDetails Details) : ICommand<Guid>;

public sealed class EnqueueTaxPaymentCommandHandler(ITaxPaymentRepository repository, TimeProvider clock)
    : ICommandHandler<EnqueueTaxPaymentCommand, Guid>
{
    public Task<Guid> HandleAsync(EnqueueTaxPaymentCommand request, CancellationToken ct) =>
        repository.EnqueueAsync(request.InvoiceDispatchId, request.Details.AccountancyId, request.ActorId,
            source => Prepare(source, request.Details, clock.GetUtcNow()), ct);

    internal static TaxPaymentRegistration Prepare(TaxPaymentSource source, TaxPaymentDetails details, DateTimeOffset now)
    {
        var receipt = source.Receipt;
        if (source.Invoice.Status != TaxDispatchStatus.Accepted || source.Invoice.Kind == TaxDispatchKind.Cancellation ||
            receipt.IsDeleted || receipt.fid != source.Invoice.FactorId || receipt.InvoiceType != InvoiceTypeEnum.فاکتور ||
            receipt.AccType != AcEnum.واریزی || receipt.Status != AccStatusEnum.تایید_شده ||
            receipt.CurrencyType != CurrencyTypeEnum.ریال || receipt.credit is not { } paid ||
            paid <= 0 || paid > 999999999999999999m || paid != decimal.Truncate(paid))
            throw new ArgumentException("پرداخت باید دریافت ریالی تأییدشده برای همان فاکتور پذیرفته‌شده باشد.");

        var method = ResolveMethod(receipt.PayType, receipt.Vosoled, details.Method);
        if (string.IsNullOrWhiteSpace(details.ReferenceNumber) ||
            !IsIdentifier(details.ReferenceNumber) ||
            (details.TerminalNumber is not null && !IsIdentifier(details.TerminalNumber)))
            throw new ArgumentException("شماره مرجع معتبر پرداخت و در صورت ارائه، شماره پایانه را از مدرک پرداخت وارد کنید.");

        using var invoice = JsonDocument.Parse(source.InvoicePayload);
        var header = invoice.RootElement.GetProperty("header");
        var settlement = header.GetProperty("setm").GetInt32();
        if (header.GetProperty("taxid").GetString() != source.Invoice.TaxId || settlement is not (2 or 3))
            throw new ArgumentException("ثبت پرداخت برای سند مالیاتی با تسویهٔ نسیه یا ترکیبی مجاز است.");

        var paidDate = receipt.PayType == PayTypeEnum.چکی ? receipt.VosolDateM : receipt.date;
        if (details.PaidAt.Offset != TimeSpan.FromMinutes(210) || paidDate is null ||
            paidDate.Value.Date != details.PaidAt.Date || details.PaidAt > now ||
            details.PaidAt < DateTimeOffset.FromUnixTimeMilliseconds(header.GetProperty("indatim").GetInt64()))
            throw new ArgumentException("تاریخ پرداخت باید با تاریخ دریافت یا وصول ERP و زمان صدور سند تطبیق داشته باشد.");

        // Exclude the cash principal already declared on a mixed invoice without estimating its VAT share.
        var upperBound = header.GetProperty("tbill").GetDecimal() -
            (settlement == 3 ? header.GetProperty("cap").GetDecimal() : 0m);
        if (paid + source.CommittedPayments > upperBound)
            throw new ArgumentException("جمع درخواست‌های پرداخت از ماندهٔ قابل بررسی صورتحساب بیشتر است.");

        var referenceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(details.ReferenceNumber.ToUpperInvariant())));
        var payload = JsonSerializer.Serialize(new
        {
            taxId = source.Invoice.TaxId, paidAmount = paid, paymentDate = details.PaidAt.ToUnixTimeMilliseconds(), paymentMethod = method,
            terminalNumber = details.TerminalNumber, referenceNumber = details.ReferenceNumber
        });
        return new(paid, referenceHash, payload);
    }

    private static string ResolveMethod(PayTypeEnum payType, bool collected, TaxReceiptMethod method) => (payType, method) switch
    {
        (PayTypeEnum.چکی, TaxReceiptMethod.Cheque) when collected => "CHEQUE",
        (PayTypeEnum.نقدی, TaxReceiptMethod.Cash) => "CASH",
        (PayTypeEnum.نقدی, TaxReceiptMethod.Pos) => "POS",
        (PayTypeEnum.نقدی, TaxReceiptMethod.Internet) => "INTERNET",
        (PayTypeEnum.نقدی, TaxReceiptMethod.Card) => "CARD",
        (PayTypeEnum.نقدی, TaxReceiptMethod.Transfer) => "TRANSFER",
        _ => throw new ArgumentException("روش پرداخت با دریافت ERP مطابقت ندارد یا چک هنوز وصول نشده است.")
    };

    private static bool IsIdentifier(string value) =>
        value.Length is > 0 and <= 100 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '/' or '_');
}
