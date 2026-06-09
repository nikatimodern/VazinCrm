using System.Threading.Tasks;
using CRM.Core.Domain.Checks.Entities;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class CheckController : Controller {
    private readonly CRMContextDb db;


    public CheckController(CRMContextDb db)
    {
        this.db = db;
    }

/*    // ۱. اکشن لود اولیه صفحه (نمایش کارت‌های دسته چک)
    public async Task<IActionResult> Index()
    {
        // دیتای دسته چک‌ها را از سرویس می‌گیریم
        var checkbooks = await db.GetActiveCheckbooksAsync();

        // دیتای مدل را به ویوی اصلی پاس می‌دهیم
        return View(checkbooks);
    }

    [HttpGet]
    public async Task<IActionResult> GetCheckListByCheckbookId(int checkbookId)
    {
        var checks = await _checkService.GetChecksByCheckbookIdAsync(checkbookId);

        return Json(checks);
    }*/
}