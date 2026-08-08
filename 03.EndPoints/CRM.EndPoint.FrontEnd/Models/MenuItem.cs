using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models;

public class MenuItem
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string Url { get; init; }
    public string IconHtml { get; init; }
    public List<string> Roles  { get; set; } = [];
    public List<MenuItem> SubItems { get; set; } = [];
    public int DisplayOrder { get; init; }
    public bool ShowInMobileBottomMenu { get; init; }

    public bool ShowInDesktop { get; init; } = true;
    public bool ShowInMobile { get; init; } = true;

    public string OnClick { get; init; }
    public string ElementId { get; init; }
    public string ClassName { get; init; }
    public bool HasBadge { get; init; }
    public bool IsStandalone { get; init; }   // بیرون از لیست اصلی (مثل هشدارها)

}