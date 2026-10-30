using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Reports.Entities;

namespace CRM.Core.ApplicationServices.Reports.Abstractions;

public interface IReportAuditLogger
{
    // Stage the audit entry in the same scope; the repository commits it with the report.
    Task DeleteAsync(Report report, AppUser user, CancellationToken cancellationToken);
    Task LogAsync(Report original, Report? updated, AppUser user, CancellationToken cancellationToken);
}
