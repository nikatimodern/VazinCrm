using System.Text.Json;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Taxation;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxExportTests
{
    private static readonly TaxSigningBinding Binding = new(TaxAuthorityEnvironment.Sandbox, new string('A', 64), "14003778990");
    private static readonly DateTimeOffset Issued = TaxProtocolTests.Issued;

    [Fact]
    public void ExportsCustomsTotalsInsteadOfCommercialAmounts()
    {
        var (source, details) = Fixture();
        var preview = InvoicePreview.Build(source, export: details);
        Assert.Empty(preview.Errors);
        Assert.Equal(TaxInvoicePattern.Export, preview.Pattern);
        Assert.Equal(70m, preview.Total);
        var filing = new TaxFilingDetails(2, Issued, 0, 2);
        var (id, raw) = TaxInvoicePayload.Build(preview, Binding, filing);
        Assert.True(TaxInvoicePayload.MatchesTaxId(preview.FiscalId!, id, filing.Serial));
        Assert.NotEqual(id, TaxInvoicePayload.Build(preview, Binding, filing with { Serial = 3 }).TaxId);
        using var document = JsonDocument.Parse(raw);
        var header = document.RootElement.GetProperty("header");
        Assert.Equal(7, header.GetProperty("inp").GetInt32());
        Assert.Equal(1, header.GetProperty("inty").GetInt32());
        Assert.Equal(details.CustomsDeclarationNumber, header.GetProperty("cdcn").GetString());
        Assert.Equal(details.CustomsDeclarationDate!.Value.ToUnixTimeSeconds() / 86400, header.GetProperty("cdcd").GetInt32());
        Assert.Equal(700001m, header.GetProperty("torv").GetDecimal());
        Assert.Equal(7.5521m, header.GetProperty("tocv").GetDecimal());
        Assert.Equal(1.25m, header.GetProperty("tonw").GetDecimal());
        Assert.Equal(700001m, header.GetProperty("tbill").GetDecimal());
        Assert.False(header.TryGetProperty("tinb", out _));
        Assert.False(header.TryGetProperty("tdis", out _));
        var line = document.RootElement.GetProperty("body")[0];
        Assert.Equal("USD", line.GetProperty("cut").GetString());
        Assert.Equal(95000m, line.GetProperty("exr").GetDecimal());
        Assert.Equal(0, line.GetProperty("vam").GetInt32());
        Assert.Equal(700001, line.GetProperty("tsstam").GetInt32());
        Assert.False(line.TryGetProperty("fee", out _));
        Assert.False(line.TryGetProperty("adis", out _));
    }

    [Theory]
    [InlineData("missing-details", "اظهارنامه")]
    [InlineData("wrong-row", "متناظر")]
    [InlineData("duplicate-rows", "دقیقاً یک")]
    [InlineData("empty-weight", "وزن")]
    [InlineData("negative-rial", "ارزش ریالی")]
    [InlineData("foreign-precision", "ارزش ارزی")]
    [InlineData("invalid-rate", "نرخ برابری")]
    [InlineData("currency-mismatch", "کد ISO")]
    [InlineData("future-customs", "تاریخ کوتاژ")]
    [InlineData("customs-office", "کد گمرک")]
    [InlineData("customs-number", "شماره کوتاژ")]
    [InlineData("duties-unknown", "عوارض")]
    [InlineData("nonzero-vat", "صفر")]
    [InlineData("domestic-with-export-data", "علامت صادرات")]
    public void InvalidExportFailsInPreviewAndPayload(string scenario, string message)
    {
        var (source, initial) = Fixture();
        ExportTaxDetails details = initial;
        var row = initial.Lines[0];
        switch (scenario)
        {
            case "missing-details": details = null; break;
            case "wrong-row": details = initial with { Lines = [row with { FactorItemId = Guid.NewGuid() }] }; break;
            case "duplicate-rows": details = initial with { Lines = [row, row] }; break;
            case "empty-weight": details = initial with { Lines = [row with { NetWeightKg = 0 }] }; break;
            case "negative-rial": details = initial with { Lines = [row with { RialValue = -1 }] }; break;
            case "foreign-precision": details = initial with { Lines = [row with { CurrencyValue = 1.23456m }] }; break;
            case "invalid-rate": details = initial with { Lines = [row with { ExchangeRate = 0 }] }; break;
            case "currency-mismatch": details = initial with { Lines = [row with { CurrencyCode = "EUR" }] }; break;
            case "future-customs": details = initial with { CustomsDeclarationDate = Issued.AddDays(1) }; break;
            case "customs-office": details = initial with { CustomsOffice = "abc" }; break;
            case "customs-number": details = initial with { CustomsDeclarationNumber = new string('1', 15) }; break;
            case "duties-unknown": details = initial with { NoOtherTaxesOrDuties = false }; break;
            case "nonzero-vat": source.Factor.PercentTax = 10; break;
            case "domestic-with-export-data": source.Factor.Export = false; break;
        }
        var preview = InvoicePreview.Build(source, export: details);
        Assert.Contains(preview.Errors, x => x.Contains(message, StringComparison.Ordinal));
        var filing = new TaxFilingDetails(2, Issued, 0, 2);
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(preview, Binding, filing));
        if (source.Factor.Export == true)
            Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(preview with { Errors = [] }, Binding, filing));
    }

    [Fact]
    public void OptionalCustomsHeaderMayBeOmittedButIssuedTimeStillApplies()
    {
        var (source, details) = Fixture();
        var filing = new TaxFilingDetails(2, Issued, 0, 1);
        var optional = details with { CustomsOffice = null, CustomsDeclarationDate = null, CustomsDeclarationNumber = null };
        using var document = JsonDocument.Parse(TaxInvoicePayload.Build(InvoicePreview.Build(source, export: optional), Binding, filing).Json);
        Assert.False(document.RootElement.GetProperty("header").TryGetProperty("cdcn", out _));
        var afterIssue = details with { CustomsDeclarationDate = Issued.AddMinutes(1) };
        var preview = InvoicePreview.Build(source, export: afterIssue);
        Assert.Empty(preview.Errors);
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(preview, Binding, filing));
    }

    [Fact]
    public void ForeignCurrencyAloneCannotSelectAnUnsupportedPattern()
    {
        var (source, _) = Fixture();
        source.Factor.Export = false;
        var preview = InvoicePreview.Build(source);
        Assert.Contains(preview.Errors, e => e.Contains("فروش ارز"));
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(preview with { Pattern = (TaxInvoicePattern)2, Errors = [] },
            Binding, new(1, Issued, 0, 1)));
    }

    [Fact]
    public void ExportCorrectionKeepsPatternAndRequiresChronologicalCustomsAmendment()
    {
        var (original, details) = Fixture();
        var reference = Reference(original, details);
        var (candidate, change) = Fixture();
        candidate.Factor.FactorItems[0].ProductCode = original.Factor.FactorItems[0].ProductCode;
        candidate.Mappings[0].ProductId = original.Mappings[0].ProductId;
        candidate = candidate with { ActiveMemory = original.ActiveMemory };
        var correction = new TaxReferenceRequest(candidate.Factor.Id, TaxDispatchKind.Correction, 2, Issued,
            null, false, null, change with { CustomsDeclarationNumber = "4321" });
        using var corrected = JsonDocument.Parse(TaxReferenceInvoice.Build(reference with { Candidate = candidate }, correction, Binding).Payload);
        var header = corrected.RootElement.GetProperty("header");
        Assert.Equal(7, header.GetProperty("inp").GetInt32());
        Assert.Equal(2, header.GetProperty("ins").GetInt32());
        Assert.Equal(reference.Reference.TaxId, header.GetProperty("irtaxid").GetString());
        Assert.Throws<ArgumentException>(() => TaxReferenceInvoice.Build(reference with { Candidate = candidate },
            correction with { Export = change with { CustomsDeclarationDate = Issued.AddDays(-4) } }, Binding));
    }

    [Fact]
    public void ExportAllowsCancellationAndRejectsReturn()
    {
        var (original, details) = Fixture();
        var reference = Reference(original, details);
        var request = new TaxReferenceRequest(null, TaxDispatchKind.Cancellation, 2, Issued,
            null, false, TaxCancellationReason.IncorrectTransactionDate);
        using var canceled = JsonDocument.Parse(TaxReferenceInvoice.Build(reference, request, Binding).Payload);
        var header = canceled.RootElement.GetProperty("header");
        Assert.Equal(3, header.GetProperty("ins").GetInt32());
        Assert.Equal(reference.Reference.TaxId, header.GetProperty("irtaxid").GetString());
        Assert.False(header.TryGetProperty("inp", out _));
        var error = Assert.Throws<ArgumentException>(() => TaxReferenceInvoice.Build(reference,
            request with { Kind = TaxDispatchKind.Return, CancellationReason = null }, Binding));
        Assert.Contains("برگشت از فروش", error.Message);
    }

    private static TaxReferenceSource Reference(TaxPreviewSource source, ExportTaxDetails details)
    {
        var preview = InvoicePreview.Build(source, export: details);
        var (id, payload) = TaxInvoicePayload.Build(preview, Binding, new(1, Issued, 0, 1));
        var reference = new TaxDispatch
        {
            Status = TaxDispatchStatus.Accepted, TaxId = id, InvoiceSerial = 1,
            MemoryId = preview.MemoryId!.Value, FiscalId = preview.FiscalId, AuthorityEnvironment = Binding.Environment
        };
        return new(reference, JsonSerializer.Serialize(preview), payload,
            JsonSerializer.Serialize(new { taxId = id, invoiceStatus = "APPROVED" }), null!);
    }

    private static (TaxPreviewSource Source, ExportTaxDetails Details) Fixture()
    {
        var product = Guid.NewGuid();
        var item = new FactorItem { Id = Guid.NewGuid(), ProductCode = product, Row = 1, WeightCount = "2", UnitAmount = 35, TotalAmount = 70 };
        var factor = new Factor
        {
            Id = Guid.NewGuid(), DateMiladi = Issued.Date, OfficialType = 1, status = FactorStatus.تایید_شده,
            CustomerId = Guid.NewGuid(), Export = true, CurrencyType = CurrencyTypeEnum.دلار,
            SumTotalAmount = 70, PaymentAmount = 70, FactorItems = [item]
        };
        var source = new TaxPreviewSource(factor, new TaxMemory { Id = Guid.NewGuid(), FiscalId = "ABC123" },
            [new TaxProductMapping { ProductId = product, GoodsId = "1234567890123", TaxUnit = "164", IsDefault = true }], true);
        return (source, new ExportTaxDetails("1234", Issued.AddDays(-1), "12345", true,
            [new ExportTaxLine(item.Id, "USD", 95000, 700001, 7.5521m, 1.25m)]));
    }
}
