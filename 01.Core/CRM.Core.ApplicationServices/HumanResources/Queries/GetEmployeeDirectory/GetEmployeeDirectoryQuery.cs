using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Abstractions;
using CRM.Core.Domain.HumanResources.Repositories;
using CRM.Core.ApplicationServices.HumanResources.Services;

namespace CRM.Core.ApplicationServices.HumanResources.Queries.GetEmployeeDirectory;

public sealed record GetEmployeeDirectoryQuery(string Search, int Page, Guid? EmployeeId, int ContractPage = 1)
    : IQuery<EmployeeDirectoryPage>;

public sealed record EmployeeDirectoryPage(bool CanView, bool CanCreate, bool CanEdit, bool CanViewContracts,
    string Search, int Page, bool HasNextPage, IReadOnlyList<EmployeeDirectoryItem> Employees,
    EmployeeProfileDetail Selected, IReadOnlyList<EmployeeAccountCandidate> Candidates,
    IReadOnlyList<EmploymentContractItem> Contracts, bool CanCreateContract, bool CanEditContract,
    bool CanApproveContract, bool CanTerminateContract, DateOnly? BusinessDate, int ContractPage, bool HasNextContractPage,
    bool CanUploadEducationOnCreate, bool CanViewEducationDocument, bool CanEditEducationDocument);

public sealed class GetEmployeeDirectoryQueryHandler(IHrEmployeeAccess access,
    IEmployeeDirectoryReadRepository directory, IEmployeeProfileRepository profiles,
    IEmploymentContractReadRepository contracts)
    : IQueryHandler<GetEmployeeDirectoryQuery, EmployeeDirectoryPage>
{
    private const int PageSize = 20;

    public async Task<EmployeeDirectoryPage> HandleAsync(GetEmployeeDirectoryQuery query, CancellationToken ct)
    {
        var rights = await access.GetAsync(ct);
        var search = query.Search?.Trim() ?? "";
        if (search.Length > 100) search = search[..100];
        var page = Math.Clamp(query.Page, 1, 100000);
        if (!rights.CanView || string.IsNullOrWhiteSpace(rights.ActorId))
            return new(false, false, false, false, "", 1, false, [], null, [], [], false, false, false, false, null, 1, false, false, false, false);

        var rows = await directory.SearchAsync(rights.ViewEmployeeIds, search, (page - 1) * PageSize, PageSize + 1, ct);
        var selected = query.EmployeeId.HasValue
            ? await profiles.GetAsync(query.EmployeeId.Value, rights.ViewEmployeeIds, ct) : null;
        var canEdit = selected != null && rights.EditEmployeeIds.Contains(selected.Id);
        var canViewContracts = selected != null && rights.ContractViewEmployeeIds.Contains(selected.Id);
        var contractPage = Math.Clamp(query.ContractPage, 1, 100000);
        var contractRows = canViewContracts
            ? await contracts.GetEmployeeContractsAsync(selected.Id, rights.ContractViewEmployeeIds, (contractPage - 1) * PageSize, PageSize + 1, ct)
            : Array.Empty<EmploymentContractItem>();
        var candidates = rights.CanCreate
            ? await profiles.GetCandidatesAsync(rights.ImportAccountIds, ct)
            : Array.Empty<EmployeeAccountCandidate>();
        return new(true, rights.CanCreate, canEdit, canViewContracts, search, page, rows.Count > PageSize,
            rows.Take(PageSize).ToArray(), selected, candidates, contractRows.Take(PageSize).ToArray(),
            canViewContracts && rights.ContractCreateEmployeeIds.Contains(selected.Id),
            canViewContracts && rights.ContractEditEmployeeIds.Contains(selected.Id),
            canViewContracts && rights.ContractApproveEmployeeIds.Contains(selected.Id),
            canViewContracts && rights.ContractTerminateEmployeeIds.Contains(selected.Id),
            canViewContracts ? HrBusinessDate.Today() : null, contractPage, contractRows.Count > PageSize,
            rights.CanCreate && rights.CanUploadEducationOnCreate,
            selected != null && rights.EducationDocumentViewIds.Contains(selected.Id),
            canEdit && rights.EducationDocumentEditIds.Contains(selected.Id));
    }
}
