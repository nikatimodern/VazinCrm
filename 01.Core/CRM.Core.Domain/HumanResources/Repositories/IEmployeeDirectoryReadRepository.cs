using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.HumanResources.Repositories;

public sealed record EmployeeDirectoryItem(Guid Id, string PersonnelCode, string FirstName, string LastName);
public sealed record EmploymentHistoryItem(Guid Id, DateOnly StartDate, DateOnly? EndDate, string EmploymentType);
public sealed record EmployeeAssignmentItem(Guid Id, Guid DepartmentId, Guid PositionId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsPrimary);

// Employee IDs must come from a server-validated permission/scope decision, never
// directly from request input. An empty set grants no access. No unrestricted read.
public interface IEmployeeDirectoryReadRepository
{
    Task<IReadOnlyList<EmployeeDirectoryItem>> SearchAsync(IReadOnlyCollection<Guid> authorizedEmployeeIds,
        string search, int offset, int limit, CancellationToken ct);
    Task<IReadOnlyList<EmploymentHistoryItem>> GetHistoryAsync(Guid employeeId,
        IReadOnlyCollection<Guid> authorizedEmployeeIds, int offset, int limit, CancellationToken ct);
    Task<IReadOnlyList<EmployeeAssignmentItem>> GetAssignmentsAsync(Guid employeeId,
        IReadOnlyCollection<Guid> authorizedEmployeeIds, int offset, int limit, CancellationToken ct);
}
