using Microsoft.Playwright;

namespace CRM.EndPoint.FrontEnd.Models.DocumentRenderOptions;

public static class ViewportPresets {
    public static readonly ViewportSize A4Portrait = new() {
        Width = 2480,
        Height = 3508
    };

    public static readonly ViewportSize A4Landscape = new() {
        Width = 3508,
        Height = 2480
    };

    public static readonly ViewportSize A5Portrait = new() {
        Width = 1748,
        Height = 2480
    };

    public static readonly ViewportSize A5Landscape = new() {
        Width = 2480,
        Height = 1748
    };
}