using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Abstractions;
using CRM.Core.Domain.HumanResources.Repositories;

namespace CRM.Core.ApplicationServices.HumanResources.Queries.GetEducationDocument;

public sealed record GetEducationDocumentQuery(Guid EmployeeId) : IQuery<EmployeeEducationDocument>;

public sealed class GetEducationDocumentQueryHandler(IHrEmployeeAccess access, IEmployeeProfileRepository repository)
    : IQueryHandler<GetEducationDocumentQuery, EmployeeEducationDocument>
{
    public async Task<EmployeeEducationDocument> HandleAsync(GetEducationDocumentQuery query, CancellationToken ct)
    {
        var rights = await access.GetAsync(ct);
        if (!rights.CanView || string.IsNullOrWhiteSpace(rights.ActorId) ||
            !rights.ViewEmployeeIds.Contains(query.EmployeeId) ||
            !rights.EducationDocumentViewIds.Contains(query.EmployeeId)) return null;
        return await repository.GetEducationDocumentAsync(query.EmployeeId, rights.EducationDocumentViewIds, ct);
    }
}
