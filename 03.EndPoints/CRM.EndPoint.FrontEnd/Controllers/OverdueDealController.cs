using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
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
    public async Task<IActionResult> GetOverdueDeals(
        [FromBody] FilterVModel model,
        DateTime? startDate,
        DateTime? endDate,
        string sortBy = "createDate",
        string sortDirection = "desc")
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
                ReportCount = deal.Reports.Count(),
                PreFactorCount = deal.PreFactors.Count(),
                PreFactorTotalAmount = deal.PreFactors.Sum(item =>
                    (decimal?)(item.SumTotalAmount + item.TotalTax - item.Discount)) ?? 0m,
                FactorCount = deal.Factors.Count(),
                FactorTotalAmount = deal.Factors.Sum(item =>
                    (decimal?)(item.SumTotalAmount + item.TotalTax - item.Discount)) ?? 0m,
                HasCustomer = deal.CustomerCode.HasValue,
                AccountancyCount = _db.Accountancies.Count(accountancy =>
                    deal.CustomerCode.HasValue &&
                    accountancy.CustomerCode == deal.CustomerCode.Value &&
                    accountancy.InvoiceType == InvoiceTypeEnum.مشتری &&
                    !accountancy.IsDeleted),
                DebtAmount = _db.Accountancies
                    .Where(accountancy =>
                        deal.CustomerCode.HasValue &&
                        accountancy.CustomerCode == deal.CustomerCode.Value &&
                        accountancy.InvoiceType == InvoiceTypeEnum.مشتری &&
                        accountancy.AccType == AcEnum.بدهکاری &&
                        !accountancy.IsDeleted)
                    .Sum(accountancy => (decimal?)accountancy.debit) ?? 0m,
                ReceivableAmount = _db.Accountancies
                    .Where(accountancy =>
                        deal.CustomerCode.HasValue &&
                        accountancy.CustomerCode == deal.CustomerCode.Value &&
                        accountancy.InvoiceType == InvoiceTypeEnum.مشتری &&
                        accountancy.AccType == AcEnum.واریزی &&
                        accountancy.PayType == PayTypeEnum.نقدی &&
                        !accountancy.IsDeleted)
                    .Sum(accountancy => (decimal?)accountancy.credit) ?? 0m,
                ReturnedAmount = _db.Accountancies
                    .Where(accountancy =>
                        deal.CustomerCode.HasValue &&
                        accountancy.CustomerCode == deal.CustomerCode.Value &&
                        accountancy.InvoiceType == InvoiceTypeEnum.مشتری &&
                        accountancy.AccType == AcEnum.برگشتی &&
                        !accountancy.IsDeleted)
                    .Sum(accountancy => (decimal?)accountancy.BackWard) ?? 0m
            })
            .ToListAsync();

        return Ok(new { data = deals });
    }



}
