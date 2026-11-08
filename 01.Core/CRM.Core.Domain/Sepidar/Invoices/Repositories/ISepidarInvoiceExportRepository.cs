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

    // Idempotent by the originating CRM Factor GUID; Sepidar assigns its own number on save.
    Task<int> CreateAsync(Factor factor, PreFactor preFactor, int quotationId,
        CancellationToken cancellationToken = default);
}
