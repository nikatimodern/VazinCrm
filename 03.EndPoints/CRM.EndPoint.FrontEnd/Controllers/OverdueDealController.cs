using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Deals;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Deals.Repositories;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
[Authorize(Roles = "مدیر فروش,مدیر ارشد,مدیر عامل,کارشناس فروش")]
public class OverdueDealController : Controller
{
    private readonly CRMContextDb _db;
    private readonly IDealRepository _deals;
    private readonly UserManager<AppUser> _userManager;
    private readonly DateTimeServices _dateTime;

    public OverdueDealController(CRMContextDb db, IDealRepository deals, UserManager<AppUser> userManager,
        DateTimeServices dateTime)
    {
        _db = db;
        _deals = deals;
        _userManager = userManager;
        _dateTime = dateTime;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> GetOverdueDeals(
        [FromBody] FilterVModel model,
        DateTime? startDate,
        DateTime? endDate,
        string sortBy = "createDate",
        string sortDirection = "desc",
        bool withFollowUp = false)
    {
        var now = DateTime.Now;
        var overdueBefore = now.AddDays(-OverdueDealRules.OverdueAfterDays);
        var restrictToOwnDeals = User.IsInRole("کارشناس فروش") &&
            !User.IsInRole("مدیر فروش") && !User.IsInRole("مدیر ارشد") && !User.IsInRole("مدیر عامل");
        var creatorId = _userManager.GetUserId(User);

        var query = _db.Deals
            .AsNoTracking()
            .Where(deal =>
                deal.Status == DealStatusEnum.باز &&
                deal.CreateDate >= OverdueDealRules.StartDate &&
                deal.CreateDate <= overdueBefore);

        if (restrictToOwnDeals)
            query = query.Where(deal => deal.UserCreatorID == creatorId);

        var overdueCount = await query.CountAsync(deal =>
            !deal.OverdueFollowUpAt.HasValue || deal.OverdueFollowUpAt <= now);

        query = withFollowUp
            ? query.Where(deal => deal.OverdueFollowUpAt.HasValue && deal.OverdueFollowUpAt > now)
            : query.Where(deal => !deal.OverdueFollowUpAt.HasValue || deal.OverdueFollowUpAt <= now);

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
            query = query.Where(deal => deal.CreateDate >= startDate.Value.Date.AddDays(-OverdueDealRules.OverdueAfterDays));
        }

        if (endDate.HasValue)
        {
            query = query.Where(deal => deal.CreateDate < endDate.Value.Date.AddDays(1).AddDays(-OverdueDealRules.OverdueAfterDays));
        }

        var descending = !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (sortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("serialnumber", true) => query.OrderByDescending(deal => deal.SerialNumber),
            ("serialnumber", false) => query.OrderBy(deal => deal.SerialNumber),
            ("title", true) => query.OrderByDescending(deal => (deal.Title ?? "").Trim()),
            ("title", false) => query.OrderBy(deal => (deal.Title ?? "").Trim()),
            ("customer", true) => query.OrderByDescending(deal => ((deal.CusCompanyName ?? deal.CustomerName) ?? "").Trim()),
            ("customer", false) => query.OrderBy(deal => ((deal.CusCompanyName ?? deal.CustomerName) ?? "").Trim()),
            ("customercode", true) => query.OrderByDescending(deal => deal.CustomerCode),
            ("customercode", false) => query.OrderBy(deal => deal.CustomerCode),
            ("createdate", true) => query.OrderByDescending(deal => deal.CreateDate),
            ("createdate", false) => query.OrderBy(deal => deal.CreateDate),
            ("waitingdays", true) => query.OrderBy(deal => deal.CreateDate),
            ("waitingdays", false) => query.OrderByDescending(deal => deal.CreateDate),
            ("usercreator", true) => query.OrderByDescending(deal => (deal.UserCreator ?? "").Trim()),
            ("usercreator", false) => query.OrderBy(deal => (deal.UserCreator ?? "").Trim()),
            ("commenttext", true) => query.OrderByDescending(deal => (deal.CommentText ?? "").Trim()),
            ("commenttext", false) => query.OrderBy(deal => (deal.CommentText ?? "").Trim()),
            _ => query.OrderByDescending(deal => deal.CreateDate)
        };

        var deals = await query
            .Select(deal => new
            {
                deal.Id,
                deal.SerialNumber,
                deal.Title,
                deal.CustomerName,
                deal.CusCompanyName,
                deal.CustomerCode,
                deal.CreateDate,
                deal.OverdueFollowUpAt,
                deal.UserCreator,
                deal.CommentText
            })
            .ToListAsync();

        return Ok(new { data = deals, overdueCount });
    }

