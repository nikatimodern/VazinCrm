using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Dtos;
using CRM.Core.Domain.Reports.Models;
namespace CRM.Core.ApplicationServices.Reports.Queries.GetAllReports;
public sealed record GetAllReportsQuery(ReportSearch Filter, Guid? DealId = null, bool IncludeImages = true) : IQuery<ReportListResult>;
public sealed class GetAllReportsQueryHandler(IReportRepository repository, IReportUserContext users) : IQueryHandler<GetAllReportsQuery, ReportListResult>
{
    public async Task<ReportListResult> HandleAsync(GetAllReportsQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(cancellationToken) ?? throw new UnauthorizedAccessException();
        var reports = await repository.SearchAsync(request.Filter, request.DealId, user.User.Id, user.CanEditOthers, cancellationToken);
        if (request.IncludeImages) await repository.PopulateUserImagesAsync(reports, cancellationToken);
        return new(reports, request.DealId.HasValue ? 4 : 0, user.IsManager, user.User.Id);
    }
}
