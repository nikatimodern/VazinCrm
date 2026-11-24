using System;

namespace CRM.Core.Domain.Taxation;

public sealed class TaxMemory
{
    public Guid Id { get; set; }
    public string FiscalId { get; set; }
    public string Title { get; set; }
    public bool IsActive { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class TaxProductMapping
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Unit { get; set; }
    public string GoodsId { get; set; }
    public string TaxUnit { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class TaxInvoiceSnapshot
{
    public Guid Id { get; set; }
    public Guid FactorId { get; set; }
    public Guid TaxMemoryId { get; set; }
    public DateTime CreatedUtc { get; set; }
    public string CreatedBy { get; set; }
    public string ProtectedPreview { get; set; }
}
