using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Models.Menu;

public class MenuItem {
    public string Id { get; init; }
    public string Title { get; set; }
    public string Url { get; init; }
    public string IconHtml { get; init; }
    public List<string> Roles { get; init; } = [];
    public List<MenuItem> SubItems { get; set; } = [];
    public int DisplayOrder { get; init; }
    public bool ShowInMobileBottomMenu { get; init; }

    public bool ShowInDesktop { get; init; } = true;
    public bool ShowInMobile { get; init; } = true;

    public string OnClick { get; init; }
    public string ElementId { get; init; }
    public string ClassName { get; init; }

    public int? BadgeCount { get; set; }
    public Func<IServiceProvider, Task<int>> GetBadgeCountAsync { get; init; }
    public bool HideWhenBadgeEmpty { get; set; }

    public bool IsStandalone { get; init; } // بیرون از لیست اصلی (مثل هشدارها)
}
