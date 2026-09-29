using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Sms;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class SmsController(SmsService smsService, CRMContextDb db, DateTimeServices dtService) : Controller {
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

            var pageSize = length == -1 ? 100 : Math.Min(length, 100);
            var page = (start / pageSize) + 1;

            var result = await smsService.GetReportsAsync(page, pageSize);

            var data = result.Data;

            if (!string.IsNullOrEmpty(searchValue))
            {
                data = data.Where(x =>
                    x.Mobile.Contains(searchValue) ||
                    x.MessageText.Contains(searchValue) ||
                    x.MessageId.ToString().Contains(searchValue) ||
                    x.DeliveryStateTxt.Contains(searchValue)
                ).ToList();
            }

            return Json(new {
                draw,
                recordsTotal = result.Total,
                recordsFiltered = result.Total,
                data,
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
                if (searchValue?.Length < 3)
                {
                    query = query.Where(x =>
                        x.Mobile.Equals(searchValue) ||
                        x.MessageText.Equals(searchValue) ||
                        x.Number.Equals(searchValue)
                    );
                } else
                {
                    query = query.Where(x =>
                        x.Mobile.Contains(searchValue) ||
                        x.MessageText.Contains(searchValue) ||
                        x.Number.Contains(searchValue)
                    );
                }
            }


            var recordsFiltered = await query.CountAsync();

            var pageSize = length == -1 ? 100 : Math.Min(length, 100);

            var data = await query
                .Skip(start)
                .Take(pageSize)
                .Select(x => new SmsReceiveModel {
                    Id = x.FarazId,
                    Mobile = x.Mobile,
                    MessageText = x.MessageText,
                    Number = x.Number,
                    CreatedAtFaraz = x.CreatedAtFaraz,
                    ReceivedDateTime = x.ReceivedDateTime,
                    ReceivedDateTimeTxt = dtService.GetDateTimePersian(x.ReceivedDateTime),
                    Seen = x.Seen
                })
                .ToListAsync();

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
            await smsService.SyncReceivesAsync(cancellationToken);

            return Ok();
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}