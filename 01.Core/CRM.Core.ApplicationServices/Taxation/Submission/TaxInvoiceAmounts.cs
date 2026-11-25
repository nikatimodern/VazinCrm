using CRM.Core.ApplicationServices.Taxation.Dtos;

namespace CRM.Core.ApplicationServices.Taxation.Submission;

internal static class TaxInvoiceAmounts
{
    internal const decimal Maximum = 999999999999999999m;

    internal static bool IsRial(decimal value) => value >= 0 && value <= Maximum && value == decimal.Truncate(value);

    internal static string? ValidateLine(PreviewLine line, int rate, out decimal gross, out decimal net)
    {
        gross = net = 0;
        if (rate is < 0 or > 100 || line.Quantity <= 0 || line.Quantity > Maximum ||
            decimal.Round(line.Quantity, 8) != line.Quantity || line.UnitPrice <= 0 || line.UnitPrice > Maximum ||
            decimal.Round(line.UnitPrice, 8) != line.UnitPrice || line.Quantity > decimal.MaxValue / Math.Max(1, line.UnitPrice))
            return "مقدار، قیمت واحد یا نرخ مالیات خارج از دامنهٔ V7.9 است.";

        gross = line.Quantity * line.UnitPrice;
        if (gross == 0 || !IsRial(gross))
            return "حاصل دقیق ضرب مقدار در قیمت واحد، مبلغ ریالی معتبر V7.9 نیست.";
        net = gross - line.Discount;
        if (!IsRial(line.Discount) || !IsRial(net))
            return "تخفیف یا مبلغ پس از تخفیف با V7.9 مطابقت ندارد.";
        if (!IsRial(line.Vat) || line.Vat != net * rate / 100m)
            return "مالیات ثبت‌شده با حاصل دقیق اعمال نرخ بر مبلغ پس از تخفیف در V7.9 یکسان نیست.";
        if (!IsRial(line.Total) || line.Total != net + line.Vat)
            return "جمع ردیف با مبلغ پس از تخفیف و مالیات V7.9 مطابقت ندارد.";
        return null;
    }

    internal static bool TotalsMatch(InvoicePreview preview, decimal gross, decimal discount, decimal vat, decimal total) =>
        IsRial(preview.Gross) && IsRial(preview.Discount) && IsRial(preview.Vat) && IsRial(preview.Total) &&
        gross == preview.Gross && discount == preview.Discount && vat == preview.Vat && total == preview.Total;
}
