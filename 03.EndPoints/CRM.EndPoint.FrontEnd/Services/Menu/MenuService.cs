using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.ReleaseNotes.Services;
using CRM.Core.Domain.ReleaseNotes.Repositories;
using CRM.EndPoint.FrontEnd.IServices.Menu;
using CRM.EndPoint.FrontEnd.Models.Menu;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

public class MenuService(
    MenuDefinitionService definitions,
    MenuBadgeService badges,
    MenuFavoritesService favorites,
    ReleaseNoteAccess releaseNoteAccess,
    IReleaseNoteRepository releaseNotes) : IMenuService
{
    public async Task<List<MenuItem>> GetMenusAsync(ClaimsPrincipal user)
    {
        var roles = user.Claims.Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value).ToHashSet();

        var menus = MenuAccessFilter.Filter(definitions.BuildMenus(user, roles), roles);
        await SetReleaseVersionAsync(menus);
        await badges.PopulateAsync(menus);
        await favorites.AttachAsync(menus, user);
        return menus;
    }

    private async Task SetReleaseVersionAsync(List<MenuItem> menus)
    {
        var version = menus.FirstOrDefault(item => item.Id == "releaseNotes");
        if (version is null) return;

        var reader = await releaseNoteAccess.ReaderAsync(default);
        var latest = await releaseNotes.GetLatestVisibleVersionInfoAsync(reader, DateTime.UtcNow, default);
        version.Title = latest is null ? "نسخه" : $"نسخه {latest.Version}";
        if (latest?.PublishDate is DateTime publishDate)
            version.Subtitle = CRM.Framework.BaseServices.GetDatePersian(publishDate);
    }
}
