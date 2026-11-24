using System.Globalization;
using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Taxation;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxReferenceTests
{
    private static readonly TaxSigningBinding Binding = new(TaxAuthorityEnvironment.Sandbox, new string('A', 64), "14003778990");
    private static readonly DateTimeOffset Issued = TaxProtocolTests.Issued;

    [Theory]
    [InlineData(TaxDispatchKind.Correction, 2, 3, 31828)]
    [InlineData(TaxDispatchKind.Return, 4, 1, 10573)]
    [InlineData(TaxDispatchKind.Cancellation, 3, 0, 0)]
    public void BuildsIndependentDocumentWithReferenceAndCorrectSubject(TaxDispatchKind kind, int subject, int quantity, int total)
    {
        // The fixture uses an integral VAT at each quantity so the exact V7.9 rules apply.
        var (source, filing) = Fixture(kind);
        var original = source.Preview;
        var result = TaxReferenceInvoice.Build(source, filing, Binding);
        using var json = JsonDocument.Parse(result.Payload);
        var header = json.RootElement.GetProperty("header");
        Assert.Equal(subject, header.GetProperty("ins").GetInt32());
        Assert.Equal(source.Reference.TaxId, header.GetProperty("irtaxid").GetString());
        Assert.NotEqual(source.Reference.TaxId, result.TaxId);
        Assert.False(header.TryGetProperty("tinb", out _));
        if (kind == TaxDispatchKind.Cancellation)
        {
            Assert.False(header.TryGetProperty("inty", out _));
            Assert.False(header.TryGetProperty("inp", out _));
            Assert.False(json.RootElement.TryGetProperty("body", out _));
        }
        else
        {
            Assert.Equal(quantity, json.RootElement.GetProperty("body")[0].GetProperty("am").GetInt32());
            Assert.Equal(total, header.GetProperty("tbill").GetInt32());
        }
        var snapshot = JsonSerializer.Deserialize<InvoicePreview>(result.Preview)!;
        Assert.Equal(source.Reference.TaxId, snapshot.ReferenceTaxId);
        Assert.Equal(kind, snapshot.DispatchKind);
        Assert.Equal(original, source.Preview);
    }

    [Theory]
    [InlineData("canceled")]
    [InlineData("wrong-evidence")]
    [InlineData("unaccepted")]
    [InlineData("before-reference")]
    [InlineData("serial")]
    [InlineData("seller")]
    [InlineData("buyer")]
    [InlineData("rate")]
    [InlineData("new-goods")]
    [InlineData("vat-discrepancy")]
    [InlineData("unchanged")]
    [InlineData("quantity-error")]
    public void InvalidCorrectionsAreRejected(string scenario)
    {
        var (source, filing) = Fixture(TaxDispatchKind.Correction);
        var binding = Binding;
        switch (scenario)
        {
            case "canceled":
                source = source with { WorkFolderEvidence = Evidence(source.Reference.TaxId, "CANCELED") };
                break;
            case "wrong-evidence":
                source = source with { WorkFolderEvidence = Evidence("wrong", "APPROVED") };
                break;
            case "unaccepted":
                source.Reference.Status = TaxDispatchStatus.Submitted;
                break;
            case "before-reference":
                filing = filing with { IssuedAt = Issued.AddSeconds(-1) };
                break;
            case "serial":
                filing = filing with { Serial = source.Reference.InvoiceSerial!.Value };
                break;
            case "seller":
                binding = binding with { SellerEconomicCode = "11111111111" };
                break;
            case "buyer":
                source.Candidate.Factor.Cus_ShenaseMeli = "11111111111";
                break;
            case "rate":
                source.Candidate.Factor.PercentTax = 8;
                break;
            case "new-goods":
                source.Candidate.Mappings[0].GoodsId = "1111111111111";
                break;
            case "vat-discrepancy":
                source.Candidate.Factor.FactorItems[0].TaxAmount++;
                break;
            case "unchanged":
                UpdateAmounts(source.Candidate.Factor, 2, 600, 1746);
                break;
            case "quantity-error":
                UpdateAmounts(source.Candidate.Factor, 1, 200, 882);
                break;
        }
        Assert.Throws<ArgumentException>(() => TaxReferenceInvoice.Build(source, filing, binding));
    }

    [Fact]
    public void QuantityCorrectionRequiresExplicitEntryError()
    {
        var (source, filing) = Fixture(TaxDispatchKind.Correction);
        UpdateAmounts(source.Candidate.Factor, 1, 300, 873);
        Assert.Throws<ArgumentException>(() => TaxReferenceInvoice.Build(source, filing, Binding));
        var registered = TaxReferenceInvoice.Build(source, filing with { CorrectsQuantityError = true }, Binding);
        Assert.Equal("QuantityEntryError", JsonSerializer.Deserialize<InvoicePreview>(registered.Preview)!.ReferenceReason);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void FullOrExcessReturnIsRejected(int amount)
    {
        var (source, filing) = Fixture(TaxDispatchKind.Return);
        if (amount == 2) UpdateAmounts(source.Candidate.Factor, 2, 600, 1746);
        else UpdateAmounts(source.Candidate.Factor, 3, 600, 2646);
        Assert.Throws<ArgumentException>(() => TaxReferenceInvoice.Build(source, filing, Binding));
    }

    [Fact]
    public void CancellationRequiresReasonAndCannotReferenceCancellation()
    {
        var (source, filing) = Fixture(TaxDispatchKind.Cancellation);
        Assert.Throws<ArgumentException>(() => TaxReferenceInvoice.Build(source, filing with { CancellationReason = null }, Binding));
        source.Reference.Kind = TaxDispatchKind.Cancellation;
        Assert.Throws<ArgumentException>(() => TaxReferenceInvoice.Build(source, filing, Binding));
    }

    private static (TaxReferenceSource, TaxReferenceRequest) Fixture(TaxDispatchKind kind)
    {
        var prior = TaxProtocolTests.Preview(Guid.NewGuid());
        var line = prior.Lines[0] with { Discount = 600, Vat = 1746, Total = 21146 };
        prior = prior with { Lines = [line], Discount = 600, Vat = 1746, Total = 21146 };
        var (id, payload) = TaxInvoicePayload.Build(prior, Binding, new TaxFilingDetails(10, Issued, 2, 2));
        var dispatch = new TaxDispatch { Id = Guid.NewGuid(), Status = TaxDispatchStatus.Accepted, FactorId = prior.FactorId,
            AuthorityEnvironment = Binding.Environment, FiscalId = prior.FiscalId, MemoryId = prior.MemoryId!.Value, TaxId = id, InvoiceSerial = 10 };
        var factor = new Factor
        {
            Id = Guid.NewGuid(), DateMiladi = Issued.Date, OfficialType = 1, status = FactorStatus.تایید_شده,
            CustomerId = Guid.NewGuid(), Cus_EghtesadiCode = prior.BuyerEconomicCode, Cus_ShenaseMeli = prior.BuyerNationalId,
            ParentReturnedId = kind == TaxDispatchKind.Return ? prior.FactorId : null,
            PercentTax = 9, FactorItems = [new FactorItem { Row = 1, ProductCode = line.ProductId, UnitAmount = 10000 }]
        };
        if (kind == TaxDispatchKind.Return) UpdateAmounts(factor, 1, 300, 873);
        else UpdateAmounts(factor, 3, 800, 2628);
        var source = new TaxPreviewSource(factor, new TaxMemory { Id = dispatch.MemoryId, FiscalId = dispatch.FiscalId },
            [new TaxProductMapping { ProductId = line.ProductId!.Value, GoodsId = line.GoodsId, TaxUnit = line.TaxUnit, IsDefault = true }], true);
        return (new TaxReferenceSource(dispatch, JsonSerializer.Serialize(prior), payload, Evidence(id, "APPROVED"), source),
            new TaxReferenceRequest(kind == TaxDispatchKind.Cancellation ? null : factor.Id, kind, 11, Issued, null, false,
                kind == TaxDispatchKind.Cancellation ? TaxCancellationReason.FullReturn : null));
    }

    private static string Evidence(string id, string state) => JsonSerializer.Serialize(new { taxId = id, invoiceStatus = state });

    private static void UpdateAmounts(Factor factor, int quantity, decimal discount, decimal vat)
    {
        var line = factor.FactorItems[0];
        line.WeightCount = quantity.ToString(CultureInfo.InvariantCulture);
        factor.SumTotalAmount = quantity * line.UnitAmount;
        line.Discount = factor.Discount = discount;
        line.TaxAmount = factor.TotalTax = vat;
        line.TotalAmount = factor.PaymentAmount = factor.SumTotalAmount - discount + vat;
    }
}
