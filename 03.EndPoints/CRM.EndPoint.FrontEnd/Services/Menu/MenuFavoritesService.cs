using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.Menu;
using Generics.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

public sealed class MenuFavoritesService(UserContextDb db)
{
    public async Task AttachAsync(List<MenuItem> menus, ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return;

        var favorites = await db.MenuFavorites.AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.SortOrder).ThenBy(f => f.MenuId)
            .ToListAsync();

        var links = new Dictionary<string, (MenuItem Item, string Icon)>(StringComparer.Ordinal);
        CollectLinks(menus, links);
        var favoriteIds = favorites.Select(f => f.MenuId).ToHashSet(StringComparer.Ordinal);
        foreach (var entry in links.Values)
            entry.Item.IsFavorite = favoriteIds.Contains(entry.Item.Id);

        var visible = favorites.Where(f => links.ContainsKey(f.MenuId)).Select(f => {
            var (source, icon) = links[f.MenuId];
            return new MenuItem {
                Id = "favorite-" + source.Id, FavoriteTargetId = source.Id,
                Title = source.Title, Url = source.Url, IconHtml = icon,
                IsFavorite = true, DisplayOrder = f.SortOrder
            };
        }).ToList();
        if (visible.Count == 0) return;

        menus.Add(new MenuItem {
            Id = "favorites", Title = "علاقه‌مندی‌ها", IconHtml = MenuIcons.Tasks,
            DisplayOrder = 0, IsFavoritesGroup = true, SubItems = visible
        });
        menus.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
    }

    private static void CollectLinks(IEnumerable<MenuItem> nodes,
        Dictionary<string, (MenuItem Item, string Icon)> links, string parentIcon = "")
    {
        foreach (var item in nodes)
        {
            if (item.Id == "overdueOpenDeals") continue;
            if (!item.ShowInDesktop && item.Id != "extraSettings") continue;

            var icon = string.IsNullOrEmpty(item.IconHtml) ? parentIcon : item.IconHtml;
            if (!string.IsNullOrEmpty(item.Url) && !links.ContainsKey(item.Id))
            {
                item.FavoriteTargetId = item.Id;
                links.Add(item.Id, (item, icon));
            }
            CollectLinks(item.SubItems, links, icon);
        }
    }
}
