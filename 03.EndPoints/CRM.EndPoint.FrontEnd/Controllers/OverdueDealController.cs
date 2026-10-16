using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Enums;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
[Authorize(Roles = "مدیر فروش,مدیر ارشد,مدیر عامل")]
public class OverdueDealController : Controller
{
    private readonly CRMContextDb _db;

    public OverdueDealController(CRMContextDb db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetOverdueDeals()
    {
        var overdueBefore = DateTime.Now.AddDays(-14);

        var deals = await _db.Deals
            .AsNoTracking()
            .Where(deal =>
                deal.Status == DealStatusEnum.باز &&
                deal.CreateDate <= overdueBefore)
            .OrderBy(deal => deal.CreateDate)
            .Select(deal => new
            {
                deal.Id,
                deal.SerialNumber,
                deal.Title,
                deal.CustomerName,
                deal.CusCompanyName,
                deal.CustomerCode,
                deal.MobileNumber,
                deal.CreateDate,
                deal.UserCreator,
                deal.CommentText
            })
            .ToListAsync();

        return Ok(new { data = deals });
    }
}
