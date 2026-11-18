using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Supplies.Repositories;

public sealed class SupplyFinancialRepository(CRMContextDb db) : ISupplyFinancialRepository
{
    public Task<SupplyFactor> GetSourceAsync(Guid id, CancellationToken ct) =>
        db.SupplyFactors.AsNoTracking().Include(x => x.Items.OrderBy(i => i.Id))
            .SingleOrDefaultAsync(x => x.Id == id && db.Customers.Any(c => c.Id == x.SupplierId
                && (c.RoleType == RoleTypeEnum.تامین_کننده || c.RoleType == RoleTypeEnum.تامین_کننده_مشتری)), ct);

    public async Task<IReadOnlyList<ExternalSoftware>> GetDestinationsAsync(CancellationToken ct) =>
        await db.ExternalSoftwares.AsNoTracking().OrderBy(x => x.Title)
            .Select(x => new ExternalSoftware { Id = x.Id, Title = x.Title }).ToListAsync(ct);

    public async Task<IReadOnlyList<ExternalMapping>> GetMappingsAsync(Guid sourceId, CancellationToken ct) =>
        await db.ExternalMappings.AsNoTracking().Where(x => x.EntityId == sourceId
            && x.EntityName == nameof(SupplyFactor)
            && (x.MapType == ExternalMappingTypeEnum.فاکتور_خرید || x.MapType == ExternalMappingTypeEnum.رسید_انبار))
            .ToListAsync(ct);

    public Task<ExternalMapping> GetMappingAsync(Guid sourceId, Guid destinationId,
        ExternalMappingTypeEnum type, CancellationToken ct) =>
        db.ExternalMappings.SingleOrDefaultAsync(x => x.EntityId == sourceId && x.ExternalSoftwareId == destinationId
            && x.MapType == type && x.EntityName == (type == ExternalMappingTypeEnum.تامین_کننده ? "Customer" : nameof(SupplyFactor)), ct);

    public async Task SaveMappingAsync(ExternalMapping mapping, CancellationToken ct)
    {
        if (db.Entry(mapping).State == EntityState.Detached)
            db.ExternalMappings.Add(mapping);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteMappingAsync(ExternalMapping mapping, CancellationToken ct)
    {
        db.ExternalMappings.Remove(mapping);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IAsyncDisposable> LockAsync(Guid sourceId, CancellationToken ct)
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
                throw new InvalidOperationException("عملیات مالی این فاکتور در حال انجام است.");
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
