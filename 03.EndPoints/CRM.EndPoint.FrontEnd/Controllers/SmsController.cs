using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sms.Enums;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CRM.EndPoint.FrontEnd.Security;
using CRM.Core.ApplicationServices.Security;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class SmsController(SmsService smsService, CRMContextDb db, DocumentAccessPolicy access) : Controller {
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasAsync("SentSms", "View", ct) && !await access.HasAsync("ReceivedSms", "View", ct)) return Forbid();
        return View();
    }

    [HttpGet]
    [CatalogPermission("SentSms", "View")]
    [CatalogPermission("ReceivedSms", "View")]
    public async Task<IActionResult> Dashboard()
    {
        return Json(await smsService.GetDashboardAsync());
    }

    [HttpPost]
    [CatalogPermission("SentSms", "View")]
    public async Task<IActionResult> GetReportsDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "20");
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim().ToLower();
            var patternTypeValue = Request.Form["patternType"].FirstOrDefault();

            var query = db.SentSmses
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtFaraz)
                .AsQueryable();

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                if (searchValue.Length < 3)
                {
                    query = query.Where(x =>
                        x.Mobile == searchValue ||
                        x.MessageText == searchValue ||
                        x.FarazId.ToString() == searchValue ||
                        x.Number == searchValue ||
                        (x.ReceivedFullName != null && x.ReceivedFullName.Contains(searchValue))
                    );
                }
                else
                {
                    query = query.Where(x =>
                        (x.Mobile != null && x.Mobile.Contains(searchValue)) ||
                        (x.MessageText != null && x.MessageText.Contains(searchValue)) ||
                        x.FarazId.ToString().Contains(searchValue) ||
                        (x.Number != null && x.Number.Contains(searchValue)) ||
                        (x.ReceivedFullName != null && x.ReceivedFullName.Contains(searchValue))
                    );
                }
            }
            if (!string.IsNullOrWhiteSpace(patternTypeValue) && Enum.TryParse<SmsPatternType>(patternTypeValue, out var patternType))
            {
                query = query.Where(x => x.PatternType == patternType);
            }


            var recordsFiltered = await query.CountAsync();

            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [CatalogPermission("ReceivedSms", "View")]
    public async Task<IActionResult> GetReceivesDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "20");
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim();

            var query = db.ReceivedSmses
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtFaraz)
                .AsQueryable();

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                if (searchValue.Length < 3)
                {
                    query = query.Where(x =>
                        x.Mobile == searchValue ||
                        x.MessageText == searchValue ||
                        x.FarazId.ToString() == searchValue ||
                        x.Number == searchValue ||
                        (x.SenderFullName != null && x.SenderFullName.Contains(searchValue))
                    );
                }
                else
                {
                    query = query.Where(x =>
                        (x.Mobile != null && x.Mobile.Contains(searchValue)) ||
                        (x.MessageText != null && x.MessageText.Contains(searchValue)) ||
                        x.FarazId.ToString().Contains(searchValue) ||
                        (x.Number != null && x.Number.Contains(searchValue)) ||
                        (x.SenderFullName != null && x.SenderFullName.Contains(searchValue))
                    );
                }
            }

            var recordsFiltered = await query.CountAsync();

            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered,
                data
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpPost]
    [CatalogPermission("ReceivedSms", "Sync")]
    public async Task<IActionResult> SyncReceives(CancellationToken cancellationToken)
    {
        try
        {
            await smsService.SyncReceiveSmsAsync(cancellationToken);

            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }


    [HttpPost]
    [CatalogPermission("SentSms", "Sync")]
    public async Task<IActionResult> SyncSents(CancellationToken cancellationToken)
    {
        try
        {
            await smsService.SyncSentSmsAsync(cancellationToken);

            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}