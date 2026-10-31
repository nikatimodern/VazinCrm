#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Attachments.Entities;

namespace CRM.Core.Domain.Transfers.Repositories;
public sealed record TransferTableRequest(string? Draw, int Start, int Length, string? Search, int SearchNumber, string? SortColumn, bool Ascending, int? Status);
public sealed record TransferTableResult(string? Draw, int RecordsTotal, int RecordsFiltered, List<Transfer> Data, Dictionary<int, int> FilterCounts);
public sealed record TransferDetails(Transfer Transfer, List<Attachment> Attachments);
public interface ITransferRepository
{
    Task<TransferTableResult> GetTableAsync(TransferTableRequest request, CancellationToken cancellationToken);
    Task<TransferDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
    Task<Transfer?> GetAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task SaveAsync(Transfer transfer, bool create, CancellationToken cancellationToken);
    void SaveAttachments(IList<Attachment> attachments);
    Task BeginAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
