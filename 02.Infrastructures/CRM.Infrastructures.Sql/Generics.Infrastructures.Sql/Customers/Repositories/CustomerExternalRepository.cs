#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Customers.Repositories;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace CRM.Infrastructures.Sql.Customers.Repositories;

public sealed class CustomerExternalRepository(Sepidar1ContextDb db) : ICustomerExternalRepository
{
    public async Task<List<Invoice>> GetInvoicesAsync(int serialNumber, bool includeNotes, CancellationToken ct)
    {
        var invoices = await db.vwInvoice.AsNoTracking().Where(i => i.QuotationNumber == serialNumber).ToListAsync(ct);
        if (includeNotes)
        {
            var ids = invoices.Select(i => i.InvoiceId).ToArray(); var notes = await db.ExtraData.AsNoTracking().Where(n => n.EntityTypeName == "SG.Sales.InvoiceManagement.Common.DsInvoice" && ids.Contains(n.EntityRef)).ToListAsync(ct);
            foreach (var invoice in invoices) invoice.ExtraDescription = notes.FirstOrDefault(n => n.EntityRef == invoice.InvoiceId)?.Note;
        }
        return invoices;
    }
    public async Task<Dictionary<Guid, int>?> GetInvoiceCountsAsync(IReadOnlyList<PreFactor> preFactors, CancellationToken ct)
    {
        if (preFactors.Count == 0) return [];
        try
        {
            var serials = preFactors.Select(p => p.SerialNumber).ToArray(); var invoices = await db.vwInvoice.AsNoTracking().Where(i => i.QuotationNumber.HasValue && serials.Contains(i.QuotationNumber.Value)).Select(i => new { i.QuotationNumber, i.CreationDate }).ToListAsync(ct);
            return preFactors.ToDictionary(p => p.Id, p => invoices.Count(i => i.QuotationNumber == p.SerialNumber && i.CreationDate >= p.CreateDate));
        }
        catch (SqlException) { return null; }
    }
}
