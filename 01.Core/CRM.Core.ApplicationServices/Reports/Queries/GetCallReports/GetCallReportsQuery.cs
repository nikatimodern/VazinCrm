using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Dtos;
namespace CRM.Core.ApplicationServices.Reports.Queries.GetCallReports;
public sealed record GetCallReportsQuery(string MobileNumber) : IQuery<ReportPartialResult>;
public sealed class GetCallReportsQueryHandler(IReportRepository repository, IReportUserContext users, IReportPresentation presentation) : IQueryHandler<GetCallReportsQuery, ReportPartialResult>
{
    public async Task<ReportPartialResult> HandleAsync(GetCallReportsQuery request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(cancellationToken) ?? throw new UnauthorizedAccessException();
        var reports = await repository.GetByPhoneAsync(request.MobileNumber, cancellationToken);
        await repository.PopulateUserImagesAsync(reports, cancellationToken);
        var result = new ReportListResult(reports, 0, user.IsManager, user.User.Id);
        return new(reports.Count, await presentation.RenderAsync(result, cancellationToken));
    }
}
