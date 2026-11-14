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

public sealed class PasswordLockoutRepository(UserContextDb db) : IPasswordLockoutRepository
{
    public async Task<PasswordAttemptResult?> GetStatusAsync(string userId, CancellationToken ct)
    {
        var state = await db.PasswordLockoutStates.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, ct);
        return state?.GetStatus(DateTime.UtcNow);
    }

    public async Task<PasswordAttemptResult> RegisterAttemptAsync(
        string userId, bool passwordIsValid, CancellationToken ct)
    {
        // Retry the complete transaction to remain compatible with EnableRetryOnFailure.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            PasswordLockoutState? state = null;
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(ct);
                await AcquireLockAsync(db.Database.GetDbConnection(), transaction.GetDbTransaction(), userId, ct);

                state = await db.PasswordLockoutStates.SingleOrDefaultAsync(x => x.UserId == userId, ct);
                if (state is null)
                {
                    state = new PasswordLockoutState { UserId = userId };
                    db.PasswordLockoutStates.Add(state);
                }

                // Locked accounts remain locked even when a valid password is submitted.
                var result = state.RegisterAttempt(passwordIsValid, DateTime.UtcNow);
                await db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            finally
            {
                if (state is not null)
                    db.Entry(state).State = EntityState.Detached;
            }
        });
    }

    private static async Task AcquireLockAsync(
        DbConnection connection, DbTransaction transaction, string userId, CancellationToken ct)
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
        parameter.Value = "password:" + userId;
        command.Parameters.Add(parameter);

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        if (result < 0)
            throw new InvalidOperationException("Unable to acquire the password login lock.");
    }
}
