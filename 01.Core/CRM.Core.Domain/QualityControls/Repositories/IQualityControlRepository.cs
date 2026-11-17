#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.QualityControls.Models;

namespace CRM.Core.Domain.QualityControls.Repositories;

public interface IQualityControlRepository
{
    Task<QualityControlCustomerListResult> GetCustomersAsync(
        QualityControlCustomerListRequest request, QualityControlAccess access, CancellationToken cancellationToken);
    Task<QualityControlCustomerForm?> GetCustomerFormAsync(
        Guid customerId, QualityControlAccess access, CancellationToken cancellationToken);
    Task<SaveQualityControlItemsResult> SaveItemsAsync(
        SaveQualityControlItemsRequest request, QualityControlAccess access, string persianNow, CancellationToken cancellationToken);
}
