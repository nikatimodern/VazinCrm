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

    // Idempotent for the same invoice number, fiscal year and quotation.
    Task<int> CreateAsync(Factor factor, PreFactor preFactor, int quotationId,
        CancellationToken cancellationToken = default);
}
