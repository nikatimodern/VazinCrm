using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Dtos;
using CRM.Core.Domain.Reports.Models;
using CRM.Core.ApplicationServices.Reports.Queries.GetAllReports;
namespace CRM.Core.ApplicationServices.Reports.Queries.ExportReports;
public sealed record ExportReportsQuery(ReportSearch Filter) : IQuery<ReportExportFile>;
public sealed class ExportReportsQueryHandler(ICqrsDispatcher dispatcher, IReportPresentation presentation) : IQueryHandler<ExportReportsQuery, ReportExportFile>
{
    public async Task<ReportExportFile> HandleAsync(ExportReportsQuery request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.QueryAsync(new GetAllReportsQuery(request.Filter, IncludeImages: false), cancellationToken);
        return presentation.Export(result.Reports, request.Filter.Type);
    }
}
