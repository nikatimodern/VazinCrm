using Microsoft.Playwright;

namespace CRM.EndPoint.FrontEnd.Models.DocumentRender;

public class DocumentRenderPdfOptions {
    public ViewportSize Viewport { get; init; }
    public PagePdfOptions PdfOptions { get; init; } = new();
}