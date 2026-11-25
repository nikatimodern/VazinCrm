namespace CRM.Core.ApplicationServices.Taxation.Submission;

public sealed record TaxSettlementShares(decimal Cash, decimal Credit);

internal static class TaxInvoiceSettlement
{
    internal static TaxSettlementShares? Validate(int method, decimal taxablePrincipal, TaxSettlementShares? shares)
    {
        if (method is not (1 or 2 or 3))
            throw new ArgumentException("روش تسویه باید نقدی، نسیه یا ترکیبی باشد.");
        if (method != 3)
        {
            if (shares is not null)
                throw new ArgumentException("درج هم‌زمان سهم نقدی و نسیه فقط برای تسویهٔ ترکیبی مجاز است.");
            return null;
        }
        if (shares is null || !TaxInvoiceAmounts.IsRial(shares.Cash) || !TaxInvoiceAmounts.IsRial(shares.Credit) ||
            shares.Cash <= 0 || shares.Credit <= 0)
            throw new ArgumentException("در تسویهٔ ترکیبی سهم نقدی و نسیه باید هر دو مبلغ ریالی صحیح و مثبت باشند.");
        if (shares.Cash + shares.Credit != taxablePrincipal)
            throw new ArgumentException("جمع سهم نقدی و نسیه باید با مبلغ پیش از مالیات و عوارض برابر باشد.");
        return shares;
    }
}
