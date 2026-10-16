using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Enums;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services.Extensions;
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

    [HttpPost]
    public async Task<IActionResult> GetOverdueDeals([FromBody] FilterVModel model, DateTime? startDate, DateTime? endDate)
    {
        var overdueBefore = DateTime.Now.AddDays(-14);

        var query = _db.Deals
            .AsNoTracking()
            .Where(deal =>
                deal.Status == DealStatusEnum.باز &&
                deal.CreateDate <= overdueBefore);

        if (model?.filt != null)
        {
            query = query.ApplyFilter(model, deal => deal.CustomerId, deal => deal.CreateDate, deal => deal.UserCreatorID);
        }

        if (!string.IsNullOrWhiteSpace(model?.serVal))
        {
            var search = model.serVal.Trim();
            query = query.Where(deal =>
                deal.SerialNumber.ToString() == search ||
                deal.CustomerCode.ToString() == search ||
                deal.Title.Contains(search) ||
                deal.CustomerName.Contains(search) ||
                deal.CusCompanyName.Contains(search) ||
                deal.CommentText.Contains(search));
        }

        // بازه انتخابی مربوط به زمان معوق‌شدن سفارش است (۱۴ روز پس از ثبت).
        if (startDate.HasValue)
        {
            query = query.Where(deal => deal.CreateDate >= startDate.Value.Date.AddDays(-14));
        }

        if (endDate.HasValue)
        {
            query = query.Where(deal => deal.CreateDate < endDate.Value.Date.AddDays(1).AddDays(-14));
        }

        var deals = await query
            .OrderByDescending(deal => deal.CreateDate)
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
