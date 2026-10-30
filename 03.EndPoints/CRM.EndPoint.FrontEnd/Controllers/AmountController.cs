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
public sealed class AmountController(ICqrsDispatcher requests) : Controller
{
    [Authorize(Roles = "کارشناس فروش ,مدیر فروش , مدیر تامین , مدیر ارشد , کارشناس تامین")]
    public async Task<IActionResult> index(CancellationToken ct) => View(await requests.QueryAsync(new GetWorkspacePageQuery("Amount"), ct));
    [HttpGet]
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

    [HttpPost]
    public async Task<IActionResult> AddAmount([FromBody] ProductAmount model, CancellationToken ct) => StatusCode(await requests.CommandAsync(new SaveProductPriceCommand(model), ct));
    [HttpGet]
    public async Task<IActionResult> DelAmount(Guid id, CancellationToken ct) => StatusCode(await requests.CommandAsync(new DeleteProductPriceCommand(id), ct));
}
