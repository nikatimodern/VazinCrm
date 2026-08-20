using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices;
using CRM.EndPoint.FrontEnd.Models.DocumentRender;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright;

namespace CRM.EndPoint.FrontEnd.Services;

public class DocumentRenderService : IDocumentRenderService {
    public async Task<byte[]> GeneratePdfAsync(HttpRequest request, string url, PagePdfOptions options = null)
    {
        options ??= new PagePdfOptions();

        options.Format ??= PaperFormat.A4;
        options.PrintBackground ??= true;
        options.PreferCSSPageSize ??= true;

        options.Margin ??= new Margin { Top = "0", Bottom = "0", Left = "0", Right = "0" };

        return await ExecuteAsync(request, url, null, page => page.PdfAsync(options));
    }

    public async Task<IReadOnlyList<byte[]>> GenerateImagesAsync(HttpRequest request, string url, DocumentRenderImageOptions options = null)
    {
        options ??= new DocumentRenderImageOptions();

        options.PageOptions.Type ??= ScreenshotType.Png;  // FullPage
        options.PageOptions.FullPage ??= true;
        options.PageOptions.Scale ??= ScreenshotScale.Device;
        options.PageOptions.Animations ??= ScreenshotAnimations.Disabled;
        options.PageOptions.Caret ??= ScreenshotCaret.Hide;

        options.LocatorOptions.Type ??= ScreenshotType.Png; // ٍElement with selector
        options.LocatorOptions.Scale ??= ScreenshotScale.Device;
        options.LocatorOptions.Animations ??= ScreenshotAnimations.Disabled;
        options.LocatorOptions.Caret ??= ScreenshotCaret.Hide;

        return await ExecuteAsync(
            request,
            url,
            options.Viewport,
            async page => {
                var images = new List<byte[]>();

                if (options.Selectors == null || !options.Selectors.Any())
                {
                    images.Add(await page.ScreenshotAsync(options.PageOptions));
                    return images;
                }

                foreach (var selector in options.Selectors ?? [])
                {
                    var locator = page.Locator(selector);

                    var count = await locator.CountAsync();

                    for (var i = 0; i < count; i++)
                    {
                        images.Add(await locator
                            .Nth(i)
                            .ScreenshotAsync(options.LocatorOptions));
                    }
                }

                return images;
            });
    }

    public async Task<byte[]> GenerateImageAsync(HttpRequest request, string url, DocumentRenderImageOptions options = null)
    {
        return (await GenerateImagesAsync(request, url, options)).First();
    }

    private static async Task<T> ExecuteAsync<T>(HttpRequest request, string url, ViewportSize viewport, Func<IPage, Task<T>> action)
    {
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions {
            Locale = "fa-IR",
            DeviceScaleFactor = 3
        });

        await context.AddCookiesAsync(
            request.Cookies.Select(c => new Cookie {
                Name = c.Key,
                Value = c.Value,
                Domain = request.Host.Host,
                Path = "/"
            })
        );

        var page = await context.NewPageAsync();

        if (viewport != null)
            await page.SetViewportSizeAsync(viewport.Width, viewport.Height);

        await page.GotoAsync(url, new PageGotoOptions {
            WaitUntil = WaitUntilState.NetworkIdle
        });

        await page.EmulateMediaAsync(new PageEmulateMediaOptions {
            Media = Media.Print
        });

        return await action(page);
    }


    public async Task<byte[]> CreateZipAsync(IEnumerable<byte[]> files, Func<int, string> fileNameFactory)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(fileNameFactory);

        using var memory = new MemoryStream();

        await using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            var index = 0;

            foreach (var file in files)
            {
                var entry = archive.CreateEntry(fileNameFactory(index++));

                await using var stream = await entry.OpenAsync();
                await stream.WriteAsync(file);
            }
        }

        return memory.ToArray();
    }
}