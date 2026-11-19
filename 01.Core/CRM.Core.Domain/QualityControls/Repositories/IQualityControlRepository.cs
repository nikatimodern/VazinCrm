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
    Task<QualityControlCustomerNotesPage?> GetCustomerNotesAsync(
        Guid customerId, int start, int length, QualityControlAccess access, CancellationToken cancellationToken);
    Task<AddQualityControlCustomerNoteResult> AddCustomerNoteAsync(
        AddQualityControlCustomerNoteRequest request, QualityControlAccess access,
        string persianNow, CancellationToken cancellationToken);
    Task<SaveQualityControlItemsResult> SaveItemsAsync(
        SaveQualityControlItemsRequest request, QualityControlAccess access, string persianNow, CancellationToken cancellationToken);
    Task<SetQualityControlCustomerContactResult> SetCustomerContactStatusAsync(
        SetQualityControlCustomerContactRequest request, QualityControlAccess access, string persianNow, CancellationToken cancellationToken);
    Task<QualityControlContactHistoryPage?> GetContactHistoryAsync(
        Guid customerId, bool includeDeleted, int start, int length,
        QualityControlAccess access, bool canManage, CancellationToken cancellationToken);
    Task<ChangeQualityControlContactHistoryResult> ChangeContactHistoryDeletionAsync(
        ChangeQualityControlContactHistoryRequest request, QualityControlAccess access,
        string persianNow, CancellationToken cancellationToken);
}
