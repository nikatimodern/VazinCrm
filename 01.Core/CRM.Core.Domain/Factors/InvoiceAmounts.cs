using System;

namespace CRM.Core.Domain.Factors;

public static class InvoiceAmounts
{
    public static decimal Gross(decimal quantity, decimal unitPrice) => Math.Round(quantity * unitPrice);

    public static decimal Tax(decimal gross, decimal discount, int percent, bool afterDiscount) =>
        Math.Floor((afterDiscount ? Math.Max(0, gross - discount) : gross) * percent / 100m);
}
