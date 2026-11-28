using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.HumanResources.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.HumanResources.Repositories;

// Internal scope source for the authorized HR access adapter, not a public query.
public sealed class HrAccessScopeRepository(CRMContextDb db, UserContextDb users) : IHrAccessScopeRepository
{
    public async Task<IReadOnlyList<Guid>> GetEmployeeIdsAsync(CancellationToken ct) =>
        await db.HrEmployeeProfiles.AsNoTracking().Where(x => !x.IsDeleted)
            .Select(x => x.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetEmployeeAccountIdsAsync(CancellationToken ct) =>
        await users.Users.AsNoTracking().Where(x => x.IsEmployee)
            .Select(x => x.Id).ToListAsync(ct);
}
