using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.Domain.Supplies.Repositories;
public interface ISupplyQueryRepository
{
    Task<SupplyResult> GetSupplyFactorAllocationsAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplyResult> SearchAllocationFactorItemsAsync(string search, Guid supplyFactorItemId, Guid? allocationId, CancellationToken cancellationToken);
    Task<SupplyResult> GetSalesFactorAllocationsAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplyResult> SearchAllocationSupplyItemsAsync(string search, Guid factorItemId, Guid? allocationId, CancellationToken cancellationToken);
    Task<SupplyResult> GetPaymentAsync(Guid? paymentId, CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplyPaymentsAsync(SupplyTableRequest request, CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplierFinancialSummaryAsync(Guid supplierId, CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplyFactorsAsync(string search, Guid? dealId, CancellationToken cancellationToken);
    Task<SupplyResult> SearchSupplyFactorsAsync(SupplySelection model, CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplyFactorAsync(Guid id, CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplyBaseDataAsync(CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplierOrdersAsync(CancellationToken cancellationToken);
    Task<SupplyResult> GetSuppliesAsync(Guid pfId, CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplyProfitAsync(SupplyTableRequest request, CancellationToken cancellationToken);
    Task<SupplyResult> GetSupplyDemandsAsync(SupplyTableRequest request, CancellationToken cancellationToken);
    Task<SupplyResult> GetReceivablesAsync(SupplyTableRequest request, CancellationToken cancellationToken);
    Task<SupplyResult> GetDealSuppliesAsync(Guid dealId, CancellationToken cancellationToken);
    Task<SupplyResult> GetOrderHeadsBySupplierAsync(Guid supplierId, CancellationToken cancellationToken);
}
