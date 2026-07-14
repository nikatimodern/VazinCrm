using System.Collections.Generic;

namespace CRM.EndPoint.FrontEnd.Models;

public class MenuItem
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public string IconHtml { get; set; }
    public List<string> Roles  { get; set; } = [];
    public List<MenuItem> SubItems { get; set; } = [];
    public int DisplayOrder { get; set; }
    public bool ShowInMobileBottomMenu { get; set; }

    public bool ShowInDesktop { get; set; } = true;
    public bool ShowInMobile { get; set; } = true;

    public string OnClick { get; set; }
    public string ElementId { get; set; }
    public string ClassName { get; set; }
    public bool HasBadge { get; set; }
    public bool IsStandalone { get; set; }   // بیرون از لیست اصلی (مثل هشدارها)

}