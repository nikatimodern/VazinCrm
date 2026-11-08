using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Models;

namespace CRM.Core.Domain.Sepidar.Invoices.Repositories;

public interface ISepidarInvoiceExportRepository
{
    Task<SepidarInvoiceExportPreview> PreviewAsync(Factor factor, PreFactor preFactor, int quotationId,
        CancellationToken cancellationToken = default);

    // Sepidar assigns the visible invoice number on save; the caller must check ExternalMapping to avoid duplicates.
    Task<int> CreateAsync(Factor factor, PreFactor preFactor, int quotationId,
        CancellationToken cancellationToken = default);
}
