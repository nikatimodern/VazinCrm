using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxPayment;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Taxation;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxPaymentTests
{
    [Theory]
    [InlineData("pending")]
    [InlineData("wrong-factor")]
    [InlineData("unapproved")]
    [InlineData("uncollected")]
    [InlineData("invalid-method")]
    [InlineData("cash-invoice")]
    [InlineData("invalid-date")]
    [InlineData("above-total")]
    public void InvalidPaymentDoesNotProduceRequest(string scenario)
    {
        var (source, details) = Fixture();
        switch (scenario)
        {
            case "pending": source.Invoice.Status = TaxDispatchStatus.Submitted; break;
            case "wrong-factor": source.Receipt.fid = Guid.NewGuid(); break;
            case "unapproved": source.Receipt.Status = AccStatusEnum.در_دست_تایید; break;
            case "uncollected": source.Receipt.Vosoled = false; break;
            case "invalid-method": details = details with { Method = TaxReceiptMethod.Cash }; break;
            case "cash-invoice": source = source with { InvoicePayload = source.InvoicePayload.Replace("\"setm\":2", "\"setm\":1") }; break;
            case "invalid-date": details = details with { PaidAt = details.PaidAt.AddDays(-1) }; break;
            case "above-total": source = source with { CommittedPayments = 101 }; break;
        }
        Assert.Throws<ArgumentException>(() => EnqueueTaxPaymentCommandHandler.Prepare(source, details, details.PaidAt.AddDays(2)));
    }

    [Fact]
    public void PaymentUsesOfficialMethodAndEpochMilliseconds()
    {
        var (source, details) = Fixture();
        var result = EnqueueTaxPaymentCommandHandler.Prepare(source, details, details.PaidAt.AddDays(1));
        using var json = JsonDocument.Parse(result.Content);
        Assert.Equal("CHEQUE", json.RootElement.GetProperty("paymentMethod").GetString());
        Assert.Equal(source.Invoice.TaxId, json.RootElement.GetProperty("taxId").GetString());
        Assert.Equal(details.PaidAt.ToUnixTimeMilliseconds(), json.RootElement.GetProperty("paymentDate").GetInt64());
        Assert.Equal(10, json.RootElement.GetProperty("paidAmount").GetInt32());
    }

    private static (TaxPaymentSource Source, TaxPaymentDetails Details) Fixture()
    {
        var issued = TaxProtocolTests.Issued;
        var paid = issued.AddHours(1);
        var invoice = new TaxDispatch { FactorId = Guid.NewGuid(), Status = TaxDispatchStatus.Accepted, TaxId = "ABC12304023000112233AA" };
        var receipt = new Accountancy
        {
            Id = Guid.NewGuid(), fid = invoice.FactorId, InvoiceType = InvoiceTypeEnum.فاکتور,
            AccType = AcEnum.واریزی, Status = AccStatusEnum.تایید_شده, PayType = PayTypeEnum.چکی,
            Vosoled = true, VosolDateM = paid.Date, credit = 10
        };
        var raw = JsonSerializer.Serialize(new { header = new { taxid = invoice.TaxId, setm = 2, tbill = 110,
            indatim = issued.ToUnixTimeMilliseconds() } });
        return (new(invoice, receipt, raw, 0), new(receipt.Id, TaxReceiptMethod.Cheque, paid, "ref-001", null));
    }
}
