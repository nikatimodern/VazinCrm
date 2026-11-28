namespace CRM.Core.ApplicationServices.HumanResources.Abstractions;

// This boundary must be implemented by the current Access Catalog adapter.
// Never accept these fields from HTTP input or infer them from an admin role.
public sealed record HrEmployeeAccess(string ActorId, bool CanView, bool CanCreate,
    IReadOnlyCollection<Guid> ViewEmployeeIds, IReadOnlyCollection<Guid> EditEmployeeIds,
    IReadOnlyCollection<Guid> ContractViewEmployeeIds, IReadOnlyCollection<string> ImportAccountIds);

public interface IHrEmployeeAccess
{
    Task<HrEmployeeAccess> GetAsync(CancellationToken ct);
}

// This branch has not integrated AccLevel yet. The default adapter deliberately
// grants no permissions; changing configuration alone cannot bypass the gate.
public sealed class LockedHrEmployeeAccess : IHrEmployeeAccess
{
    public Task<HrEmployeeAccess> GetAsync(CancellationToken ct) =>
        Task.FromResult(new HrEmployeeAccess("", false, false, [], [], [], []));
}
