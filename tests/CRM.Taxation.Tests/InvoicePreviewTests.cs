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
            [new TaxProductMapping { ProductId = product, Unit = 4, IsDefault = true, GoodsId = "1234567890123", TaxUnit = "164" }]);
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

    [Fact]
    public void MissingMemoryAndMissingMappingAreReportedIndependently()
    {
        var sale = Sale() with { ActiveMemory = null, Mappings = [] };
        Assert.Equal(2, InvoicePreview.Build(sale).Errors.Count);
    }
}
