using System.Collections.Generic;
using System.Linq;
using CRM.EndPoint.FrontEnd.Models.Menu;

namespace CRM.EndPoint.FrontEnd.Services.Menu;

internal static class MenuAccessFilter
{
    internal static List<MenuItem> Filter(List<MenuItem> menus, HashSet<string> userRoles, IReadOnlyDictionary<string, bool>? catalogViews = null) =>
        FilterItems(menus, userRoles, [], catalogViews ?? new Dictionary<string, bool>(), true);

    private static List<MenuItem> FilterItems(List<MenuItem> items, HashSet<string> userRoles,
        List<string> parentRoles, IReadOnlyDictionary<string, bool> catalogViews, bool parentLegacyAllowed)
    {
        var result = new List<MenuItem>();
        foreach (var item in items ?? [])
        {
            var effectiveRoles = item.Roles.Count > 0 ? item.Roles : parentRoles;
            var legacyAllowed = parentLegacyAllowed && (effectiveRoles.Count == 0 || effectiveRoles.Any(userRoles.Contains));
            var catalogKey = string.IsNullOrEmpty(item.Url) ? "#" + item.Id : item.Url;
            var allowed = catalogViews.TryGetValue(catalogKey, out var granted) ? granted : legacyAllowed;
            var children = FilterItems(item.SubItems, userRoles, effectiveRoles, catalogViews, legacyAllowed);
            item.SubItems = children;

            // A denied direct action must not be reintroduced by an allowed descendant.
            var hasAction = !string.IsNullOrEmpty(item.Url) || !string.IsNullOrEmpty(item.OnClick);
            if (item.Id == "settings" ? children.Count > 0 : hasAction ? allowed : children.Count > 0)
                result.Add(item);
        }
        return result.OrderBy(x => x.DisplayOrder).ToList();
    }

}

