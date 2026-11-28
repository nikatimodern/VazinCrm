using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemorySettings;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxInvoices;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxDispatch;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPreviewSnapshot;
using CRM.Core.ApplicationServices.Taxation.Queries.PreviewTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxMemories;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxProducts;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxReport;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.EndPoint.FrontEnd.Integrations.Moadian;
using CRM.EndPoint.FrontEnd.Models.Taxation;
using CRM.EndPoint.FrontEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = AppRoles.Admin), Route("Taxation")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ServiceFilter(typeof(TaxationAuditFilter))]
public sealed class TaxationPageController(ICqrsDispatcher dispatcher, IWebHostEnvironment environment, IConfiguration configuration,
    ITaxSigningBindings bindings, ITaxSandboxAccess access = null)
    : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string search, bool? export, string state, int page = 1, CancellationToken ct = default)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, configuration)) return NotFound();
        var unqueued = state == "unqueued";
        TaxDispatchStatus? status = null;
        if (!unqueued && !string.IsNullOrEmpty(state))
        {
            if (Enum.TryParse<TaxDispatchStatus>(state, out var parsed) && Enum.IsDefined(parsed))
                status = parsed;
            else
                ModelState.AddModelError(nameof(state), "وضعیت مالیاتی معتبر نیست.");
        }

        if (!ModelState.IsValid)
            return InvalidFilters(search, export, state);

        try
        {
            var invoices = await dispatcher.QueryAsync(new ListTaxInvoicesQuery(search, export, status, unqueued, page), ct);
            return View(new TaxationListModel(invoices, search, export, state));
        }
        catch (ArgumentException)
        {
            return InvalidFilters(search, export, state);
        }
    }

    [HttpGet("memories")]
    public async Task<IActionResult> Memories([FromServices] ITaxMemoryCredentials credentials, [FromServices] ITaxMemoryReadiness legacy, CancellationToken ct)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, configuration)) return NotFound();
        var memories = await dispatcher.QueryAsync(new ListTaxMemoriesQuery(), ct);
        var rows = memories.Select(memory =>
        {
            if (memory.SellerEconomicCode is null)
                return new TaxMemoryRow(memory, legacy.DescribeSandbox(memory.Id));
            var credential = credentials.Describe(memory);
            return new TaxMemoryRow(memory, credential.Message, credential.Ready);
        }).ToArray();
        return View(new TaxMemoryAdminPage(rows, credentials.ListCertificates()));
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products(string search, bool unmapped = false, int page = 1, CancellationToken ct = default)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, configuration)) return NotFound();
        if (!ModelState.IsValid || search?.Length > 100)
            return BadRequest("فیلتر کالا معتبر نیست.");
        var products = await dispatcher.QueryAsync(new ListTaxProductsQuery(search, unmapped, page), ct);
        return View(new TaxProductListModel(products, search, unmapped));
    }

    [HttpGet("reports")]
    public async Task<IActionResult> Reports([FromQuery] TaxReportFilter filter, CancellationToken ct)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, configuration)) return NotFound();
        if (!ModelState.IsValid) return BadRequest("فیلترهای گزارش معتبر نیستند.");
        try
        {
            var report = await dispatcher.QueryAsync(new GetTaxReportQuery(filter), ct);
            var memories = await dispatcher.QueryAsync(new ListTaxMemoriesQuery(), ct);
            return View(new TaxationReportModel(filter, report, memories));
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }

    [HttpGet("reports/excel")]
    public async Task<IActionResult> ExportReport([FromQuery] TaxReportFilter filter, CancellationToken ct)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, configuration)) return NotFound();
        if (!ModelState.IsValid) return BadRequest("فیلترهای گزارش معتبر نیستند.");
        try
        {
            var report = await dispatcher.QueryAsync(new GetTaxReportQuery(filter, Export: true), ct);
            return TaxationReportWorkbook.Export(report, filter.Scope);
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }

    [HttpGet("invoices/{id:guid}/preview")]
    public Task<IActionResult> Preview(Guid id, CancellationToken ct) => ShowPreviewAsync(async () =>
        new(await dispatcher.QueryAsync(new PreviewTaxInvoiceQuery(id), ct), null, false));

    [HttpGet("dispatches/{id:guid}")]
    public Task<IActionResult> Dispatch(Guid id, CancellationToken ct, long? historyBefore = null, bool cancelFullReturn = false) => ShowPreviewAsync(async () =>
    {
        var item = await dispatcher.QueryAsync(new GetTaxDispatchQuery(id, historyBefore), ct);
        var snapshot = await dispatcher.QueryAsync(new GetTaxPreviewSnapshotQuery(item.SnapshotId), ct);
        return new(snapshot, item, await HasSandboxBindingAsync(item, ct), HistoryBefore: historyBefore,
            CancelFullReturn: cancelFullReturn && item.Status == TaxDispatchStatus.Accepted &&
                item.Kind == TaxDispatchKind.Original && item.HasWorkFolderEvidence);
    });

    private async Task<IActionResult> ShowPreviewAsync(Func<Task<TaxationPreviewModel>> load)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, configuration)) return NotFound();
        try { return View("Preview", await load()); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (CryptographicException)
        {
            Response.StatusCode = 503;
            return View("Preview", new TaxationPreviewModel(null, null, false, "بازخوانی Snapshot رمزگذاری‌شده امکان‌پذیر نیست."));
        }
    }

    private async Task<bool> HasSandboxBindingAsync(TaxDispatchDetails item, CancellationToken ct)
    {
        try
        {
            var protocol = item.TaxId is not null ? item.Protocol : (await bindings.GetAsync(item.MemoryId, ct)).Protocol;
            return access is null ? TaxationAvailability.TryGetSandboxAddress(environment, configuration, protocol, out _) :
                await access.IsAllowedAsync(item.MemoryId, protocol, ct);
        }
        catch (ArgumentException) { return false; }
    }

    private IActionResult InvalidFilters(string search, bool? export, string state)
    {
        Response.StatusCode = 400;
        return View(nameof(Index), new TaxationListModel(new([], 1, 1, 0), search, export, state,
            "فیلترها معتبر نیستند. شماره یا نام مشتری و وضعیت را بررسی کنید."));
    }
}
