using CRM.EndPoint.FrontEnd.Security;
using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.ApplicationServices.Amounts.Queries.GetProductPrices;
using CRM.Core.ApplicationServices.Amounts.Commands.SaveProductPrice;
using CRM.Core.ApplicationServices.Amounts.Commands.DeleteProductPrice;
using CRM.Core.Domain.Products.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public sealed class AmountController(ICqrsDispatcher requests, DocumentAccessPolicy access) : Controller
{
    [LegacyCatalogRoles("ProductAmount", "کارشناس فروش ,مدیر فروش , مدیر تامین , مدیر ارشد , کارشناس تامین")]
    [CatalogPermission("ProductAmount", "View")]
    [CatalogPermission("Product", "View")]
    public async Task<IActionResult> index(CancellationToken ct) => View(await requests.QueryAsync(new GetWorkspacePageQuery("Amount"), ct));
    [HttpGet]
    [CatalogPermission("ProductAmount", "View")]
    [CatalogPermission("Product", "View")]
    public async Task<IActionResult> ListProduct(string searchValue, string from = "amountIndex", CancellationToken ct = default)
    {
        try
        {
            var result = await requests.QueryAsync(new GetProductPricesQuery(searchValue, from), ct);
            ViewBag.hasAccess = result.HasAccess;
            ViewBag.from = result.From;
            return PartialView(result.Products);
        }
        catch (Exception)when (!ct.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAmount([FromBody] ProductAmount model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.HasAsync("Product", "View", ct) || !await access.HasAsync("ProductAmount", "View", ct) ||
            !await access.HasAsync("ProductAmount", model.Id == Guid.Empty ? "Create" : "Edit", ct)) return Forbid();
        return StatusCode(await requests.CommandAsync(new SaveProductPriceCommand(model), ct));
    }
    [HttpPost, ValidateAntiForgeryToken]
    [CatalogPermission("Product", "View")]
    [CatalogPermission("ProductAmount", "Delete")]
    public async Task<IActionResult> DelAmount(Guid id, CancellationToken ct) => StatusCode(await requests.CommandAsync(new DeleteProductPriceCommand(id), ct));
}
