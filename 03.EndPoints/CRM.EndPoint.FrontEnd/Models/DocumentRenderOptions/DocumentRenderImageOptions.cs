using System.Collections.Generic;
using Microsoft.Playwright;

namespace CRM.EndPoint.FrontEnd.Models.DocumentRenderOptions;

public class DocumentRenderImageOptions
{
    /// <summary>
    /// اگر null باشد کل صفحه Screenshot گرفته می‌شود.
    /// در غیر اینصورت از هر Selector یک تصویر تولید می‌شود.
    /// </summary>
    public IEnumerable<string> Selectors { get; init; }

    public ViewportSize Viewport { get; init; }

    public LocatorScreenshotOptions LocatorOptions { get; init; } = new();
    public PageScreenshotOptions PageOptions { get; init; } = new();
}