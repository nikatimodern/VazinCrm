using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.Deals;
using CRM.Core.Domain.Deals.Enums;
using CRM.EndPoint.FrontEnd.Models.Menu;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

public sealed class MenuBadgeService(CRMContextDb db, Service warnings, IServiceProvider services)
{
    public async Task PopulateAsync(List<MenuItem> items)
    {
        foreach (var item in items)
        {
            if (item.GetBadgeCountAsync is not null)
                item.BadgeCount = await item.GetBadgeCountAsync(services);

            if (item.SubItems.Count > 0)
                await PopulateAsync(item.SubItems);
        }
        items.RemoveAll(item => item.HideWhenBadgeEmpty && item.BadgeCount <= 0);
    }

    internal Task<int> GetOverdueCountAsync(ClaimsPrincipal user)
    {
        var now = DateTime.Now;
        var overdueBefore = now.AddDays(-OverdueDealRules.OverdueAfterDays);
        var restrictToOwnDeals = user.IsInRole(AppRoles.SalesExpert) &&
            !user.IsInRole(AppRoles.SalesAdmin) && !user.IsInRole(AppRoles.Admin) &&
            !user.IsInRole(AppRoles.ManagingDirector);
        var creatorId = user.FindFirstValue(ClaimTypes.NameIdentifier);

        return db.Deals.AsNoTracking().CountAsync(deal =>
            deal.Status == DealStatusEnum.باز &&
            deal.CreateDate >= OverdueDealRules.StartDate &&
            deal.CreateDate <= overdueBefore &&
            (!deal.OverdueFollowUpAt.HasValue || deal.OverdueFollowUpAt <= now) &&
            (!restrictToOwnDeals || deal.UserCreatorID == creatorId));
    }

    internal async Task<int> GetWarningCountAsync(ClaimsPrincipal user)
    {
        var result = await warnings.GetAllWarnings(user);
        return result?.Count ?? 0;
    }
}
