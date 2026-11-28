using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.HumanResources.Repositories;

public interface IHrAccessScopeRepository
{
    Task<IReadOnlyList<Guid>> GetEmployeeIdsAsync(CancellationToken ct);
    Task<IReadOnlyList<string>> GetEmployeeAccountIdsAsync(CancellationToken ct);
}
