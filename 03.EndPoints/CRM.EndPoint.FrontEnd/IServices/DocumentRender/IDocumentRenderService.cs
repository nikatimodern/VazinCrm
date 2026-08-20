using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Models.DocumentRender;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright;

namespace CRM.EndPoint.FrontEnd.IServices.DocumentRender;

public interface IDocumentRenderService {
    Task<byte[]> GeneratePdfAsync(HttpRequest request, string url, PagePdfOptions options = null);

    Task<IReadOnlyList<byte[]>> GenerateImagesAsync(HttpRequest request, string url, DocumentRenderImageOptions options = null);

    Task<byte[]> GenerateImageAsync(HttpRequest request, string url, DocumentRenderImageOptions options = null);

    Task<byte[]> CreateZipAsync(IEnumerable<byte[]> files, Func<int, string> fileNameFactory);

}