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

    [HttpGet]
    public async Task<IActionResult> Reports(int page = 1, int pageSize = 100)
    {
        return Json(await smsService.GetReportsAsync(page, pageSize));
    }

    [HttpGet]
    public async Task<IActionResult> Receives(int page = 1, int pageSize = 100)
    {
        return Json(await smsService.GetReceivesAsync(page, pageSize));
    }
}