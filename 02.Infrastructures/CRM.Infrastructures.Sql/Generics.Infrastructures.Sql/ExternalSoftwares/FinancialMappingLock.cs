using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.ExternalSoftwares;

internal static class FinancialMappingLock
{
    public static async Task<IAsyncDisposable> AcquireAsync(CRMContextDb db, Guid sourceId, CancellationToken ct)
    {
        var resource = $"SupplyFinancial:{sourceId:D}";
        await db.Database.OpenConnectionAsync(ct);
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = "DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=5000; SELECT @result;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@resource";
            parameter.Value = resource;
            command.Parameters.Add(parameter);
            if (Convert.ToInt32(await command.ExecuteScalarAsync(ct)) < 0)
                throw new InvalidOperationException("عملیات مالی این رکورد در حال انجام است.");
            return new FinancialLock(db, resource);
        }
        catch
        {
            await db.Database.CloseConnectionAsync();
            throw;
        }
    }

    private sealed class FinancialLock(CRMContextDb db, string resource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sys.sp_releaseapplock @Resource={resource}, @LockOwner='Session'");
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }
}
