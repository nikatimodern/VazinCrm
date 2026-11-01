#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Deals;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Deals.Repositories;
using CRM.Core.Domain.Querying;
using CRM.Infrastructures.Sql.Querying;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Deals.Repositories;
public sealed class OverdueDealRepository(CRMContextDb db) : IOverdueDealRepository
{
    public async Task<OverdueDealsResult> GetListAsync(QueryFilter filter, OverdueSearch options, CancellationToken ct)
    {
        var now = options.Now;
        var overdueBefore = options.Cutoff;
        var restrictToOwnDeals = options.OwnOnly;
        var creatorId = options.CreatorId;
        var startDate = options.StartDate;
        var endDate = options.EndDate;
        var sortBy = options.SortBy;
        var sortDirection = options.SortDirection;
        var withFollowUp = options.WithFollowUp;
        var query = db.Deals.AsNoTracking().Where(deal => deal.Status == DealStatusEnum.باز && deal.CreateDate >= OverdueDealRules.StartDate && deal.CreateDate <= overdueBefore);
        if (restrictToOwnDeals)
            query = query.Where(deal => deal.UserCreatorID == creatorId);
        var overdueCount = await query.CountAsync(deal => !deal.OverdueFollowUpAt.HasValue || deal.OverdueFollowUpAt <= now, ct);
        query = withFollowUp ? query.Where(deal => deal.OverdueFollowUpAt.HasValue && deal.OverdueFollowUpAt > now) : query.Where(deal => !deal.OverdueFollowUpAt.HasValue || deal.OverdueFollowUpAt <= now);
        if (filter.HasFilter)
        {
            query = query.ApplyFilter(filter, deal => deal.CustomerId, deal => deal.CreateDate, deal => deal.UserCreatorID);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(deal => deal.SerialNumber.ToString() == search || deal.CustomerCode.ToString() == search || deal.Title.Contains(search) || deal.CustomerName.Contains(search) || deal.CusCompanyName.Contains(search) || deal.CommentText.Contains(search));
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
            _ => query.OrderByDescending(deal => deal.CreateDate)};
        var deals = await query.Select(deal => new OverdueDealRow { Id = deal.Id, SerialNumber = deal.SerialNumber, Title = deal.Title, CustomerName = deal.CustomerName, CusCompanyName = deal.CusCompanyName, CustomerCode = deal.CustomerCode, CreateDate = deal.CreateDate, OverdueFollowUpAt = deal.OverdueFollowUpAt, UserCreator = deal.UserCreator, CommentText = deal.CommentText, }).ToListAsync(ct);
        return new(deals, overdueCount);
    }

    public async Task<OverdueDealDetails?> GetDetailsAsync(Guid id, DateTime overdueBefore, bool restrictToOwnDeals, string creatorId, CancellationToken ct)
    {
        var deal = await db.Deals.AsNoTracking().Where(deal => deal.Id == id && deal.Status == DealStatusEnum.باز && deal.CreateDate >= OverdueDealRules.StartDate && (!restrictToOwnDeals || deal.UserCreatorID == creatorId) && deal.CreateDate <= overdueBefore).Select(deal => new OverdueDealDetails { Id = deal.Id, CustomerId = deal.CustomerId, UserCreatorID = deal.UserCreatorID, FinancialStatus = deal.FinancialStatus, SerialNumber = deal.SerialNumber, Title = deal.Title, CustomerName = deal.CustomerName, CusCompanyName = deal.CusCompanyName, CustomerCode = deal.CustomerCode, MobileNumber = deal.MobileNumber, CreateDate = deal.CreateDate, UserCreator = deal.UserCreator, CommentText = deal.CommentText, OverdueFollowUpNote = deal.OverdueFollowUpNote, OverdueFollowUpAt = deal.OverdueFollowUpAt, Status = deal.Status, ReportCount = deal.Reports.Count(), PreFactorCount = deal.PreFactors.Count(), PreFactorTotalAmount = deal.PreFactors.Sum(item => (decimal? )(item.SumTotalAmount + item.TotalTax - item.Discount)) ?? 0m, FactorCount = deal.Factors.Count(), FactorTotalAmount = deal.Factors.Sum(item => (decimal? )(item.SumTotalAmount + item.TotalTax - item.Discount)) ?? 0m, AccountancyCount = db.Accountancies.Count(accountancy => accountancy.pfid.HasValue && deal.PreFactors.Any(preFactor => preFactor.Id == accountancy.pfid.Value) && accountancy.InvoiceType == InvoiceTypeEnum.پیش_فاکتور && accountancy.AccType == AcEnum.واریزی && !accountancy.IsDeleted), AccountancyTotalAmount = db.Accountancies.Where(accountancy => accountancy.pfid.HasValue && deal.PreFactors.Any(preFactor => preFactor.Id == accountancy.pfid.Value) && accountancy.InvoiceType == InvoiceTypeEnum.پیش_فاکتور && accountancy.AccType == AcEnum.واریزی && (accountancy.PayType == PayTypeEnum.نقدی || accountancy.PayType == PayTypeEnum.تهاتری) && !accountancy.IsDeleted).Sum(accountancy => accountancy.credit) ?? 0m }).FirstOrDefaultAsync(ct);
        return deal;
    }
}
