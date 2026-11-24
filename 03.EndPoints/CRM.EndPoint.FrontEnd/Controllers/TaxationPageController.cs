using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.ApplicationServices.Taxation.Queries.ListTaxInvoices;
using CRM.Core.Domain.Taxation;
using CRM.EndPoint.FrontEnd.Composition;
using CRM.EndPoint.FrontEnd.Models.Taxation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = AppRoles.Admin), Route("Taxation")]
public sealed class TaxationPageController(ICqrsDispatcher dispatcher, IWebHostEnvironment environment, IConfiguration configuration)
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

    private IActionResult InvalidFilters(string search, bool? export, string state)
    {
        Response.StatusCode = 400;
        return View(nameof(Index), new TaxationListModel(new([], 1, 1, 0), search, export, state,
            "فیلترها معتبر نیستند. شماره یا نام مشتری و وضعیت را بررسی کنید."));
    }
}
