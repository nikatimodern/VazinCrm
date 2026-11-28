using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Models.Menu;

public class MenuItem {
    public string Id { get; init; }
    public MenuDisplayType DisplayType { get; init; } = MenuDisplayType.Link;
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public string Url { get; set; }
    public string IconHtml { get; init; }
    public List<string> Roles { get; set; } = [];
    public List<MenuItem> SubItems { get; set; } = [];
    public int DisplayOrder { get; set; }
    public bool ShowInMobileBottomMenu { get; set; }

    public bool ShowInDesktop { get; set; } = true;
    public bool ShowInMobile { get; init; } = true;

    public string FavoriteTargetId { get; set; }
    public bool IsFavorite { get; set; }
    public bool IsFavoritesGroup { get; init; }

    public string OnClick { get; init; }
    public string ElementId { get; init; }
    public string ClassName { get; init; }

    public int? BadgeCount { get; set; }
    public Func<IServiceProvider, Task<int>> GetBadgeCountAsync { get; init; }
    public bool HideWhenBadgeEmpty { get; set; }

    public bool IsStandalone { get; init; } // بیرون از لیست اصلی (مثل هشدارها)
}
