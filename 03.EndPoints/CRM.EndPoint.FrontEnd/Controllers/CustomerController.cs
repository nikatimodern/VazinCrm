using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using CRM.Core.Domain.AccessCatalog.Entities;
using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.EndPoint.FrontEnd.Security;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Customers.Dtos;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Repositories;
using CRM.EndPoint.FrontEnd.Models.Customer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomersPage;
using CRM.Core.ApplicationServices.Customers.Queries.SearchCustomerOptions;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerSuppliers;
using CRM.Core.ApplicationServices.Customers.Queries.GetRelatedCustomers;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerProfile;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerDetail;
using CRM.Core.ApplicationServices.Customers.Queries.CheckCustomerRegistration;
using CRM.Core.ApplicationServices.Customers.Queries.FindDuplicateCustomers;
using CRM.Core.ApplicationServices.Customers.Queries.GetJuridicalCustomerTable;
using CRM.Core.ApplicationServices.Customers.Queries.GetIndividualCustomerTable;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerCards;
using CRM.Core.ApplicationServices.Customers.Queries.SearchCustomerCards;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerUserNames;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerSummaries;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomer;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerByMobile;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerExperts;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerPhonebook;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerSepidarInvoices;
using CRM.Core.ApplicationServices.Customers.Queries.GetCustomerPreFactors;
using CRM.Core.ApplicationServices.Customers.Queries.GetPreFactorUsers;
using CRM.Core.ApplicationServices.Customers.Commands.RemoveCustomerExpert;
using CRM.Core.ApplicationServices.Customers.Commands.AssignCustomerExperts;
using CRM.Core.ApplicationServices.Customers.Commands.DeleteCustomer;
using CRM.Core.ApplicationServices.Customers.Commands.AcceptCustomer;
using CRM.Core.ApplicationServices.Customers.Commands.AddCustomerToCurrentUser;
using CRM.Core.ApplicationServices.Customers.Commands.UpdateCustomer;
using CRM.Core.ApplicationServices.Customers.Commands.CreateCustomer;
using CRM.Core.ApplicationServices.Customers.Commands.NormalizeCustomers;
using CRM.Core.ApplicationServices.Customers.Commands.ChangePreFactorUsers;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize]
public sealed class CustomerController(
    ICqrsDispatcher requests, IAccessScopeService accessScopes, ICustomerRepository customers,
    IConfiguration configuration) : Controller
{
    // Opt-in while the remaining ERP controllers are migrated from legacy roles.
    private bool Enforce => configuration.GetValue<bool>("AccessCatalog:EnforceCustomerPermissions");
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    private Task<byte> ScopeAsync(string permission, CancellationToken ct) =>
        Enforce ? accessScopes.GetEffectiveScopeAsync(UserId, "Customer." + permission, ct)
                : Task.FromResult(AccessScopes.All);

    private Task<bool> CanAccessAsync(string permission, Guid personId, CancellationToken ct) =>
        !Enforce ? Task.FromResult(true) :
        accessScopes.CanAccessCustomerAsync(UserId, "Customer." + permission, personId, ct);

    // Guid.Empty filters to no rows when Identity uses an unexpected identifier format.
    private Guid? OwnFilter(byte scope) => Enforce && scope == AccessScopes.Own
        ? (Guid.TryParse(UserId, out var id) ? id : Guid.Empty) : null;

    private bool? DirectoryReadAll(byte scope) => Enforce ? scope == AccessScopes.All : null;

    private async Task<bool> CanEditAllAsync(CancellationToken ct) =>
        !Enforce || await ScopeAsync("Edit", ct) == AccessScopes.All;

    private async Task<bool> CanViewPreFactorAsync(Guid pfId, CancellationToken ct)
    {
        if (!Enforce) return true;
        var pf = await customers.GetPreFactorAsync(pfId, ct);
        return pf?.CustomerId.HasValue == true && await CanAccessAsync("View", pf.CustomerId.Value, ct);
    }

    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var viewScope = await ScopeAsync("View", cancellationToken);
        if (viewScope == AccessScopes.None) return Forbid();
        ViewBag.AllowCreate = !Enforce || await ScopeAsync("Create", cancellationToken) != AccessScopes.None;
        ViewBag.AllowEdit = !Enforce || await ScopeAsync("Edit", cancellationToken) != AccessScopes.None;
        ViewBag.ScopeEnforced = Enforce;
        var user = await requests.QueryAsync(new GetCustomersPageQuery(), cancellationToken);
        ViewBag.SubTitle = " فرم ثبت شخص"; ViewBag.Head = "Customer";
        return View(user);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> RemoveUserCustomer([FromBody] UserCustomer model, CancellationToken cancellationToken)
    {
        if (model is null || model.CustomerId == Guid.Empty) return BadRequest();
        if (!await CanEditAllAsync(cancellationToken)) return Forbid();
        return await Mutation(requests.CommandAsync(new RemoveCustomerExpertCommand(model.CustomerId, model.UserId), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> AddSelsToCustomers([FromBody] SelsCustomerVModel model, CancellationToken cancellationToken)
    {
        if (model is null || model.customerId == Guid.Empty) return BadRequest();
        if (!await CanEditAllAsync(cancellationToken)) return Forbid();
        return await Mutation(requests.CommandAsync(new AssignCustomerExpertsCommand(model.customerId, model.selExperts), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpGet]
    public async Task<IActionResult> GetCustomerOp(string search, int? roleType, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        return Ok(await requests.QueryAsync(new SearchCustomerOptionsQuery(search, roleType, OwnFilter(scope)), cancellationToken));
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpGet]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        return Ok(await requests.QueryAsync(new GetCustomerSuppliersQuery(OwnFilter(scope)), cancellationToken));
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpGet]
    public async Task<IActionResult> GetCustomerRel(Guid Id, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None || !await CanAccessAsync("View", Id, cancellationToken)) return Forbid();
        var data = await requests.QueryAsync(new GetRelatedCustomersQuery(Id, OwnFilter(scope)), cancellationToken);
        return data.Count == 0 ? Ok(null) : PartialView("DuplicatePartialDetail", data);
    }
    [HttpGet]
    public async Task<IActionResult> CustomerProfile(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("View", id, cancellationToken)) return Forbid();
        var result = await requests.QueryAsync(new GetCustomerProfileQuery(id), cancellationToken);
        return result.Status switch
        {
            400 => BadRequest(),
            403 => Forbid(),
            404 => NotFound(),
            _ => View(new CustomerPanelVModel { CustomerId = result.Id, CustomerName = result.Name, SelExpertRel = result.SalesExperts })
        };
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.Managers), HttpGet]
    public async Task<IActionResult> CustomerDetail(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("View", id, cancellationToken)) return Forbid();
        var data = await requests.QueryAsync(new GetCustomerDetailQuery(id), cancellationToken);
        if (id == Guid.Empty) return BadRequest();
        ViewBag.CustomerId = id;
        return View(data);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AbortCustomer(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("Delete", id, cancellationToken)) return Forbid();
        return await Mutation(requests.CommandAsync(new DeleteCustomerCommand(id), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.Managers), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptCustomer(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanEditAllAsync(cancellationToken)) return Forbid();
        return await Mutation(requests.CommandAsync(new AcceptCustomerCommand(id), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToMyCustomer(Guid id, CancellationToken cancellationToken)
    {
        // Self-assignment must not be a route around Own-only access.
        if (!await CanEditAllAsync(cancellationToken)) return Forbid();
        return await Mutation(requests.CommandAsync(new AddCustomerToCurrentUserCommand(id), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> CheckCus(Customer model, CancellationToken cancellationToken)
    {
        if (model is null) return BadRequest();
        if (await ScopeAsync("View", cancellationToken) == AccessScopes.None ||
            (Enforce && await ScopeAsync("Create", cancellationToken) == AccessScopes.None &&
                        await ScopeAsync("Edit", cancellationToken) == AccessScopes.None))
            return Forbid();
        var exists = await requests.QueryAsync(new CheckCustomerRegistrationQuery(model), cancellationToken);
        return exists.HasValue ? Ok(new { Status = exists.Value ? 500 : 200, error = exists.Value ? "مشتری قبلا در سیستم ثبت شده است" : "" }) : Ok();
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> CheckDuplicate([FromBody] Customer model, CancellationToken cancellationToken)
    {
        if (model == null) return BadRequest();
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None ||
            (Enforce && await ScopeAsync("Create", cancellationToken) == AccessScopes.None &&
                        await ScopeAsync("Edit", cancellationToken) == AccessScopes.None)) return Forbid();
        var data = await requests.QueryAsync(new FindDuplicateCustomersQuery(model, OwnFilter(scope)), cancellationToken);
        return data.Count == 0 ? Ok(null) : PartialView("DuplicatePartial", data);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> UpCustomer([FromBody] Customer model, CancellationToken cancellationToken)
    {
        if (model is null || model.Id == Guid.Empty) return BadRequest();
        if (!await CanAccessAsync("Edit", model.Id, cancellationToken)) return Forbid();
        return await Mutation(requests.CommandAsync(new UpdateCustomerCommand(model), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> AddCustomer([FromBody] Customer model, CancellationToken cancellationToken)
    {
        if (model is null) return BadRequest();
        if (await ScopeAsync("Create", cancellationToken) == AccessScopes.None) return Forbid();
        return await Mutation(requests.CommandAsync(new CreateCustomerCommand(model), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> GetAllCustomerJuridical(CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        var request = (await ReadTableAsync(cancellationToken)) with { OnlyAssignedUserId = OwnFilter(scope) };
        return Ok(await requests.QueryAsync(new GetJuridicalCustomerTableQuery(request), cancellationToken));
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpPost]
    public async Task<IActionResult> GetAllCustomerNonJuridical(CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        var request = (await ReadTableAsync(cancellationToken)) with { OnlyAssignedUserId = OwnFilter(scope) };
        return Ok(await requests.QueryAsync(new GetIndividualCustomerTableQuery(request), cancellationToken));
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly)]
    public async Task<IActionResult> GetAllCustomerPartial(int skip = 0, int take = 10, int roleType = -1, int juridical = -1, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        var result = await requests.QueryAsync(new GetCustomerCardsQuery(skip, take, roleType, juridical, DirectoryReadAll(scope)), cancellationToken);
        ViewBag.AccessAccepte = Enforce ? await CanEditAllAsync(cancellationToken) : result.CanAccept;
        ViewBag.EditScope = await ScopeAsync("Edit", cancellationToken);
        ViewBag.DeleteScope = await ScopeAsync("Delete", cancellationToken);
        ViewBag.ScopeEnforced = Enforce;
        ViewBag.ActorGuid = Guid.TryParse(UserId, out var actorGuid) ? actorGuid : Guid.Empty;
        return PartialView("GetAllCustomerPartial", result.Customers);
    }
    [NonAction]
    public async Task<string> GetUsersRel(IList<UserCustomer> userCustomers, CancellationToken cancellationToken) =>
        await requests.QueryAsync(new GetCustomerUserNamesQuery(userCustomers.Select(u => u.UserId).ToList()), cancellationToken);
    [HttpPost, Authorize(Roles = "مدیر ارشد")]
    public async Task<IActionResult> NormalizeAllCustomers(CancellationToken cancellationToken)
    {
        if (!await CanEditAllAsync(cancellationToken)) return Forbid();
        await requests.CommandAsync(new NormalizeCustomersCommand(), cancellationToken);
        return Ok();
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly)]
    public async Task<IActionResult> SearchAllCustomerPartial(string val, int roleType = -1, int juridical = -1, CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        var result = await requests.QueryAsync(new SearchCustomerCardsQuery(val, roleType, juridical, DirectoryReadAll(scope)), cancellationToken);
        ViewBag.AccessAccepte = Enforce ? await CanEditAllAsync(cancellationToken) : result.CanAccept;
        ViewBag.EditScope = await ScopeAsync("Edit", cancellationToken);
        ViewBag.DeleteScope = await ScopeAsync("Delete", cancellationToken);
        ViewBag.ScopeEnforced = Enforce;
        ViewBag.ActorGuid = Guid.TryParse(UserId, out var actorGuid) ? actorGuid : Guid.Empty;
        return PartialView("GetAllCustomerPartial", result.Customers);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly)]
    public async Task<IActionResult> GetCustomersList(CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        return Ok(await requests.QueryAsync(new GetCustomerSummariesQuery(OwnFilter(scope)), cancellationToken));
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly)]
    public async Task<IActionResult> GetCustomer(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("View", id, cancellationToken)) return Forbid();
        return Ok(await requests.QueryAsync(new GetCustomerQuery(id), cancellationToken));
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly)]
    public async Task<IActionResult> GetCustomerByMobile(string mobile, CancellationToken cancellationToken)
    {
        if (await ScopeAsync("View", cancellationToken) == AccessScopes.None) return Forbid();
        var person = await requests.QueryAsync(new GetCustomerByMobileQuery(mobile), cancellationToken);
        if (person is null) return Enforce ? NotFound() : Ok(null);
        return await CanAccessAsync("View", person.Id, cancellationToken) ? Ok(person) : Forbid();
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.Managers), HttpGet]
    public async Task<IActionResult> GetCustomerSelRelation(Guid id, CancellationToken cancellationToken)
    {
        if (!await CanAccessAsync("View", id, cancellationToken)) return Forbid();
        return Ok(await requests.QueryAsync(new GetCustomerExpertsQuery(id), cancellationToken));
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpGet]
    public async Task<IActionResult> GetSepidarFactors(int pfSerialNumber, int passType = 1, CancellationToken cancellationToken = default)
    {
        // This endpoint looks up invoices by serial number, not a person ID.
        // Until the invoice-to-person lookup is integrated, Own access is denied.
        if (Enforce && await ScopeAsync("View", cancellationToken) != AccessScopes.All) return Forbid();
        try
        {
            var result = await requests.QueryAsync(new GetCustomerSepidarInvoicesQuery(pfSerialNumber, passType), cancellationToken);
            return result.Status == 204 ? NoContent() : Ok(new { count = result.Count, html = result.Html });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return BadRequest(); }
    }
    [HttpGet]
    public async Task<IActionResult> GetAllPreFactorPartial(Guid? customerId, Guid? dealId, bool showDeleted = false, CancellationToken cancellationToken = default)
    {
        Guid? requiredCustomerId = null;
        if (Enforce)
        {
            if (!customerId.HasValue && !dealId.HasValue) return BadRequest();
            // The query prioritizes DealId; never trust a different CustomerId passed alongside it.
            var personId = dealId.HasValue ? await customers.GetDealCustomerIdAsync(dealId.Value, cancellationToken) : customerId;
            if (!personId.HasValue || (customerId.HasValue && customerId != personId) ||
                !await CanAccessAsync("View", personId.Value, cancellationToken)) return Forbid();
            requiredCustomerId = personId;
        }
        try
        {
            var result = await requests.QueryAsync(new GetCustomerPreFactorsQuery(customerId, dealId, showDeleted, requiredCustomerId), cancellationToken);
            return Ok(new { count = result.Count, html = result.Html });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return BadRequest(); }
    }
    // PreFactor expert reassignment remains admin-only until dedicated PreFactor permissions are mapped.
    [Authorize(Roles = "مدیر ارشد"), HttpGet]
    public async Task<IActionResult> GetUsersForPreFactor(Guid cusId, Guid pfId, CancellationToken cancellationToken)
    {
        if (!await CanEditAllAsync(cancellationToken)) return Forbid();
        if (!await CanViewPreFactorAsync(pfId, cancellationToken)) return Forbid();
        if (Enforce)
        {
            var personId = (await customers.GetPreFactorAsync(pfId, cancellationToken))?.CustomerId;
            if (!personId.HasValue || (cusId != Guid.Empty && personId.Value != cusId)) return Forbid();
        }
        var result = await requests.QueryAsync(new GetPreFactorUsersQuery(cusId, pfId), cancellationToken);
        return result == null ? NotFound("رکوردی یافت نشد!") : Ok(result);
    }
    [Authorize(Roles = "مدیر ارشد"), HttpGet]
    public async Task<IActionResult> ChangeUsersPreFactor(Guid pfId, Guid userId, Guid taminUserId, CancellationToken cancellationToken)
    {
        if (!await CanEditAllAsync(cancellationToken)) return Forbid();
        return await Mutation(requests.CommandAsync(new ChangePreFactorUsersCommand(pfId, userId, taminUserId), cancellationToken), cancellationToken);
    }
    [CustomerLegacyAuthorize(CustomerLegacyMode.NotCustomerOnly), HttpGet]
    public async Task<IActionResult> GetPhonebook(string search = "", CancellationToken cancellationToken = default)
    {
        var scope = await ScopeAsync("View", cancellationToken);
        if (scope == AccessScopes.None) return Forbid();
        return Ok(await requests.QueryAsync(new GetCustomerPhonebookQuery(search, OwnFilter(scope)), cancellationToken));
    }

    private async Task<IActionResult> Mutation(Task<CustomerMutationResult> operation, CancellationToken cancellationToken)
    {
        try
        {
            var result = await operation;
            if (result.Status == 403) return Forbid();
            return result.Status == 200 && result.Value == null ? Ok() : StatusCode(result.Status, result.Value);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return BadRequest(); }
    }
    private async Task<CustomerTableRequest> ReadTableAsync(CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        var sort = form["order[0][column]"].FirstOrDefault();
        return new(form["draw"].FirstOrDefault(), int.TryParse(form["start"], out var start) ? start : 0,
            int.TryParse(form["length"], out var length) ? length : 0, form[$"columns[{sort}][name]"].FirstOrDefault(),
            form["order[0][dir]"] == "asc", form["search[value]"].FirstOrDefault(), 0);
    }
}
