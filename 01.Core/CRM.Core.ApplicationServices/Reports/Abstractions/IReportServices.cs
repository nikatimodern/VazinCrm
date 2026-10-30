using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.ApplicationServices.Reports.Dtos;
namespace CRM.Core.ApplicationServices.Reports.Abstractions;
public sealed record ReportUser(AppUser User, bool CanEditOthers, bool IsManager);
public interface IReportUserContext { Task<ReportUser?> GetAsync(CancellationToken cancellationToken); }
public interface IReportNotifications { Task ChangedAsync(string userId, CancellationToken cancellationToken); }
public interface IReportPresentation
{
    Task<string> RenderAsync(ReportListResult result, CancellationToken cancellationToken);
    ReportExportFile Export(List<Report> reports, int reportType);
}
public interface IReportCallAudio { Task<Stream?> OpenAsync(string path, CancellationToken cancellationToken); }
