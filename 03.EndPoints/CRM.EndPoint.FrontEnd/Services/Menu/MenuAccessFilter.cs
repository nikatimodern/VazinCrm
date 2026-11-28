using System.Collections.Generic;
using System.Linq;
using CRM.EndPoint.FrontEnd.Models.Menu;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

internal static class MenuAccessFilter
{
    internal static List<MenuItem> Filter(List<MenuItem> menus, HashSet<string> userRoles) =>
        FilterItems(menus, userRoles, []);

    private static List<MenuItem> FilterItems(List<MenuItem> items, HashSet<string> userRoles,
        List<string> parentRoles)
    {
        var visible = new List<MenuItem>();
        foreach (var item in items ?? [])
        {
            var effectiveRoles = item.Roles.Count > 0 ? item.Roles : parentRoles;
            if (effectiveRoles.Count > 0 && !effectiveRoles.Any(userRoles.Contains))
                continue;

            item.SubItems = FilterItems(item.SubItems, userRoles, effectiveRoles);

            if (string.IsNullOrEmpty(item.Url) && string.IsNullOrEmpty(item.OnClick) && item.SubItems.Count == 0)
                continue;

            visible.Add(item);
        }
        return visible.OrderBy(item => item.DisplayOrder).ToList();
    }
}
