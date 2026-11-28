namespace CRM.Core.ApplicationServices.HumanResources.Abstractions;

// Server-resolved permissions and employee scopes. The temporary senior-admin
// adapter will be replaced by Access Catalog integration. Never bind from HTTP input.
public sealed record HrEmployeeAccess(string ActorId, bool CanView, bool CanCreate,
    IReadOnlyCollection<Guid> ViewEmployeeIds, IReadOnlyCollection<Guid> EditEmployeeIds,
    IReadOnlyCollection<Guid> ContractViewEmployeeIds, IReadOnlyCollection<string> ImportAccountIds,
    IReadOnlyCollection<Guid> ContractCreateEmployeeIds, IReadOnlyCollection<Guid> ContractEditEmployeeIds,
    IReadOnlyCollection<Guid> ContractApproveEmployeeIds, IReadOnlyCollection<Guid> ContractTerminateEmployeeIds,
    bool CanUploadEducationOnCreate, IReadOnlyCollection<Guid> EducationDocumentViewIds,
    IReadOnlyCollection<Guid> EducationDocumentEditIds)
{
    public static HrEmployeeAccess Denied { get; } =
        new("", false, false, [], [], [], [], [], [], [], [], false, [], []);
}

public interface IHrEmployeeAccess
{
    Task<HrEmployeeAccess> GetAsync(CancellationToken ct);
}

