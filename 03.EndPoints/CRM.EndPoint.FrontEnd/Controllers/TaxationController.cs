using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxPayment;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPayment;
using CRM.Core.ApplicationServices.Security;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxReference;
using CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxSubmission;
using CRM.Core.ApplicationServices.Taxation.Commands.InquireTaxWorkFolder;
using CRM.Core.ApplicationServices.Taxation.Submission;
using System.Security.Cryptography;
using CRM.Core.ApplicationServices.Taxation.Commands.EnqueueTaxInvoice;
using CRM.Core.Domain.Taxation;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPreviewSnapshot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxMemory;
using CRM.Core.ApplicationServices.Taxation.Commands.ActivateTaxMemory;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxMemories;
using CRM.Core.ApplicationServices.Taxation.Queries.PreviewTaxInvoice;
using CRM.Core.ApplicationServices.Taxation.Commands.SaveTaxPreview;
using CRM.Core.ApplicationServices.Taxation.Commands.SetTaxProductMapping;

namespace CRM.EndPoint.FrontEnd.Controllers;

[ApiController, Route("api/taxation"), Authorize(Roles = AppRoles.Admin), AutoValidateAntiforgeryToken]
public sealed class TaxationController(ICqrsDispatcher dispatcher, IWebHostEnvironment environment, IConfiguration config) : ControllerBase
{
    [HttpGet("memories")]
    public Task<IActionResult> Memories(CancellationToken ct) =>
        Run(() => dispatcher.QueryAsync(new ListTaxMemoriesQuery(), ct));

    [HttpPost("memories")]
    public Task<IActionResult> SaveMemory(SaveTaxMemoryCommand command, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(command, ct));

    [HttpPost("memories/{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(new ActivateTaxMemoryCommand(id), ct));

    [HttpPost("product-mappings")]
    public Task<IActionResult> MapProduct(SetTaxProductMappingCommand command, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(command, ct));

    [HttpGet("invoices/{id:guid}/preview")]
    public Task<IActionResult> Preview(Guid id, CancellationToken ct) =>
        Run(() => dispatcher.QueryAsync(new PreviewTaxInvoiceQuery(id), ct));

    [HttpPost("invoices/{id:guid}/preview")]
    public Task<IActionResult> PreviewExport(Guid id, ExportTaxDetails export, CancellationToken ct) =>
        Run(() => dispatcher.QueryAsync(new PreviewTaxInvoiceQuery(id, export), ct));

    [HttpGet("snapshots/{id:guid}")]
    public Task<IActionResult> GetSnapshot(Guid id, CancellationToken ct) =>
        Run(() => dispatcher.QueryAsync(new GetTaxPreviewSnapshotQuery(id), ct));

    [HttpPost("invoices/{id:guid}/snapshots")]
    public Task<IActionResult> Snapshot(Guid id, [FromBody] TaxPreviewVersion body, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(new SaveTaxPreviewCommand(id, User.FindFirstValue(ClaimTypes.NameIdentifier), body.Version, body.Export), ct));

    [HttpPost("invoices/{id:guid}/queue")]
    public Task<IActionResult> Enqueue(Guid id, [FromBody] TaxPreviewVersion body, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(new EnqueueTaxInvoiceCommand(id, User.FindFirstValue(ClaimTypes.NameIdentifier), body.Version, body.Export), ct));

    [HttpPost("dispatches/{id:guid}/references")]
    public Task<IActionResult> EnqueueReference(Guid id, TaxReferenceRequest body, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(new EnqueueTaxReferenceCommand(id,
            User.FindFirstValue(ClaimTypes.NameIdentifier), body), ct));

    [HttpPost("dispatches/{id:guid}/payments")]
    public Task<IActionResult> EnqueuePayment(Guid id, TaxPaymentDetails details, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(new EnqueueTaxPaymentCommand(id,
            User.FindFirstValue(ClaimTypes.NameIdentifier), details), ct));

    [HttpPost("payments/{id:guid}/submit")]
    public Task<IActionResult> SubmitPayment(Guid id, CancellationToken ct) =>
        RunSandbox(() => dispatcher.CommandAsync(new SubmitTaxPaymentCommand(id), ct));

    [HttpGet("payments/{id:guid}")]
    public Task<IActionResult> PaymentState(Guid id, CancellationToken ct) =>
        Run(() => dispatcher.QueryAsync(new GetTaxPaymentQuery(id), ct));

    [HttpPost("dispatches/{id:guid}/submit")]
    public Task<IActionResult> Submit(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] TaxFilingDetails filing, CancellationToken ct) =>
        RunSandbox(() => dispatcher.CommandAsync(new SubmitTaxInvoiceCommand(id, filing), ct));

    [HttpPost("dispatches/{id:guid}/inquire")]
    public Task<IActionResult> Inquire(Guid id, CancellationToken ct) =>
        RunSandbox(() => dispatcher.CommandAsync(new InquireTaxSubmissionCommand(id), ct));

    [HttpPost("dispatches/{id:guid}/work-folder")]
    public Task<IActionResult> WorkFolder(Guid id, CancellationToken ct) =>
        RunSandbox(() => dispatcher.CommandAsync(new InquireTaxWorkFolderCommand(id), ct));

    private Task<IActionResult> RunSandbox<T>(Func<Task<T>> action) =>
        config.GetValue<bool>("Taxation:Sandbox:Enabled") ? Run(action) : Task.FromResult<IActionResult>(NotFound());

    public sealed record TaxPreviewVersion(string Version, ExportTaxDetails Export = null);

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        if (!TaxationAvailability.IsLocalEnabled(environment, config)) return NotFound();
        try { return Ok(await action()); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (TaxPreviewConflictException ex) { return Conflict(ex.Message); }
        catch (TaxTransportException) { return StatusCode(502, "پاسخ معتبر از محیط آزمایشی دریافت نشد."); }
        catch (CryptographicException) { return StatusCode(503, "گواهی یا اطلاعات رمزگذاری‌شده در دسترس نیست."); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
