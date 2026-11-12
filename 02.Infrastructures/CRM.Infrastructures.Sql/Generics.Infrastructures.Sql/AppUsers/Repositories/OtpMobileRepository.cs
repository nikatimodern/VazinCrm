using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.AppUsers.Repositories;

public sealed class OtpMobileRepository(UserContextDb db) : IOtpMobileRepository
{
    public Task<OtpActionResult> IssueAsync(string mobile, string type, string code, CancellationToken ct) =>
        WithMobileLockAsync(mobile, true, state => state!.Issue(type, code, DateTime.UtcNow), ct);

    public Task<OtpActionResult> VerifyAsync(string mobile, string type, string? code, CancellationToken ct) =>
        WithMobileLockAsync(mobile, false, state =>
            state?.Verify(type, code, DateTime.UtcNow) ?? new(OtpActionStatus.Expired), ct);

    public async Task<OtpActionResult?> GetStatusAsync(string mobile, string type, CancellationToken ct)
    {
        var state = await db.Set<OtpMobileState>().AsNoTracking().SingleOrDefaultAsync(x => x.Mobile == mobile, ct);
        return state?.GetStatus(type, DateTime.UtcNow);
    }

    public Task CancelAsync(string mobile, string type, string code, CancellationToken ct) =>
        WithMobileLockAsync(mobile, false, state =>
        {
            state?.CancelIfMatches(type, code);
            return true;
        }, ct);

    public Task ResetLockoutAsync(string mobile, CancellationToken ct) =>
        WithMobileLockAsync(mobile, false, state =>
        {
            state?.ClearLockout();
            return true;
        }, ct);

    private async Task<T> WithMobileLockAsync<T>(
        string mobile, bool createIfMissing, Func<OtpMobileState?, T> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await AcquireLockAsync(db.Database.GetDbConnection(), transaction.GetDbTransaction(), mobile, ct);

        var state = await db.Set<OtpMobileState>().SingleOrDefaultAsync(x => x.Mobile == mobile, ct);
        if (state is null && createIfMissing)
        {
            state = new OtpMobileState { Mobile = mobile };
            db.Set<OtpMobileState>().Add(state);
        }

        var result = action(state);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    private static async Task AcquireLockAsync(DbConnection connection, DbTransaction transaction, string mobile, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock
                @Resource = @resource, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 10000;
            SELECT @lockResult;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.Value = "otp:" + mobile;
        command.Parameters.Add(parameter);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        if (result < 0)
            throw new InvalidOperationException("Unable to acquire the OTP mobile lock.");
    }
}
