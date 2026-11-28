using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Core.Domain.HumanResources.Repositories;

public sealed record EmployeeProfileDetail(Guid Id, string PersonnelCode, string FirstName, string LastName,
    bool HasAccount, byte[] RowVersion);
public sealed record EmployeeAccountCandidate(string Id, string FirstName, string LastName);
public sealed record CreateEmployeeProfileInput(string PersonnelCode, string FirstName, string LastName, string AppUserId);
public sealed record EditEmployeeProfileInput(Guid Id, string FirstName, string LastName, byte[] RowVersion);
public enum EmployeeSaveStatus { Saved, Invalid, Denied, NotFound, Conflict }
public sealed record EmployeeSaveResult(EmployeeSaveStatus Status, Guid? Id = null);

public interface IEmployeeProfileRepository
{
    Task<EmployeeProfileDetail> GetAsync(Guid id, IReadOnlyCollection<Guid> allowedIds, CancellationToken ct);
    Task<IReadOnlyList<EmployeeAccountCandidate>> GetCandidatesAsync(IReadOnlyCollection<string> allowedAccountIds, CancellationToken ct);
    Task<EmployeeSaveResult> CreateAsync(CreateEmployeeProfileInput input, string actorId,
        IReadOnlyCollection<string> allowedAccountIds, CancellationToken ct);
    Task<EmployeeSaveResult> EditAsync(EditEmployeeProfileInput input, string actorId,
        IReadOnlyCollection<Guid> allowedIds, CancellationToken ct);
}
