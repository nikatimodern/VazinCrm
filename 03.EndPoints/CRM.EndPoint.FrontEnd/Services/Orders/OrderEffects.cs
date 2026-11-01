using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Orders.Abstractions;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Sms.Enums;
using CRM.EndPoint.FrontEnd.IServices.DocumentRender;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.DocumentRender;
using CRM.EndPoint.FrontEnd.Models.Sms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Playwright;

namespace CRM.EndPoint.FrontEnd.Services.Orders;
public sealed class OrderEffects(IViewRenderService views, IDocumentRenderService documents, IHttpContextAccessor context, LinkGenerator links, ISmsQueue sms) : IOrderEffects
{
    public Task<string> RenderColumnAsync(List<OrderHead> heads, int passType, CancellationToken ct) => views.RenderToStringAsync("GetAllHeadOrdersPartial", heads, new Dictionary<string, object> { { "PassType", passType } });
    public Task<byte[]> RenderDocumentAsync(Guid id, bool image, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var http = context.HttpContext ?? throw new InvalidOperationException("An HTTP request is required.");
        var url = links.GetUriByAction(http, "PreviewPrint", "Order", new { id }) ?? throw new InvalidOperationException("Order print route not found.");
        return image ? documents.GenerateImageAsync(http.Request, url, new DocumentRenderImageOptions { Viewport = ViewportPresets.A5Landscape, Selectors = [".sheet"] }) : documents.GeneratePdfAsync(http.Request, url, new PagePdfOptions { Landscape = true, Format = PaperFormat.A5 });
    }

    public Task QueueReadyMessageAsync(string mobile, string serialNumber, CancellationToken ct) => sms.QueueAsync(new SmsJob { PatternType = SmsPatternType.ReadyToDeliverToCustomer, Mobile = mobile, SerialNumber = serialNumber });
}
