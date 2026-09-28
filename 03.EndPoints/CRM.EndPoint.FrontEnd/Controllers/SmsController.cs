using System;
using System.Linq;
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
            var page = start / pageSize + 1;

            var data = await smsService.GetReportsAsync(page, pageSize);

            if (!string.IsNullOrEmpty(searchValue))
            {
                data = data.Where(x =>
                    x.Mobile.Contains(searchValue) ||
                    x.MessageText.Contains(searchValue) ||
                    x.MessageId.ToString().Contains(searchValue) ||
                    x.DeliveryStateTxt.Contains(searchValue)
                ).ToList();
            }

            var recordsTotal = data.Count;

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data
            });
        }
        catch (Exception)
        {
            return BadRequest();
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
            var page = start / pageSize + 1;

            var data = await smsService.GetReceivesAsync(page, pageSize);

            if (!string.IsNullOrEmpty(searchValue))
            {
                data = data.Where(x =>
                    x.Mobile.Contains(searchValue) ||
                    x.MessageText.Contains(searchValue) ||
                    x.Number.Contains(searchValue)
                ).ToList();
            }

            var recordsTotal = data.Count;

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data
            });
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}