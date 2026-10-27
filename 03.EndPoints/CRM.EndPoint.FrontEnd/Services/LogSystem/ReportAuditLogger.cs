#nullable enable
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Reports.Commands.AddReport;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Reports.Entities;

namespace CRM.EndPoint.FrontEnd.Services.LogSystem;

public sealed class ReportAuditLogger(LogSystemService logSystemService) : IReportAuditLogger
{
    public async Task LogAsync(Report original, Report? updated, AppUser user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await logSystemService.LogChange(original, updated,
            updated is null ? LogActionEnum.ثبت : LogActionEnum.ویرایش, user);
    }
}
