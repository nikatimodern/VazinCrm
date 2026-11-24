using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Queries.GetTaxPreviewSnapshot;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

[ApiController, Route("api/taxation"), Authorize(Roles = "مدیر ارشد"), AutoValidateAntiforgeryToken]
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

    [HttpGet("snapshots/{id:guid}")]
    public Task<IActionResult> GetSnapshot(Guid id, CancellationToken ct) =>
        Run(() => dispatcher.QueryAsync(new GetTaxPreviewSnapshotQuery(id), ct));

    [HttpPost("invoices/{id:guid}/snapshots")]
    public Task<IActionResult> Snapshot(Guid id, CancellationToken ct) =>
        Run(() => dispatcher.CommandAsync(new SaveTaxPreviewCommand(id, User.FindFirstValue(ClaimTypes.NameIdentifier)), ct));

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        if (!environment.IsDevelopment() || !config.GetValue<bool>("Taxation:EnableLocalPreview")) return NotFound();
        try { return Ok(await action()); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
    }
}