    [HttpGet]
    public async Task<IActionResult> GetOverdueDealDetails(Guid id)
    {
        var overdueBefore = DateTime.Now.AddDays(-OverdueDealRules.OverdueAfterDays);
        var restrictToOwnDeals = User.IsInRole("کارشناس فروش") &&
            !User.IsInRole("مدیر فروش") && !User.IsInRole("مدیر ارشد") && !User.IsInRole("مدیر عامل");
        var creatorId = _userManager.GetUserId(User);
        var deal = await _db.Deals
            .AsNoTracking()
            .Where(deal => deal.Id == id &&
                deal.Status == DealStatusEnum.باز &&
                deal.CreateDate >= OverdueDealRules.StartDate &&
                (!restrictToOwnDeals || deal.UserCreatorID == creatorId) &&
                deal.CreateDate <= overdueBefore)
            .Select(deal => new
            {
                deal.Id,
                deal.CustomerId,
                deal.UserCreatorID,
                deal.FinancialStatus,
                deal.SerialNumber,
                deal.Title,
                deal.CustomerName,
                deal.CusCompanyName,
                deal.CustomerCode,
                deal.MobileNumber,
                deal.CreateDate,
                deal.UserCreator,
                deal.CommentText,
                deal.OverdueFollowUpNote,
                deal.OverdueFollowUpAt,
                deal.Status,
                ReportCount = deal.Reports.Count(),
                PreFactorCount = deal.PreFactors.Count(),
                PreFactorTotalAmount = deal.PreFactors.Sum(item =>
                    (decimal?)(item.SumTotalAmount + item.TotalTax - item.Discount)) ?? 0m,
                FactorCount = deal.Factors.Count(),
                FactorTotalAmount = deal.Factors.Sum(item =>
                    (decimal?)(item.SumTotalAmount + item.TotalTax - item.Discount)) ?? 0m,
                AccountancyCount = _db.Accountancies.Count(accountancy =>
                    accountancy.pfid.HasValue &&
                    deal.PreFactors.Any(preFactor => preFactor.Id == accountancy.pfid.Value) &&
                    accountancy.InvoiceType == InvoiceTypeEnum.پیش_فاکتور &&
                    accountancy.AccType == AcEnum.واریزی &&
                    !accountancy.IsDeleted),
                AccountancyTotalAmount = _db.Accountancies
                    .Where(accountancy =>
                        accountancy.pfid.HasValue &&
                        deal.PreFactors.Any(preFactor => preFactor.Id == accountancy.pfid.Value) &&
                        accountancy.InvoiceType == InvoiceTypeEnum.پیش_فاکتور &&
                        accountancy.AccType == AcEnum.واریزی &&
                        (accountancy.PayType == PayTypeEnum.نقدی || accountancy.PayType == PayTypeEnum.تهاتری) &&
                        !accountancy.IsDeleted)
                    .Sum(accountancy => accountancy.credit) ?? 0m
            })
            .FirstOrDefaultAsync();

        return deal == null ? NotFound() : Ok(deal);
    }

    [HttpPost]
    [Authorize(Roles = "مدیر فروش,مدیر ارشد")]
    public async Task<IActionResult> UpdateFollowUp([FromBody] OverdueFollowUpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Id == Guid.Empty || request.Note?.Trim().Length > 4000)
            return BadRequest(new { message = "توضیحات پیگیری نباید بیشتر از ۴۰۰۰ نویسه باشد." });

        if (request.Status is not (DealStatusEnum.باز or DealStatusEnum.موفق or DealStatusEnum.ناموفق) ||
            (request.Status != DealStatusEnum.باز && !string.IsNullOrWhiteSpace(request.FollowUpDate)))
            return BadRequest(new { message = "وضعیت سفارش یا تاریخ پیگیری مجدد معتبر نیست." });

        DateTime? followUpAt = null;
        if (!string.IsNullOrWhiteSpace(request.FollowUpDate))
        {
            followUpAt = _dateTime.GetDateMiladi(request.FollowUpDate);
            if (!followUpAt.HasValue || followUpAt.Value.Date <= DateTime.Today)
                return BadRequest(new { message = "تاریخ پیگیری مجدد باید بعد از امروز باشد. برای ثبت بدون مهلت، تاریخ قبلی را پاک کنید." });
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        var result = await _deals.UpdateOverdueFollowUpAsync(request.Id, followUpAt,
            request.Status, request.Note, user.Id, user.FullName,
            cancellationToken);

        return result switch
        {
            OverdueFollowUpUpdateResult.Updated => Ok(),
            OverdueFollowUpUpdateResult.HasFactors => Conflict(new { message = "این سفارش فاکتور دارد و نمی‌توان وضعیت آن را ناموفق کرد." }),
            _ => NotFound(new { message = "سفارش معوقه یافت نشد یا دیگر باز نیست." })
        };
    }

}

public sealed class OverdueFollowUpRequest
{
    public Guid Id { get; set; }
    public string FollowUpDate { get; set; }
    public DealStatusEnum Status { get; set; }
    public string Note { get; set; }
}
