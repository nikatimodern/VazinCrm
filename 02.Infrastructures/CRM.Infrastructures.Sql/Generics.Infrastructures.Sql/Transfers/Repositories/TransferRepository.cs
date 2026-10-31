#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Transfers.Entities;
using CRM.Core.Domain.Transfers.Enums;
using CRM.Core.Domain.Transfers.Repositories;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.Transfers.Repositories;
public sealed class TransferRepository(CRMContextDb db) : ITransferRepository, IAsyncDisposable
{
    private IDbContextTransaction? transaction;
    public async Task<TransferTableResult> GetTableAsync(TransferTableRequest request, CancellationToken ct)
    {
        var query = db.Transfers.AsNoTracking();
        var total = await query.CountAsync(ct);
        if (!string.IsNullOrEmpty(request.Search))
            query = query.Where(t => t.TransferNumber == request.SearchNumber || t.Description.Contains(request.Search) || t.ToAccountTxt.Contains(request.Search) || t.FromAccountTxt.Contains(request.Search));
        var grouped = await query.GroupBy(t => t.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Status, g => g.Count, ct);
        var counts = new Dictionary<int, int>
        {
            [-1] = grouped.Values.Sum(),
            [1] = grouped.GetValueOrDefault(TransferStatusEnum.در_دست_تایید) + grouped.GetValueOrDefault(TransferStatusEnum.ثبت_اولیه),
            [2] = grouped.GetValueOrDefault(TransferStatusEnum.تایید_شده),
            [3] = grouped.GetValueOrDefault(TransferStatusEnum.رد_شده)
        };
        query = request.Status switch
        {
            1 => query.Where(t => t.Status == TransferStatusEnum.در_دست_تایید),
            2 => query.Where(t => t.Status == TransferStatusEnum.تایید_شده),
            3 => query.Where(t => t.Status == TransferStatusEnum.رد_شده),
            _ => query
        };
        var filtered = await query.CountAsync(ct);
        query = (request.SortColumn, request.Ascending) switch
        {
            ("transferNumber", true) => query.OrderBy(t => t.TransferNumber),
            ("transferNumber", false) => query.OrderByDescending(t => t.TransferNumber),
            ("amount", true) => query.OrderBy(t => t.Amount),
            ("amount", false) => query.OrderByDescending(t => t.Amount),
            ("fromAccountTxt", true) => query.OrderBy(t => t.FromAccountTxt),
            ("fromAccountTxt", false) => query.OrderByDescending(t => t.FromAccountTxt),
            ("toAccountTxt", true) => query.OrderBy(t => t.ToAccountTxt),
            ("toAccountTxt", false) => query.OrderByDescending(t => t.ToAccountTxt),
            ("transferDate", true) => query.OrderBy(t => t.TransferDateMiladi),
            ("transferDate", false) => query.OrderByDescending(t => t.TransferDateMiladi),
            ("createDateTxt", true) => query.OrderBy(t => t.CreateDate),
            _ => query.OrderByDescending(t => t.CreateDate)};
        if (request.Length != -1)
            query = query.Skip(request.Start).Take(request.Length);
        return new(request.Draw, total, filtered, await query.ToListAsync(ct), counts);
    }

    public Task<TransferDetails?> GetDetailsAsync(Guid id, CancellationToken ct) => db.Transfers.AsNoTracking().Where(t => t.Id == id).Select(t => new TransferDetails(t, db.Attachments.Where(a => a.EntityId == t.Id && a._AttachType == _AttachType.transfer).ToList())).FirstOrDefaultAsync(ct);
    public Task<Transfer?> GetAsync(Guid id, bool tracking, CancellationToken ct) => (tracking ? db.Transfers : db.Transfers.AsNoTracking()).FirstOrDefaultAsync(t => t.Id == id, ct);
    public async Task SaveAsync(Transfer transfer, bool create, CancellationToken ct)
    {
        if (create)
            db.Transfers.Add(transfer);
        else
        {
            var current = await db.Transfers.FirstAsync(t => t.Id == transfer.Id, ct);
            db.Entry(current).CurrentValues.SetValues(transfer);
            db.Entry(current).Property(t => t.TransferNumber).IsModified = false;
        }
    }

    public void SaveAttachments(IList<Attachment> attachments) => db.Attachments.UpdateRange(attachments);
    public async Task BeginAsync(CancellationToken ct)
    {
        transaction = await db.Database.BeginTransactionAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    public async Task CommitAsync(CancellationToken ct)
    {
        if (transaction != null)
            await transaction.CommitAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (transaction != null)
            await transaction.DisposeAsync();
    }
}
