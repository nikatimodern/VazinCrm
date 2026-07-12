using ClosedXML.Excel;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Reports.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.SignalR;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using DocumentFormat.OpenXml.InkML;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class ReportController : Controller {
        private readonly IHubContext<TaskHub> hub;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly CRMContextDb db;
        private readonly DateTimeServices dts;
        private readonly UserContextDb udb;
        private readonly Service genService;
        private readonly IVeiwRenderService vrs;
        private readonly LogSystemService _logSystemService;
        private readonly IssabelService issabelService;

        public ReportController(IHubContext<TaskHub> hub, UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db,
            DateTimeServices dts, UserContextDb udb, Service GenService, IVeiwRenderService vrs, LogSystemService logSystemService,
            IssabelService issabelService)
        {
            this.hub = hub;
            this.userManager = userManager;
            _logger = logger;
            this.db = db;
            this.dts = dts;
            this.udb = udb;
            genService = GenService;
            this.vrs = vrs;
            _logSystemService = logSystemService;
            this.issabelService = issabelService;
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
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ExportExcelReport([FromBody] ReportFilterVModel model)
        {
            try
            {
                using var workbook = new XLWorkbook();
                //workbook.ColumnWidth = 30;
                var worksheet = workbook.Worksheets.Add("Reports");
                worksheet.Columns().AdjustToContents();

                worksheet.RightToLeft = true;
                var currentRow = 1;

                worksheet.Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.LightGreen);
                worksheet.Row(currentRow).Style.Font.Bold = true;
                worksheet.Cell(currentRow, 1).Value = "ردیف";
                worksheet.Cell(currentRow, 2).Value = "تاریخ";
                worksheet.Cell(currentRow, 3).Value = "نوع";
                worksheet.Cell(currentRow, 4).Value = "مشتری";
                worksheet.Cell(currentRow, 5).Value = "تلفن";
                worksheet.Cell(currentRow, 6).Value = "متن گزارش";
                worksheet.Cell(currentRow, 7).Value = "کارشناس فروش";
                if (model._type == (int)ReportTypeEnum.گزارش_پیگیری)
                {
                    worksheet.Cell(currentRow, 8).Value = "نتیجه تماس";
                    worksheet.Cell(currentRow, 9).Value = "نتیجه نهایی";
                    worksheet.Cell(currentRow, 10).Value = "علت انصراف";
                }


                var data = await GetAllReports(model);

                foreach (var item in data)
                {
                    currentRow++;
                    if (currentRow % 2 == 0)
                        worksheet.Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.LightCyan);
                    worksheet.Cell(currentRow, 1).Value = currentRow;
                    worksheet.Cell(currentRow, 2).Value = TextService.ToEnglishNumber(item.RegDateTxt + " " + item.RegTimeTxt);
                    worksheet.Cell(currentRow, 3).Value = item.ReportTypeTxt;
                    worksheet.Cell(currentRow, 4).Value = item.CustomerTxt;
                    worksheet.Cell(currentRow, 5).Value = item.RepTel;
                    worksheet.Cell(currentRow, 6).Value = item.ReportText;
                    worksheet.Cell(currentRow, 7).Value = item.UserCreator;
                    if (model._type == (int)ReportTypeEnum.گزارش_پیگیری)
                    {
                        worksheet.Cell(currentRow, 8).Value = item.ResultCallTxt;
                        worksheet.Cell(currentRow, 9).Value = item.FinalResultCallTxt;
                        worksheet.Cell(currentRow, 10).Value = item.ReasonCancelTxt;
                    }
                }

                worksheet.Columns().AdjustToContents();
                worksheet.Columns().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Columns().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(
                    content,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Reports.xlsx"
                );
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException);
            }
        }


        [HttpGet]
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
                    return Ok(new { status = 200, message = "با موفقیت حذف شد!", data = "" });
                }

                return Ok(new { status = 201, message = "شما مجاز به حذف این گزارش نیستید!", data = "" });
            }
            catch
            {
                return BadRequest();
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetReport(Guid Id)
        {
            var rep = await db.Reports.Where(f => f.Id == Id).FirstOrDefaultAsync();
            return Ok(rep);
        }


        [HttpPost]
        public async Task<IActionResult> GetAllReportPartial([FromBody] ReportFilterVModel model, [FromQuery] Guid? dealId = null)
        {
            var data = await GetAllReports(model, dealId);

            //var userIds = data.Select(r => r.UserCreatorID).Distinct().ToList();

            var userIds = data.Select(r => r.UserCreatorID)
                .Union(data.Where(r => r.CallRecording != null).SelectMany(r => r.CallRecording.Participants.Select(p => p.UserId)))
                .Where(id => !string.IsNullOrEmpty(id))
                .ToList();

            var userImages = await udb.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.PersonImageUrl);

            foreach (var r in data)
            {
                if (userImages.TryGetValue(r.UserCreatorID, out var img))
                    r.UserImage = img;

                foreach (var p in r.CallRecording?.Participants ?? [])
                {
                    if (userImages.TryGetValue(p.UserId, out var pImg))
                        p.UserImage = pImg;
                }
            }

            var html = await vrs.RenderToStringAsync("GetAllReportPartial", data,
                new Dictionary<string, object> {
                    { "PassType", ViewBag.PassType },
                    { "IsManager", ViewBag.IsManager },
                    { "CurrentUserId", ViewBag.CurrentUserId },
                });

            return Ok(new { count = data.Count, html });
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
                        EF.Functions.Like(r.ReportText, val)
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
        public async Task<IActionResult> AddReport(Report model)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);
                var deal = await db.Deals.Where(f => f.Id == model.DealId).FirstOrDefaultAsync();

                if (deal != null)
                {
                    deal.ModifyDate = DateTime.Now;
                    deal.ModifyDateTxt = dts.GetDateTimePersian();
                }

                if (model.Id == Guid.Empty)
                {
                    model.ReportTypeTxt = Service.getTypetxtReportType((int)model.ReportType);
                    model.UserCreator = user.FullName;
                    model.UserCreatorID = user.Id;

                    model.RegDate = DateTime.Now;
                    model.RegDateTxt = dts.GetDatePersian();
                    model.RegTimeTxt = dts.GetNowTime("HH:mm:ss");

                    db.Reports.Add(model);

                    await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
                    await db.SaveChangesAsync();
                    await hub.Clients.AllExcept(TaskHub.GetConnectionId(user.Id)).SendAsync("ShowReport");
                    return Ok(new { status = 200, message = "گزارش جدید ثبت شد", data = "" });
                } else
                {
                    var oldModel = await db.Reports.Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                    if (rols.Contains("مدیر فروش") || rols.Contains("مدیر ارشد") || oldModel.UserCreatorID == user.Id)
                    {
                        model.ReportTypeTxt = Service.getTypetxtReportType((int)model.ReportType);
                        model.UserModify = user.FullName;
                        model.UserModifyID = user.Id;


                        await _logSystemService.LogChange(oldModel, model, LogActionEnum.ویرایش, user);

                        db.Entry(oldModel).CurrentValues.SetValues(model);


                        db.Entry(model).Property(f => f.CreateDate).IsModified = false;
                        db.Entry(model).Property(f => f.CreateDateTxt).IsModified = false;
                        db.Entry(model).Property(f => f.UserCreator).IsModified = false;
                        db.Entry(model).Property(f => f.UserCreatorID).IsModified = false;

                        await db.SaveChangesAsync();
                        await hub.Clients.AllExcept(TaskHub.GetConnectionId(user.Id)).SendAsync("ShowReport");


                        return Ok(new { status = 200, message = "گزارش ویرایش شد", data = "" });
                    }

                    //await hub.Clients.All.SendAsync("ShowReport");

                    return Ok(new { status = 201, message = "شما مجاز به ویرایش این گزارش نیستید", data = "" });
                }
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

            if (call == null) return NotFound();

            var stream = await issabelService.GetAudioWithRetry(call.RecordingFilePath);

            if (stream == null) return NotFound();

            return File(stream, "audio/wav", Path.GetFileName(call.RecordingFilePath));
        }
    }
}