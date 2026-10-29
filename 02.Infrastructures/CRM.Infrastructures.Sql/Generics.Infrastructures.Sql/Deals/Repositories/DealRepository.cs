#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Deals;
using CRM.Core.Domain.Deals.Enums;
using CRM.Core.Domain.Deals.Repositories;
using CRM.Framework;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.Deals.Repositories;

public sealed class DealRepository(CRMContextDb db) : IDealRepository
{
    public Task<Deal?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        return db.Deals.FirstOrDefaultAsync(deal => deal.Id == id, cancellationToken);
    }

    public async Task<OverdueFollowUpUpdateResult> UpdateOverdueFollowUpAsync(Guid id, DateTime? followUpAt,
        DealStatusEnum status, string note, string userId, string userName,
        CancellationToken cancellationToken)
    {
        var overdueBefore = DateTime.Now.AddDays(-OverdueDealRules.OverdueAfterDays);
        var deal = await db.Deals.Include(item => item.Factors).FirstOrDefaultAsync(item =>
            item.Id == id && item.Status == DealStatusEnum.باز &&
            item.CreateDate >= OverdueDealRules.StartDate &&
            item.CreateDate <= overdueBefore, cancellationToken);
        if (deal is null) return OverdueFollowUpUpdateResult.NotFound;
        if (status == DealStatusEnum.ناموفق && deal.Factors.Any())
            return OverdueFollowUpUpdateResult.HasFactors;

        deal.OverdueFollowUpAt = status == DealStatusEnum.باز ? followUpAt : null;
        deal.Status = status;
        deal.OverdueFollowUpNote = note?.Trim() ?? string.Empty;

        deal.UserModifyID = userId;
        deal.UserModify = userName;
        deal.ModifyDate = DateTime.Now;
        deal.ModifyDateTxt = BaseServices.GetDateTimePersian();
        await db.SaveChangesAsync(cancellationToken);
        return OverdueFollowUpUpdateResult.Updated;
    }
}
