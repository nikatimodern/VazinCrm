using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Supplies.Commands.AddPayment;
using CRM.Core.ApplicationServices.Supplies.Commands.AddSupply;
using CRM.Core.ApplicationServices.Supplies.Commands.AddSupplyFactor;
using CRM.Core.ApplicationServices.Supplies.Commands.DeleteSalesAllocation;
using CRM.Core.ApplicationServices.Supplies.Commands.DeleteSupplyAllocation;
using CRM.Core.ApplicationServices.Supplies.Commands.DeleteSupplyFactor;
using CRM.Core.ApplicationServices.Supplies.Commands.ProcessSupplyInvoice;
using CRM.Core.ApplicationServices.Supplies.Commands.SaveSalesAllocation;
using CRM.Core.ApplicationServices.Supplies.Commands.SaveSupplyAllocation;
using CRM.Core.ApplicationServices.Supplies.Commands.UpdatePayment;
using CRM.Core.ApplicationServices.Supplies.Commands.UpdateSupplyFactor;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.ApplicationServices.Supplies.Queries.GetDealSupplies;
using CRM.Core.ApplicationServices.Supplies.Queries.GetOrderHeadsBySupplier;
using CRM.Core.ApplicationServices.Supplies.Queries.GetPayment;
using CRM.Core.ApplicationServices.Supplies.Queries.GetReceivables;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSalesFactorAllocations;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplierFinancialSummary;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplierOrders;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplies;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyBaseData;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyDemands;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFactor;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFactorAllocations;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyFactors;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyPayments;
using CRM.Core.ApplicationServices.Supplies.Queries.GetSupplyProfit;
using CRM.Core.ApplicationServices.Supplies.Queries.SearchAllocationFactorItems;
using CRM.Core.ApplicationServices.Supplies.Queries.SearchAllocationSupplyItems;
using CRM.Core.ApplicationServices.Supplies.Queries.SearchSupplyFactors;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.EndPoint.FrontEnd.Models;
using GetAllBySelectedVModel = CRM.EndPoint.FrontEnd.Models.Supply.GetAllBySelectedVModel;
using CRM.EndPoint.FrontEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public class SupplyController(ISupplyRequestDispatcher requests, UserManager<AppUser> users, DateTimeServices dates) : Controller
{
    private const string SalesAllocationRoles = "مدیر تامین,کارشناس تامین,مدیر ارشد,مدیر اجرایی,مدیر عامل,مدیر مالی,کارشناس مالی,مدیر فروش,کارشناس فروش";
    public class SupplyFactorAllocationRequest : CRM.Core.Domain.Supplies.Models.SupplyFactorAllocationRequest
    {
    }

    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public IActionResult SupplyFactor() => View();
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public IActionResult Payment() => View();
    [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
    public IActionResult Profit() => View();
    [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
    public IActionResult Demand() => View();
    [HttpGet]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> GetSupplyFactorAllocations(Guid id, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSupplyFactorAllocationsQuery(id), cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> SearchAllocationFactorItems(string search, Guid supplyFactorItemId, Guid? allocationId = null, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new SearchAllocationFactorItemsQuery(search, supplyFactorItemId, allocationId), cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> GetSalesFactorAllocations(Guid id, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSalesFactorAllocationsQuery(id), cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> SearchAllocationSupplyItems(string search, Guid factorItemId, Guid? allocationId = null, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new SearchAllocationSupplyItemsQuery(search, factorItemId, allocationId), cancellationToken));
    }

    public async Task<IActionResult> GetPayment(Guid? paymentId, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetPaymentQuery(paymentId), cancellationToken));
    }

    public async Task<IActionResult> GetAllSupplyPaymentDt(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await ReadTableRequestAsync(false, false, cancellationToken);
            return Map(await requests.QueryAsync(new GetSupplyPaymentsQuery(request), cancellationToken));
        }
        catch (Exception)when (!cancellationToken.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetSupplierFinancialSummary(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSupplierFinancialSummaryQuery(supplierId), cancellationToken));
    }

    public async Task<IActionResult> GetAllSupplyFactorsPartial(string search, Guid? dealId = null, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSupplyFactorsQuery(search, dealId), cancellationToken), nameof(GetAllSupplyFactorsPartial));
    }

    [HttpPost]
    public async Task<IActionResult> GetAllSupplyFactorsTablePartial([FromBody] GetAllBySelectedVModel model, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new SearchSupplyFactorsQuery(model == null ? null : new SupplySelection(model.Search, model.SelectedIds)), cancellationToken), nameof(GetAllSupplyFactorsTablePartial));
    }

    public async Task<IActionResult> GetSupplyFactor(Guid id, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSupplyFactorQuery(id), cancellationToken));
    }

    public async Task<IActionResult> GetSupplyBaseData(CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSupplyBaseDataQuery(), cancellationToken));
    }

    public async Task<IActionResult> GetGroupedOrdersBySupplier(CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSupplierOrdersQuery(), cancellationToken), nameof(GetGroupedOrdersBySupplier));
    }

    [HttpGet]
    public async Task<IActionResult> GetSupplies(Guid pfId, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSuppliesQuery(pfId), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> GetSuppliesProfitDt(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await ReadTableRequestAsync(true, true, cancellationToken);
            return Map(await requests.QueryAsync(new GetSupplyProfitQuery(request), cancellationToken));
        }
        catch (Exception)when (!cancellationToken.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetSuppliesDemandsDt(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await ReadTableRequestAsync(false, true, cancellationToken);
            return Map(await requests.QueryAsync(new GetSupplyDemandsQuery(request), cancellationToken));
        }
        catch (Exception)when (!cancellationToken.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetRpMotalebat(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = await ReadTableRequestAsync(false, false, cancellationToken);
            return Map(await requests.QueryAsync(new GetReceivablesQuery(request), cancellationToken));
        }
        catch (Exception)when (!cancellationToken.IsCancellationRequested)
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliesDetails(Guid dealId, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetDealSuppliesQuery(dealId), cancellationToken), nameof(GetSuppliesDetails));
    }

    public async Task<IActionResult> GetOrderHeadsBySupplier(Guid supplierId, CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetOrderHeadsBySupplierQuery(supplierId), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> AddSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new SaveSupplyAllocationCommand(model, null, user), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> UpdateSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new SaveSupplyAllocationCommand(model, model?.Id ?? Guid.Empty, user), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> AddSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new SaveSalesAllocationCommand(model, null, user), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> UpdateSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new SaveSalesAllocationCommand(model, model?.Id ?? Guid.Empty, user), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new DeleteSupplyAllocationCommand(model, user), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> DeleteSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new DeleteSalesAllocationCommand(model, user), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddSupplyFactor([FromBody] SupplyFactor model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new AddSupplyFactorCommand(model, user), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSupplyFactor([FromBody] SupplyFactor model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new UpdateSupplyFactorCommand(model, user), cancellationToken));
    }

    public async Task<IActionResult> DeleteSupplyFactor(Guid id, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new DeleteSupplyFactorCommand(id, user), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddPayment([FromBody] Payment model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new AddPaymentCommand(model, user), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePayment([FromBody] Payment model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new UpdatePaymentCommand(model, user), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddSupply([FromBody] List<Supply> model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest();
        var user = await users.GetUserAsync(User);
        if (user == null)
            return Unauthorized();
        return Map(await requests.CommandAsync(new AddSupplyCommand(model, user), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> ProccessSupplyFactorWithAi([FromBody] Attachment attachment, CancellationToken ct) => Map(await requests.CommandAsync(new ProcessSupplyInvoiceCommand(attachment), ct));
    private IActionResult Map(SupplyResult result, string viewName = null)
    {
        if (result.Kind == SupplyResultKind.Invalid)
            return result.Error == null ? BadRequest() : BadRequest(result.Error);
        if (result.Kind == SupplyResultKind.Missing)
            return result.Error == null ? NotFound() : NotFound(result.Error);
        if (viewName != null)
        {
            object data = result.Data;
            if (viewName == nameof(GetAllSupplyFactorsPartial))
                data = ((System.Collections.Generic.List<SupplyFactorVModel>)result.Data).Select(row => new Models.Supply.SupplyFactorVModel { SupplyFactor = row.SupplyFactor, OrderSerials = row.OrderSerials, IsAllocationComplete = row.IsAllocationComplete }).ToList();
            if (viewName == nameof(GetGroupedOrdersBySupplier))
                data = ((System.Collections.Generic.List<GroupedOrdersBySupplierVModal>)result.Data).Select(row => new Models.Supply.GroupedOrdersBySupplierVModal { SupplierId = row.SupplierId, SupplierTxt = row.SupplierTxt, Products = row.Products.Select(p => new Models.Supply.ProductGroupVModel { Orders = p.Orders, OrderHead = p.OrderHead, SumWeight = p.SumWeight, ProductCode = p.ProductCode, ProductSmallName = p.ProductSmallName, UnitTxt = p.UnitTxt }).ToList() }).ToList();
            return PartialView(viewName, data);
        }

        return result.HasData ? Ok(result.Data) : Ok();
    }

    private async Task<SupplyTableRequest> ReadTableRequestAsync(bool tolerantPaging, bool includeFilters, CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        var rawStart = form["start"].FirstOrDefault();
        var rawLength = form["length"].FirstOrDefault();
        var start = tolerantPaging ? int.TryParse(rawStart, out var parsedStart) ? Math.Max(0, parsedStart) : 0 : Convert.ToInt32(rawStart ?? "0");
        var length = tolerantPaging ? int.TryParse(rawLength, out var parsedLength) ? parsedLength : 10 : Convert.ToInt32(rawLength ?? "10");
        var model = JsonConvert.DeserializeObject<FilterVModel>(form["model"].FirstOrDefault());
        var column = form["order[0][column]"].FirstOrDefault();
        var dateFilter = includeFilters && model?.filt?.DateFilterChk == true;
        var userFilter = includeFilters && model?.filt?.SelFilterChd == true;
        var from = dateFilter ? dates.GetDateMiladi(model.filt.fromDate) ?? dates.GetDateMiladi("1000/01/01") : null;
        var to = dateFilter ? (dates.GetDateMiladi(model.filt.toDate) ?? DateTime.Today).Date.AddDays(1).AddTicks(-1) : (DateTime? )null;
        return new SupplyTableRequest(form["draw"].FirstOrDefault(), start, length, tolerantPaging && string.IsNullOrWhiteSpace(column) ? null : form[$"columns[{column}][name]"].FirstOrDefault(), form["order[0][dir]"].FirstOrDefault(), form["search[value]"].FirstOrDefault()?.Trim(), model?._type, dateFilter, from, to, userFilter, model?.filt?.selFilter?.ToString());
    }
}
