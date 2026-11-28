using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using Xunit;

namespace CRM.Taxation.Tests;

public sealed class TaxUnitCatalogTests
{
    [Fact]
    public void PublishedCodesAreExactAndDistinct()
    {
        Assert.Equal(102, TaxUnitCatalog.Units.Count);
        Assert.Equal(102, TaxUnitCatalog.Codes.Distinct().Count());
        Assert.Contains(TaxUnitCatalog.Units, unit => unit is { Code: "164", Title: "کیلوگرم" });
        Assert.Contains(TaxUnitCatalog.Units, unit => unit is { Code: "1622", Title: "گرم" });
        Assert.Contains(TaxUnitCatalog.Units, unit => unit is { Code: "16134", Title: "نفر-ماه" });
        Assert.False(TaxUnitCatalog.Contains("17"));
        Assert.False(TaxUnitCatalog.Contains("164 "));
    }

    [Fact]
    public void PayloadRejectsUnknownCodeEvenWhenPreviewHasNoErrors()
    {
        var memory = Guid.NewGuid();
        var preview = TaxProtocolTests.Preview(memory);
        var invalid = preview with { Lines = [preview.Lines[0] with { TaxUnit = "17" }] };
        var binding = new TaxSigningBinding(TaxAuthorityEnvironment.Sandbox, new string('A', 64), "14003778990");
        var filing = new TaxFilingDetails(49321217, TaxProtocolTests.Issued, 2, 2);
        Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(invalid, binding, filing));
        Assert.Contains("\"mu\":\"164\"", TaxInvoicePayload.Build(preview, binding, filing).Json);
    }
}
