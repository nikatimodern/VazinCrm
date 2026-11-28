using CRM.Core.Domain.Taxation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Data.Common;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

internal static class TaxDispatchEventWriter
{
    internal static async Task<bool> RecordAsync(CRMContextDb db, Guid dispatchId, Guid attemptId,
        TaxDispatchEventKind kind, string result, Func<Task<int>> update, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        if (await update() != 1)
            return false;
        var entry = db.TaxDispatchEvents.Add(new TaxDispatchEvent
        {
            DispatchId = dispatchId, AttemptId = attemptId, Kind = kind,
            Result = result, OccurredAt = DateTimeOffset.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is DbException sql)
        {
            ExceptionDispatchInfo.Throw(sql);
        }
        finally
        {
            entry.State = EntityState.Detached;
        }
        return true;
    }
}
