using System;
using System.Collections.Generic;
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

    Task<SepidarInvoiceExportPreview> PreviewDirectAsync(Factor factor, int saleTypeId,
        int customerPartyId, IReadOnlyList<(Guid FactorItemId, int ItemId, int StockId)> mappings,
        CancellationToken cancellationToken = default);


}
