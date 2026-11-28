using CRM.Core.Domain.PreFactors.Entities;

namespace CRM.Core.Domain.Sepidar.Quotations.Entites;

public static class QuotationExportValues
{
    public static void ApplyAmounts(Quotation quote, PreFactor source)
    {
        quote.Price = quote.PriceInBaseCurrency = source.SumTotalAmount;
        quote.Discount = quote.DiscountInBaseCurrency = source.Discount;
        quote.Tax = quote.TaxInBaseCurrency = source.TotalTax;
        quote.NetPriceInBaseCurrency = source.PaymentAmount;
    }

    public static QuotationItem CreateItem(int id, int quotationId, int row, int itemId, int stockId,
        int weight, PreFactorItem item)
    {
        return new QuotationItem {
            QuotationItemID = id,
            QuotationRef = quotationId,
            RowID = row,
            ItemRef = itemId,

            TracingRef = null,

            StockRef = stockId,

            Quantity = weight,
            SecondaryQuantity = null,
            Fee = item.UnitAmount,

            Price = item.TotalAmount,
            PriceInBaseCurrency = item.TotalAmount,

            Discount = item.Discount,
            DiscountInBaseCurrency = item.Discount,

            PriceInfoDiscountRate = 0,
            PriceInfoPriceDiscount = 0,
            PriceInfoPercentDiscount = 0,
            CustomerDiscount = 0,
            CustomerDiscountRate = 0,

            Addition = 0,
            AdditionInBaseCurrency = 0,
            Tax = item.TaxAmount,
            TaxInBaseCurrency = item.TaxAmount,
            Duty = 0,
            DutyInBaseCurrency = 0,

            NetPriceInBaseCurrency = item.TotalAmount,
            Rate = 1,
            UsedQuantity = 0,

            Description = null,
            DiscountQuotationItemRef = null,
            Description_En = null,

            ProductPackRef = null,
            ProductPackQuantity = null,

            AggregateAmountDiscountRate = 0,
            AggregateAmountPriceDiscount = 0,
            AggregateAmountPercentDiscount = 0,

            AdditionFactor_VatEffective = 0,
            AdditionFactorInBaseCurrency_VatEffective = 0,
            AdditionFactor_VatIneffective = 0,
            AdditionFactorInBaseCurrency_VatIneffective = 0,

            IsAggregateDiscountInvoiceItem = false,
            IsReadyForProductOrder = true,
        };
    }
}
