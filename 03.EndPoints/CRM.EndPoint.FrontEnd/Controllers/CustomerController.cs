using System;
using System.Collections.Generic;
using System.Linq;
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
public sealed class CustomerController(ICqrsDispatcher requests) : Controller
{
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await requests.QueryAsync(new GetCustomersPageQuery(), cancellationToken);
        ViewBag.SubTitle = " فرم ثبت مشتری"; ViewBag.Head = "Customer";
        return View(user);
    }
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public Task<IActionResult> RemoveUserCustomer([FromBody] UserCustomer model, CancellationToken cancellationToken) =>
        Mutation(requests.CommandAsync(new RemoveCustomerExpertCommand(model.CustomerId, model.UserId), cancellationToken), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public Task<IActionResult> AddSelsToCustomers([FromBody] SelsCustomerVModel model, CancellationToken cancellationToken) =>
        Mutation(requests.CommandAsync(new AssignCustomerExpertsCommand(model.customerId, model.selExperts), cancellationToken), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public async Task<IActionResult> GetCustomerOp(string search, CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new SearchCustomerOptionsQuery(search), cancellationToken));
    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public async Task<IActionResult> GetSuppliers(CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetCustomerSuppliersQuery(), cancellationToken));
    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public async Task<IActionResult> GetCustomerRel(Guid Id, CancellationToken cancellationToken)
    {
        var data = await requests.QueryAsync(new GetRelatedCustomersQuery(Id), cancellationToken);
        return data.Count == 0 ? Ok(null) : PartialView("DuplicatePartialDetail", data);
    }
    [HttpGet]
    public async Task<IActionResult> CustomerProfile(Guid id, CancellationToken cancellationToken)
    {
        var result = await requests.QueryAsync(new GetCustomerProfileQuery(id), cancellationToken);
        return result.Status switch
        {
            400 => BadRequest(),
            403 => Forbid(),
            404 => NotFound(),
            _ => View(new CustomerPanelVModel { CustomerId = result.Id, CustomerName = result.Name, SelExpertRel = result.SalesExperts })
        };
    }
    [Authorize(Roles = "مدیر ارشد ,مدیر فروش"), HttpGet]
    public async Task<IActionResult> CustomerDetail(Guid id, CancellationToken cancellationToken)
    {
        var data = await requests.QueryAsync(new GetCustomerDetailQuery(id), cancellationToken);
        if (id == Guid.Empty) return BadRequest();
        ViewBag.CustomerId = id;
        return View(data);
    }
    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public Task<IActionResult> AbortCustomer(Guid id, CancellationToken cancellationToken) =>
        Mutation(requests.CommandAsync(new DeleteCustomerCommand(id), cancellationToken), cancellationToken);
    [Authorize(Roles = "مدیر ارشد ,مدیر فروش"), HttpGet]
    public Task<IActionResult> AcceptCustomer(Guid id, CancellationToken cancellationToken) =>
        Mutation(requests.CommandAsync(new AcceptCustomerCommand(id), cancellationToken), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public Task<IActionResult> AddToMyCustomer(Guid id, CancellationToken cancellationToken) =>
        Mutation(requests.CommandAsync(new AddCustomerToCurrentUserCommand(id), cancellationToken), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public async Task<IActionResult> CheckCus(Customer model, CancellationToken cancellationToken)
    {
        var exists = await requests.QueryAsync(new CheckCustomerRegistrationQuery(model), cancellationToken);
        return exists.HasValue ? Ok(new { Status = exists.Value ? 500 : 200, error = exists.Value ? "مشتری قبلا در سیستم ثبت شده است" : "" }) : Ok();
    }
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public async Task<IActionResult> CheckDuplicate([FromBody] Customer model, CancellationToken cancellationToken)
    {
        if (model == null) return BadRequest();
        var data = await requests.QueryAsync(new FindDuplicateCustomersQuery(model), cancellationToken);
        return data.Count == 0 ? Ok(null) : PartialView("DuplicatePartial", data);
    }
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public Task<IActionResult> UpCustomer([FromBody] Customer model, CancellationToken cancellationToken) =>
        Mutation(requests.CommandAsync(new UpdateCustomerCommand(model), cancellationToken), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public Task<IActionResult> AddCustomer([FromBody] Customer model, CancellationToken cancellationToken) =>
        model == null ? Task.FromResult<IActionResult>(BadRequest()) : Mutation(requests.CommandAsync(new CreateCustomerCommand(model), cancellationToken), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public async Task<IActionResult> GetAllCustomerJuridical(CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetJuridicalCustomerTableQuery(await ReadTableAsync(cancellationToken)), cancellationToken));
    [Authorize(Policy = "NotCustomerOnly"), HttpPost]
    public async Task<IActionResult> GetAllCustomerNonJuridical(CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetIndividualCustomerTableQuery(await ReadTableAsync(cancellationToken)), cancellationToken));
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> GetAllCustomerPartial(int skip = 0, int take = 10, int roleType = -1, int juridical = -1, CancellationToken cancellationToken = default)
    {
        var result = await requests.QueryAsync(new GetCustomerCardsQuery(skip, take, roleType, juridical), cancellationToken);
        ViewBag.AccessAccepte = result.CanAccept;
        return PartialView("GetAllCustomerPartial", result.Customers);
    }
    public async Task<string> GetUsersRel(IList<UserCustomer> userCustomers, CancellationToken cancellationToken) =>
        await requests.QueryAsync(new GetCustomerUserNamesQuery(userCustomers.Select(u => u.UserId).ToList()), cancellationToken);
    public async Task NormalizeAllCustomers(CancellationToken cancellationToken) =>
        await requests.CommandAsync(new NormalizeCustomersCommand(), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> SearchAllCustomerPartial(string val, int roleType = -1, int juridical = -1, CancellationToken cancellationToken = default)
    {
        var result = await requests.QueryAsync(new SearchCustomerCardsQuery(val, roleType, juridical), cancellationToken);
        ViewBag.AccessAccepte = result.CanAccept;
        return PartialView("GetAllCustomerPartial", result.Customers);
    }
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> GetAllCustomers(CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetCustomerSummariesQuery(), cancellationToken));
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> GetCustomer(Guid id, CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetCustomerQuery(id), cancellationToken));
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> GetCustomerByMobile(string mobile, CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetCustomerByMobileQuery(mobile), cancellationToken));
    [Authorize(Roles = "مدیر ارشد ,مدیر فروش"), HttpGet]
    public async Task<IActionResult> GetCustomerSelRelation(Guid id, CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetCustomerExpertsQuery(id), cancellationToken));
    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public async Task<IActionResult> GetSepidarFactors(int pfSerialNumber, int passType = 1, CancellationToken cancellationToken = default)
    {
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
        try
        {
            var result = await requests.QueryAsync(new GetCustomerPreFactorsQuery(customerId, dealId, showDeleted), cancellationToken);
            return Ok(new { count = result.Count, html = result.Html });
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested) { return BadRequest(); }
    }
    [Authorize(Roles = "مدیر ارشد"), HttpGet]
    public async Task<IActionResult> GetUsersForPreFactor(Guid cusId, Guid pfId, CancellationToken cancellationToken)
    {
        var result = await requests.QueryAsync(new GetPreFactorUsersQuery(cusId, pfId), cancellationToken);
        return result == null ? NotFound("رکوردی یافت نشد!") : Ok(result);
    }
    [Authorize(Roles = "مدیر ارشد"), HttpGet]
    public Task<IActionResult> ChangeUsersPreFactor(Guid pfId, Guid userId, Guid taminUserId, CancellationToken cancellationToken) =>
        Mutation(requests.CommandAsync(new ChangePreFactorUsersCommand(pfId, userId, taminUserId), cancellationToken), cancellationToken);
    [Authorize(Policy = "NotCustomerOnly"), HttpGet]
    public async Task<IActionResult> GetPhonebook(string search = "", CancellationToken cancellationToken = default) =>
        Ok(await requests.QueryAsync(new GetCustomerPhonebookQuery(search), cancellationToken));

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
