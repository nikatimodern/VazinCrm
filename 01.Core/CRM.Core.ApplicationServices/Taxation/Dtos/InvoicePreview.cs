using System.Globalization;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Dtos;

public sealed record PreviewLine(int Row, Guid? ProductId, string Description, decimal Quantity,
    string? GoodsId, string? TaxUnit, decimal UnitPrice, decimal Discount, decimal Vat, decimal Total);

public sealed record InvoicePreview(Guid FactorId, Guid? MemoryId, string? FiscalId, DateTime? InvoiceDate,
    string Buyer, string BuyerEconomicCode, string Currency, decimal Gross, decimal Discount, decimal Vat,
    decimal Total, IReadOnlyList<PreviewLine> Lines, IReadOnlyList<string> Errors)
{
    public string RuleVersion => "RC_IITP.IS_V7.9";
    public string? BuyerNationalId { get; init; }
    public string? BuyerPostalCode { get; init; }
    public int VatPercent { get; init; }

    public static InvoicePreview Build(TaxPreviewSource source)
    {
        var factor = source.Factor;
        var errors = new List<string>();
        var lines = new List<PreviewLine>();
        if (source.ActiveMemory is null) errors.Add("حافظه فعال انتخاب نشده است.");
        if (factor.DateMiladi is null) errors.Add("تاریخ فاکتور مشخص نیست.");
        if (factor.CustomerId is null || string.IsNullOrWhiteSpace(factor.Cus_EghtesadiCode))
            errors.Add("اطلاعات خریدار برای پیش‌نمایش فروش داخلی کامل نیست.");
        if (factor.Export == true || factor.CurrencyType != 0 || factor.ParentReturnedId.HasValue)
            errors.Add("این پیش‌نمایش برای فاکتور اصلی فروش داخلی ریالی است.");
        if (factor.status == CRM.Core.Domain.Factors.Enum.FactorStatus.ابطال_شده)
            errors.Add("فاکتور ابطال شده است.");
        if (factor.OfficialType != 1) errors.Add("فاکتور رسمی نیست.");
        var mappings = source.Mappings.Where(x => x.IsDefault).ToDictionary(x => (x.ProductId, x.Unit));
        decimal gross = 0, discount = 0, vat = 0, total = 0;
        foreach (var item in factor.FactorItems.Where(x => !x.IsDeleted).OrderBy(x => x.Row).ThenBy(x => x.Id))
        {
            var label = $"ردیف {item.Row} ";
            if (!decimal.TryParse(item.WeightCount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out var quantity) || quantity <= 0)
                errors.Add(label + "مقدار کالا معتبر نیست.");
            mappings.TryGetValue((item.ProductCode.GetValueOrDefault(), item.Unit), out var mapping);
            if (mapping?.GoodsId is not { Length: 13 } || !mapping.GoodsId.All(char.IsAsciiDigit)
                || string.IsNullOrWhiteSpace(mapping.TaxUnit))
                errors.Add(label + "شناسه کالا یا واحد مالیاتی معتبر ندارد.");
            if (item.UnitAmount <= 0 || quantity > 999999999999999m || item.UnitAmount > 999999999999999999m ||
                item.UnitAmount > decimal.MaxValue / Math.Max(1m, quantity))
            {
                errors.Add(label + "مقدار یا مبلغ خارج از محدوده است.");
                continue;
            }
            var baseAmount = quantity * item.UnitAmount;
            if (baseAmount > 999999999999999999m)
            {
                errors.Add(label + "مبلغ پایه بیش از حد مجاز است.");
                continue;
            }
            var net = baseAmount - item.Discount;
            if (item.UnitAmount <= 0 || item.Discount < 0 || net < 0 || item.TaxAmount < 0 ||
                factor.PercentTax < 0 || item.TaxAmount != net * factor.PercentTax / 100m ||
                item.TotalAmount != net + item.TaxAmount)
                errors.Add(label + "مبالغ یا مالیات نیازمند تطبیق است.");
            gross += baseAmount;
            discount += item.Discount;
            vat += item.TaxAmount;
            total += item.TotalAmount;
            lines.Add(new(item.Row, item.ProductCode, item.Product, quantity, mapping?.GoodsId, mapping?.TaxUnit,
                item.UnitAmount, item.Discount, item.TaxAmount, item.TotalAmount));
        }
        if (lines.Count == 0) errors.Add("فاکتور ردیف فعال ندارد.");
        if (gross != factor.SumTotalAmount || discount != factor.Discount || vat != factor.TotalTax || total != factor.PaymentAmount)
            errors.Add("جمع ردیف‌ها با مبالغ فاکتور یکسان نیست.");
        return new(factor.Id, source.ActiveMemory?.Id, source.ActiveMemory?.FiscalId, factor.DateMiladi,
            factor.Customer, factor.Cus_EghtesadiCode, factor.CurrencyType.ToString(), gross, discount, vat, total, lines, errors)
        {
            BuyerNationalId = factor.Cus_ShenaseMeli,
            BuyerPostalCode = factor.Cus_PostalCode,
            VatPercent = factor.PercentTax
        };
    }
}
