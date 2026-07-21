using Microsoft.Playwright;

namespace CRM.EndPoint.FrontEnd.Models.DocumentRenderOptions;

public class DocumentRenderPdfOptions {
    public ViewportSize Viewport { get; init; }
    public PagePdfOptions PdfOptions { get; init; } = new();
}