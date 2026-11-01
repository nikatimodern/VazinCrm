#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
namespace CRM.EndPoint.FrontEnd.Services.Reports;
public sealed class ReportUserContext(IHttpContextAccessor accessor, UserManager<AppUser> users) : IReportUserContext
{
    public async Task<ReportUser?> GetAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var principal = accessor.HttpContext?.User;
        if (principal is null) return null;
        var user = await users.GetUserAsync(principal);
        if (user is null) return null;
        var roles = await users.GetRolesAsync(user);
        var canEdit = roles.Contains("مدیر فروش") || roles.Contains("مدیر ارشد");
        return new(user, canEdit, canEdit || roles.Contains("مدیر عامل") || roles.Contains("مدیر اجرایی"));
    }
}
public sealed class ReportNotifications(IHubContext<TaskHub> hub) : IReportNotifications
{
    public Task ChangedAsync(string userId, CancellationToken cancellationToken) =>
        hub.Clients.AllExcept(TaskHub.GetConnectionId(userId)).SendAsync("ShowReport", cancellationToken);
}
public sealed class ReportCallAudio(IssabelService service) : IReportCallAudio
{
    public async Task<Stream?> OpenAsync(string path, CancellationToken cancellationToken) =>
        await service.GetAudioWithRetry(path, cancellationToken);
}
public sealed class ReportPresentation(IViewRenderService renderer) : IReportPresentation
{
    public Task<string> RenderAsync(ReportListResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return renderer.RenderToStringAsync("GetAllReportPartial", result.Reports, new Dictionary<string, object>
        {
            ["PassType"] = result.PassType, ["IsManager"] = result.IsManager, ["CurrentUserId"] = result.CurrentUserId
        });
    }
    public ReportExportFile Export(List<Report> data, int reportType)
    {
        var columns = new List<ExcelColumnConfig<Report>> {
            new() { Header = "تاریخ", ValueSelector = x => TextService.ToEnglishNumber($"{x.RegDateTxt} {x.RegTimeTxt}") },
            new() { Header = "نوع", ValueSelector = x => x.ReportTypeTxt },
            new() { Header = "مشتری", ValueSelector = x => x.CustomerTxt },
            new() { Header = "تلفن", ValueSelector = x => x.RepTel },
            new() { Header = "متن گزارش", ValueSelector = x => x.ReportText },
            new() { Header = "کارشناس فروش", ValueSelector = x => x.UserCreator }
        };

        if (reportType == (int)ReportTypeEnum.گزارش_پیگیری)
        {
            columns.Add(new() { Header = "نتیجه تماس", ValueSelector = x => x.ResultCallTxt });
            columns.Add(new() { Header = "نتیجه نهایی", ValueSelector = x => x.FinalResultCallTxt });
            columns.Add(new() { Header = "علت انصراف", ValueSelector = x => x.ReasonCancelTxt });
        }

        var file = ExcelExportService.ExportToExcel(data, columns, "Reports");
        return new(file.FileContents, file.ContentType, file.FileDownloadName);
    }
}
