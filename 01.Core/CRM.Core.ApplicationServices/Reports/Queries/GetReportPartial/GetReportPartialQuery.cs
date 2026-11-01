using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Dtos;
using CRM.Core.Domain.Reports.Models;
using CRM.Core.ApplicationServices.Reports.Queries.GetAllReports;
namespace CRM.Core.ApplicationServices.Reports.Queries.GetReportPartial;
public sealed record GetReportPartialQuery(ReportSearch Filter, Guid? DealId = null) : IQuery<ReportPartialResult>;
public sealed class GetReportPartialQueryHandler(ICqrsDispatcher dispatcher, IReportPresentation presentation) : IQueryHandler<GetReportPartialQuery, ReportPartialResult>
{
    public async Task<ReportPartialResult> HandleAsync(GetReportPartialQuery request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.QueryAsync(new GetAllReportsQuery(request.Filter, request.DealId), cancellationToken);
        return new(result.Reports.Count, await presentation.RenderAsync(result, cancellationToken));
    }
}
