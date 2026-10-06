using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sms.Enums;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class SmsController(SmsService smsService, CRMContextDb db) : Controller {
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        return Json(await smsService.GetDashboardAsync());
    }

    [HttpPost]
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
                        x.Mobile.Equals(searchValue) ||
                        x.MessageText.Equals(searchValue) ||
                        x.FarazId.ToString().Equals(searchValue) ||
                        x.Number.Equals(searchValue)
                    );
                } else
                {
                    query = query.Where(x =>
                        x.Mobile.Contains(searchValue) ||
                        x.MessageText.Contains(searchValue) ||
                        x.FarazId.ToString().Contains(searchValue) ||
                        x.Number.Contains(searchValue)
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
                        x.Mobile.Equals(searchValue) ||
                        x.MessageText.Equals(searchValue) ||
                        x.FarazId.ToString().Equals(searchValue) ||
                        x.Number.Equals(searchValue)
                    );
                } else
                {
                    query = query.Where(x =>
                        x.Mobile.Contains(searchValue) ||
                        x.MessageText.Contains(searchValue) ||
                        x.FarazId.ToString().Contains(searchValue) ||
                        x.Number.Contains(searchValue)
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