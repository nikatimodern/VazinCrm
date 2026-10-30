using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Core.Domain.Reports.Models;
namespace CRM.Core.ApplicationServices.Reports.Queries.GetReportBaseItems;
public sealed record GetReportBaseItemsQuery() : IQuery<ReportBaseItems>;
public sealed class GetReportBaseItemsQueryHandler(IReportRepository repository) : IQueryHandler<GetReportBaseItemsQuery, ReportBaseItems>
{
    public async Task<ReportBaseItems> HandleAsync(GetReportBaseItemsQuery request, CancellationToken cancellationToken)
    {
        return await repository.GetBaseItemsAsync(cancellationToken);
    }
}
