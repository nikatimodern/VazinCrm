using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Services.Sms;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class SmsController(SmsService smsService) : Controller {
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
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.Trim().ToLower();

            var pageSize = length == -1 ? 100 : Math.Min(length, 100);

            // Search
            if (!string.IsNullOrWhiteSpace(searchValue))
            {
                var searchResult = await smsService.SearchReceivesAsync(searchValue);

                var filteredData = searchResult.Data;

                return Json(new {
                    draw,
                    recordsTotal = searchResult.Total,
                    recordsFiltered = filteredData.Count,
                    data = filteredData
                });
            }

            var page = (start / pageSize) + 1;

            var result = await smsService.GetReceivesAsync(page, pageSize);

            return Json(new {
                draw,
                recordsTotal = result.Total,
                recordsFiltered = result.Total,
                data = result.Data,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }


    [HttpPost]
    public async Task<IActionResult> SyncSms(CancellationToken cancellationToken)
    {
        try
        {
            await smsService.SyncReceivesAsync(cancellationToken);

            return Ok("پیام‌ها با موفقیت همگام‌سازی شدند.");
        }
        catch (Exception)
        {
            return BadRequest("خطا در همگام‌سازی پیام‌ها.");
        }
    }
}