using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Reports.Queries;
using CRM.Core.ApplicationServices.Reports.Commands;
using CRM.Core.ApplicationServices.Reports.Dtos;
using System.Threading;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using Microsoft.AspNetCore.SignalR;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Infrastructures.Sql;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class ReportController : Controller {
        private readonly IHubContext<TaskHub> hub;
        private readonly UserManager<AppUser> userManager;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly IViewRenderService vrs;
        private readonly LogSystemService _logSystemService;
        private readonly IssabelService issabelService;
        private readonly IQueryHandler<GetReportQuery, Report> getReportHandler;
        private readonly ICommandHandler<AddReportCommand, AddReportResult> addReportHandler;

        public ReportController(IHubContext<TaskHub> hub, UserManager<AppUser> userManager, CRMContextDb db,
            UserContextDb udb, IViewRenderService vrs, LogSystemService logSystemService,
            IssabelService issabelService, IQueryHandler<GetReportQuery, Report> getReportHandler,
            ICommandHandler<AddReportCommand, AddReportResult> addReportHandler)
        {
            this.hub = hub;
            this.userManager = userManager;
            this.db = db;
            this.udb = udb;
            this.vrs = vrs;
            _logSystemService = logSystemService;
            this.issabelService = issabelService;
            this.getReportHandler = getReportHandler;
            this.addReportHandler = addReportHandler;
        }

        public IActionResult Index()
        {
            return View();
        }


        public async Task<IActionResult> GetBaseItemsReport()
        {
            try
            {
                var items = await db.BaseItems
                    .Where(f =>
                        f.TypeCode == BaseItemEnum.نتیجه_تماس
                        || f.TypeCode == BaseItemEnum.نتیجه_نهایی
                        || f.TypeCode == BaseItemEnum.علت_انصراف
                    )
                    .ToListAsync();

                var result = new {
                    ResultCall = items
                        .Where(x => x.TypeCode == BaseItemEnum.نتیجه_تماس)
                        .Select(x => new { Value = x.Id, Text = x.Title })
                        .ToList(),

                    FinalResultCall = items
                        .Where(x => x.TypeCode == BaseItemEnum.نتیجه_نهایی)
                        .Select(x => new { Value = x.Id, Text = x.Title })
                        .ToList(),

                    ReasonCancel = items
                        .Where(x => x.TypeCode == BaseItemEnum.علت_انصراف)
                        .Select(x => new { Value = x.Id, Text = x.Title })
                        .ToList()
                };

                return Ok(result);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ExportExcelReport([FromBody] ReportFilterVModel model)
        {
            try
            {
                var data = await GetAllReports(model);


                var columns = new List<ExcelColumnConfig<Report>> {
                    new() { Header = "تاریخ", ValueSelector = x => TextService.ToEnglishNumber($"{x.RegDateTxt} {x.RegTimeTxt}") },
                    new() { Header = "نوع", ValueSelector = x => x.ReportTypeTxt },
                    new() { Header = "مشتری", ValueSelector = x => x.CustomerTxt },
                    new() { Header = "تلفن", ValueSelector = x => x.RepTel },
                    new() { Header = "متن گزارش", ValueSelector = x => x.ReportText },
                    new() { Header = "کارشناس فروش", ValueSelector = x => x.UserCreator }
                };

                if (model._type == (int)ReportTypeEnum.گزارش_پیگیری)
                {
                    columns.Add(new() { Header = "نتیجه تماس", ValueSelector = x => x.ResultCallTxt });
                    columns.Add(new() { Header = "نتیجه نهایی", ValueSelector = x => x.FinalResultCallTxt });
                    columns.Add(new() { Header = "علت انصراف", ValueSelector = x => x.ReasonCancelTxt });
                }

                return ExcelExportService.ExportToExcel(data, columns, "Reports");
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        public async Task<IActionResult> DelReport(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);
                var rp = await db.Reports.Where(f => f.Id == id).FirstOrDefaultAsync();

                if (rols.Contains("مدیر فروش") || rols.Contains("مدیر ارشد") || rp.UserCreatorID == user.Id)
                {
                    await _logSystemService.LogChange(rp, null, LogActionEnum.حذف, user);

                    rp.IsDeleted = true;
                    rp.UserModify = user.FullName;
                    rp.UserModifyID = user.Id;

                    await db.SaveChangesAsync();
                    await hub.Clients.AllExcept(TaskHub.GetConnectionId(user.Id)).SendAsync("ShowReport");
                    return Ok();
                }

                return BadRequest("شما مجاز به حذف این گزارش نیستید!");
            }
            catch
            {
                return BadRequest();
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetReport(Guid Id, CancellationToken cancellationToken)
        {
            var rep = await getReportHandler.HandleAsync(new GetReportQuery(Id), cancellationToken);
            return Ok(rep);
        }


        [HttpPost]
        public async Task<IActionResult> GetAllReportPartial([FromBody] ReportFilterVModel model, [FromQuery] Guid? dealId = null)
        {
            try
            {
                var data = await GetAllReports(model, dealId);

                data = await GenerateImage(data);

                var html = await vrs.RenderToStringAsync("GetAllReportPartial", data,
                    new Dictionary<string, object> {
                        { "PassType", ViewBag.PassType },
                        { "IsManager", ViewBag.IsManager },
                        { "CurrentUserId", ViewBag.CurrentUserId },
                    });

                return Ok(new { count = data.Count, html });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public async Task<IActionResult> GetCallReport(string mobileNumber)
        {
            try
            {
                var qData = db.Reports.AsNoTracking()
                    .Where(f => f.RepTel == mobileNumber)
                    .Include(r => r.CallRecording.Participants)
                    .OrderByDescending(r => r.CreateDate)
                    .AsQueryable();


                var data = await qData.ToListAsync();

                data = await GenerateImage(data);

                var isManager = User.IsAdmin() || User.IsSalesAdmin() || User.IsManagingDirector() || User.IsExecutiveDirector();
                var currentUserId = User.GetUserId();

                var html = await vrs.RenderToStringAsync("GetAllReportPartial", data,
                    new Dictionary<string, object> {
                        { "PassType", 0 },
                        { "IsManager", isManager },
                        { "CurrentUserId", currentUserId },
                    });

                return Ok(new { count = data.Count, html });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task<List<Report>> GenerateImage(List<Report> qData)
        {
            var userIds = qData.Select(r => r.UserCreatorID)
                .Union(qData.Where(r => r.CallRecording != null).SelectMany(r => r.CallRecording.Participants.Select(p => p.UserId)))
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            var userImages = await udb.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.PersonImageUrl);

            foreach (var r in qData)
            {
                if (!string.IsNullOrEmpty(r.UserCreatorID) && userImages.TryGetValue(r.UserCreatorID, out var img))
                {
                    r.UserImage = img;
                }

                foreach (var p in r.CallRecording?.Participants ?? [])
                {
                    if (!string.IsNullOrEmpty(p.UserId) && userImages.TryGetValue(p.UserId, out var pImg))
                    {
                        p.UserImage = pImg;
                    }
                }
            }

            return qData;
        }

        private async Task<List<Report>> GetAllReports(ReportFilterVModel model, Guid? dealId = null)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);
            var qData = db.Reports.AsNoTracking()
                .Include(r => r.CallRecording.Participants)
                .OrderByDescending(r => r.CreateDate)
                .AsQueryable();

            ViewBag.IsManager = rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش") || rols.Contains("مدیر عامل") ||
                                rols.Contains("مدیر اجرایی");
            ViewBag.CurrentUserId = user.Id;

            if (dealId != null)
            {
                ViewBag.PassType = 4;
                qData = qData.Where(f => f.DealId == dealId);
            } else
            {
                ViewBag.PassType = 0;

                if (!string.IsNullOrWhiteSpace(model.serVal))
                {
                    var val = $"%{model.serVal}%";
                    qData = qData.Where(r =>
                        EF.Functions.Like(r.CustomerTxt, val) ||
                        EF.Functions.Like(r.ReportText, val) ||
                        EF.Functions.Like(r.RepTel, val)
                    );
                }

                if (model._type != -1)
                {
                    qData = qData.Where(r => r.ReportType == (ReportTypeEnum)model._type);
                }

                if (model._type == 1)
                {
                    if (model.ResultCallId != null)
                    {
                        qData = qData.Where(f => f.ResultCallId == model.ResultCallId);
                    }

                    if (model.FinalResultCallId != null)
                    {
                        qData = qData.Where(f => f.FinalResultCallId == model.FinalResultCallId);
                    }

                    if (model.ReasonCancelId != null)
                    {
                        qData = qData.Where(f => f.ReasonCancelId == model.ReasonCancelId);
                    }
                }


                if (model.filt != null)
                {
                    qData = qData.ApplyFilter(model, f => f.CustomerId, f => f.CreateDate, f => f.UserCreatorID);
                }


                if (!rols.Contains("مدیر ارشد") && !rols.Contains("مدیر فروش"))
                {
                    qData = qData.Where(r =>
                        (r.ReportType == ReportTypeEnum.گزارش_رضایتمندی && r.UserCreatorID == user.Id) ||
                        r.ReportType != ReportTypeEnum.گزارش_رضایتمندی
                    );
                }

                if (model.Count != -1)
                    qData = qData.Take(model.Count);
            }

            var data = await qData.ToListAsync();
            return data;
        }


        [HttpPost]
        public async Task<IActionResult> AddReport(Report model, CancellationToken cancellationToken)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                if (user == null) return BadRequest();

                var roles = await userManager.GetRolesAsync(user);
                var result = await addReportHandler.HandleAsync(
                    new AddReportCommand(model, user, roles.Contains("مدیر فروش") || roles.Contains("مدیر ارشد")),
                    cancellationToken);

                if (result == AddReportResult.NotFound) return BadRequest();
                if (result == AddReportResult.Forbidden)
                    return Ok(new { status = 201, message = "شما مجاز به ویرایش این گزارش نیستید", data = "" });

                await hub.Clients.AllExcept(TaskHub.GetConnectionId(user.Id))
                    .SendAsync("ShowReport", cancellationToken);
                return Ok(new {
                    status = 200,
                    message = result == AddReportResult.Created ? "گزارش جدید ثبت شد" : "گزارش ویرایش شد",
                    data = ""
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCallAudio(Guid callId)
        {
            var call = await db.CallRecordings.FirstOrDefaultAsync(x => x.Id == callId);

            if (call == null) return NotFound("رکوردی یافت نشد!");

            var stream = await issabelService.GetAudioWithRetry(call.RecordingFilePath);

            if (stream == null) return NotFound("رکوردی یافت نشد!");

            return File(stream, "audio/wav", Path.GetFileName(call.RecordingFilePath));
        }
    }
}
