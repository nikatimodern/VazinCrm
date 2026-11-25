using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Taxation;
using Xunit;

namespace CRM.Taxation.Tests;

public class InvoicePreviewTests
{
    private static TaxPreviewSource Sale()
    {
        var product = Guid.NewGuid();
        var item = new FactorItem { ProductCode = product, Row = 1, Unit = 4, WeightCount = "2.5",
            UnitAmount = 4000, Discount = 1000, TaxAmount = 900, TotalAmount = 9900 };
        var factor = new Factor { Id = Guid.NewGuid(), CustomerId = Guid.NewGuid(), Cus_EghtesadiCode = "123",
            DateMiladi = new DateTime(2026, 11, 24), OfficialType = 1, PercentTax = 10,
            SumTotalAmount = 10000, Discount = 1000, TotalTax = 900, PaymentAmount = 9900, FactorItems = [item] };
        return new(factor, new TaxMemory { Id = Guid.NewGuid(), FiscalId = "ABC123" },
            [new TaxProductMapping { ProductId = product, Unit = 4, IsDefault = true, GoodsId = "1234567890123", TaxUnit = "164" }], true);
    }

    [Fact]
    public void PreviewCapturesSelectedMemoryAndMappedAmounts()
    {
        var sale = Sale();
        var result = InvoicePreview.Build(sale);
        Assert.Empty(result.Errors);
        Assert.Equal(9900m, result.Total);
        Assert.Equal("164", Assert.Single(result.Lines).TaxUnit);
        sale.ActiveMemory.FiscalId = "XYZ123";
        Assert.Equal("ABC123", result.FiscalId);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("-2")]
    [InlineData("1,2")]
    public void MalformedQuantityBlocksPreview(string quantity)
    {
        var sale = Sale();
        sale.Factor.FactorItems[0].WeightCount = quantity;
        Assert.NotEmpty(InvoicePreview.Build(sale).Errors);
    }

    [Fact]
    public void MismatchNeverRewritesApprovedAmounts()
    {
        var sale = Sale();
        sale.Factor.FactorItems[0].TaxAmount = 1000;
        Assert.NotEmpty(InvoicePreview.Build(sale).Errors);
        Assert.Equal(1000m, sale.Factor.FactorItems[0].TaxAmount);
    }

    [Theory]
    [InlineData(true, 9)]
    [InlineData(false, 10)]
    public void MatchesErpRoundingAndDiscountTaxBase(bool afterDiscount, int expectedTax)
    {
        var sale = Sale() with { TaxAfterDiscount = afterDiscount };
        var factor = sale.Factor;
        var item = Assert.Single(factor.FactorItems);
        item.WeightCount = "1.005";
        item.UnitAmount = 100;
        item.Discount = factor.Discount = 5;
        factor.SumTotalAmount = 100;
        item.TaxAmount = factor.TotalTax = expectedTax;
        item.TotalAmount = factor.PaymentAmount = 95 + expectedTax;
        var result = InvoicePreview.Build(sale);
        Assert.Contains(result.Errors, e => e.Contains("حاصل دقیق ضرب"));
        Assert.Equal(100m, result.Gross);
        Assert.Equal(expectedTax, result.Vat);
        Assert.Equal(expectedTax, item.TaxAmount);
        Assert.Equal(95m + expectedTax, item.TotalAmount);
    }

    [Theory]
    [InlineData(true, 9, true)]
    [InlineData(false, 10, false)]
    [InlineData(false, 9, true)]
    [InlineData(true, 10, false)]
    public void TaxBasisIsReconciledWithoutChangingHistoricalAmounts(bool current, int vat, bool allowed)
    {
        var source = Sale() with { TaxAfterDiscount = current };
        var factor = source.Factor;
        var item = Assert.Single(factor.FactorItems);
        item.WeightCount = "2.5";
        item.UnitAmount = 40.4m;
        item.Discount = factor.Discount = 11;
        item.TaxAmount = factor.TotalTax = vat;
        factor.SumTotalAmount = 101;
        item.TotalAmount = factor.PaymentAmount = 90 + vat;
        factor.Cus_EghtesadiCode = factor.Cus_ShenaseMeli = "10100302746";
        var preview = InvoicePreview.Build(source);
        var binding = new TaxSigningBinding(TaxAuthorityEnvironment.Sandbox, new string('A', 64), "14003778990");
        var filing = new TaxFilingDetails(15, new DateTimeOffset(factor.DateMiladi!.Value, TimeSpan.FromMinutes(210)), 2, 2);
        Assert.Equal(allowed, preview.Errors.Count == 0);
        if (allowed)
        {
            using var payload = System.Text.Json.JsonDocument.Parse(TaxInvoicePayload.Build(preview, binding, filing).Json);
            var header = payload.RootElement.GetProperty("header");
            Assert.Equal(101, header.GetProperty("tprdis").GetInt32());
            Assert.Equal(11, header.GetProperty("tdis").GetInt32());
            Assert.Equal(9, header.GetProperty("tvam").GetInt32());
            Assert.Equal(99, header.GetProperty("tbill").GetInt32());
        }
        else
        {
            // A historical snapshot may predate the preview's V7.9 validation.
            Assert.Throws<ArgumentException>(() => TaxInvoicePayload.Build(preview with { Errors = [] }, binding, filing));
        }
        Assert.Equal(vat, item.TaxAmount);
        Assert.Equal(90m + vat, factor.PaymentAmount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FloorAndHeaderMismatchAreReportedSeparately(bool headerMismatch)
    {
        var source = Sale();
        var factor = source.Factor;
        var item = Assert.Single(factor.FactorItems);
        item.WeightCount = "1";
        item.UnitAmount = factor.SumTotalAmount = 101;
        item.Discount = factor.Discount = 0;
        item.TaxAmount = factor.TotalTax = 10;
        item.TotalAmount = 111;
        factor.PaymentAmount = headerMismatch ? 112 : 111;
        var result = InvoicePreview.Build(source);
        Assert.Contains(result.Errors, e => e.Contains("حاصل دقیق اعمال نرخ"));
        Assert.Equal(headerMismatch, result.Errors.Any(e => e.Contains("جمع ردیف‌ها")));
    }

    [Theory]
    [InlineData("gross")]
    [InlineData("discount")]
    [InlineData("vat")]
    [InlineData("total")]
    public void EachRecordedHeaderAmountMustMatchRows(string field)
    {
        var source = Sale();
        var factor = source.Factor;
        if (field == "gross") factor.SumTotalAmount++;
        if (field == "discount") factor.Discount++;
        if (field == "vat") factor.TotalTax++;
        if (field == "total") factor.PaymentAmount++;
        var preview = InvoicePreview.Build(source);
        Assert.Contains(preview.Errors, e => e.Contains("جمع ردیف‌ها"));
        Assert.Equal(9900m, Assert.Single(factor.FactorItems).TotalAmount);
    }

    [Fact]
    public void MissingMemoryAndMissingMappingAreReportedIndependently()
    {
        var sale = Sale() with { ActiveMemory = null, Mappings = [] };
        Assert.Equal(2, InvoicePreview.Build(sale).Errors.Count);
    }
}
