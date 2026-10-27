using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Repositories;

namespace CRM.Core.ApplicationServices.Reports.Queries.GetReport;

public sealed class GetReportQueryHandler : IQueryHandler<GetReportQuery, Report?>
{
    private readonly IReportRepository _reports;

    public GetReportQueryHandler(IReportRepository reports)
    {
        _reports = reports;
    }

    public Task<Report?> HandleAsync(GetReportQuery query, CancellationToken cancellationToken)
    {
        return _reports.GetByIdAsync(query.Id, cancellationToken);
    }
}
