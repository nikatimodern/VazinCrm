using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
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
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
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
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.EndPoint.FrontEnd.Models;
using GetAllBySelectedVModel = CRM.EndPoint.FrontEnd.Models.Supply.GetAllBySelectedVModel;
using Microsoft.AspNetCore.Authorization;
using CRM.EndPoint.FrontEnd.Security;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Policy = "NotCustomerOnly")]
public partial class SupplyController(ICqrsDispatcher requests, DocumentAccessPolicy documentAccess, CRMContextDb db) : Controller
{
    private async Task<bool> ValidateLegacyPayment(Payment model, bool create, CancellationToken ct)
    {
        if (model == null) return false;
        if (create)
        {
            if (!await documentAccess.HasAsync("Payment", "Create", ct)) return false;
            if (documentAccess.IsEnforced("Payment") && model.Id != Guid.Empty) return false;
        }
        else if (!await documentAccess.CanAsync("Payment", "Edit", model.Id, ct)) return false;
        if (!await documentAccess.CanAsync("BankAccount", "View", model.FromAccountId, ct) ||
            !await documentAccess.CanAsync("BankAccount", "View", model.ToAccountId, ct)) return false;
        foreach (var link in model.PaymentSupplyFactors ?? [])
        {
            if (!await documentAccess.CanAsync("SupplyFactor", "View", link.SupplyFactorId, ct)) return false;
            link.PaymentId = model.Id;
        }
        if (!create)
        {
            if (documentAccess.IsEnforced("Payment") || documentAccess.IsEnforced("BankAccount"))
            {
                var previous = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id, ct);
                if (previous == null ||
                    !await documentAccess.CanAsync("BankAccount", "View", previous.FromAccountId, ct) ||
                    !await documentAccess.CanAsync("BankAccount", "View", previous.ToAccountId, ct)) return false;
                if (documentAccess.IsEnforced("Payment"))
                {
                    model.UserCreatorID = previous.UserCreatorID;
                    model.UserCreator = previous.UserCreator;
                    model.CreateDate = previous.CreateDate;
                    model.CreateDateTxt = previous.CreateDateTxt;
                }
            }
            var previousIds = await db.PaymentSupplyFactors.AsNoTracking().Where(x => x.PaymentId == model.Id).Select(x => x.SupplyFactorId).ToListAsync(ct);
            foreach (var id in previousIds)
                if (!await documentAccess.CanAsync("SupplyFactor", "View", id, ct)) return false;
        }
        if (documentAccess.IsEnforced("Payment"))
        {
            // Request-linked payments must use the request payment workflow.
            if (model.RequestAmountPayments?.Any() == true ||
                await db.RequestAmountPayments.AnyAsync(x => x.PaymentId == model.Id, ct)) return false;
            model.RequestAmountPayments = null;
            model.IsDeleted = false;
            foreach (var att in model.Attachments ?? [])
                if (att.Id != Guid.Empty && !await db.Attachments.AsNoTracking().AnyAsync(x => x.Id == att.Id && x.EntityId == model.Id, ct)) return false;
        }
        return true;
    }

    private Task<bool> CanSupplyFactor(string action, Guid id, CancellationToken ct) => documentAccess.CanAsync("SupplyFactor", action, id, ct);
    private async Task<bool> CanSupplyItem(string action, Guid id, CancellationToken ct)
    {
        if (!documentAccess.IsEnforced("SupplyFactor")) return true;
        var parent = await db.SupplyFactorItems.AsNoTracking().Where(x => x.Id == id).Select(x => (Guid?)x.SupplyFactorId).FirstOrDefaultAsync(ct);
        return parent.HasValue && await CanSupplyFactor(action, parent.Value, ct);
    }

    private async Task<bool> CanAllocation(SupplyFactorAllocationRequest model, CancellationToken ct)
    {
        if (model == null) return false;
        if (!await CanSupplyItem("ManageAllocations", model.SupplyFactorItemId, ct)) return false;
        if (model.FactorItemId.HasValue && documentAccess.IsEnforced("Factor"))
        {
            var parent = await db.FactorItems.AsNoTracking().Where(x => x.Id == model.FactorItemId).Select(x => (Guid?)x.factorId).FirstOrDefaultAsync(ct);
            if (!parent.HasValue || !await documentAccess.CanAsync("Factor", "Edit", parent.Value, ct)) return false;
        }
        if (model.Id != Guid.Empty && documentAccess.IsEnforced("SupplyFactor"))
        {
            var old = await db.SupplyFactorItemAllocations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.Id, ct);
            if (old == null || !old.SupplyFactorItemId.HasValue || !await CanSupplyItem("ManageAllocations", old.SupplyFactorItemId.Value, ct)) return false;
        }
        return true;
    }

    private async Task<bool> ValidateSupplyFactor(SupplyFactor model, bool create, CancellationToken ct)
    {
        if (!documentAccess.IsEnforced("SupplyFactor")) return true;
        if (model == null) return false;
        if (create)
        {
            if (model.Id != Guid.Empty || !await documentAccess.HasAsync("SupplyFactor", "Create", ct)) return false;
            if (model.Items?.Any(x => x.Id != Guid.Empty) == true) return false;
            model.IsDeleted = false;
        }
        else if (!await CanSupplyFactor("Edit", model.Id, ct)) return false;
        model.Deal = null;
        model.RequestAmountSupplyFactor = null;
        model.PaymentSupplyFactors = null;
        foreach (var link in model.SupplyFactorOrderHeads ?? new List<SupplyFactorOrderHead>())
            if (!await documentAccess.CanAsync("OrderHead", "View", link.OrderHeadId, ct)) return false;
        foreach (var attachment in model.Attachments ?? new List<Attachment>())
            if (attachment.Id != Guid.Empty && !await db.Attachments.AsNoTracking().AnyAsync(x => x.Id == attachment.Id && x.EntityId == model.Id, ct)) return false;
        return true;
    }

    private const string SalesAllocationRoles = "مدیر تامین,کارشناس تامین,مدیر ارشد,مدیر اجرایی,مدیر عامل,مدیر مالی,کارشناس مالی,مدیر فروش,کارشناس فروش";
    public class SupplyFactorAllocationRequest : CRM.Core.Domain.Supplies.Models.SupplyFactorAllocationRequest
    {
    }

    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> SupplyFactor(CancellationToken cancellationToken)
    {
        if (!await documentAccess.HasViewAsync("SupplyFactor", cancellationToken)) return Forbid();
        return View(await requests.QueryAsync(new GetWorkspacePageQuery("Supply/SupplyFactor"), cancellationToken));
    }
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> Payment(CancellationToken cancellationToken)
    {
        if (!await documentAccess.HasViewAsync("Payment", cancellationToken)) return Forbid();
        return View(await requests.QueryAsync(new GetWorkspacePageQuery("Supply/Payment"), cancellationToken));
    }
    [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
    public async Task<IActionResult> Profit(CancellationToken cancellationToken) => View(await requests.QueryAsync(new GetWorkspacePageQuery("Supply/Profit"), cancellationToken));
    [Authorize(Roles = "مدیر ارشد, مدیر اجرایی, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> Demand(CancellationToken cancellationToken) => View(await requests.QueryAsync(new GetWorkspacePageQuery("Supply/Demand"), cancellationToken));
    [HttpGet]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> GetSupplyFactorAllocations(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await CanSupplyFactor("View", id, cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new GetSupplyFactorAllocationsQuery(id), cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> SearchAllocationFactorItems(string search, Guid supplyFactorItemId, Guid? allocationId = null, CancellationToken cancellationToken = default)
    {
        if (!await CanSupplyItem("ManageAllocations", supplyFactorItemId, cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new SearchAllocationFactorItemsQuery(search, supplyFactorItemId, allocationId), cancellationToken));
    }

    [HttpGet]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> GetSalesFactorAllocations(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await documentAccess.CanAsync("Factor", "View", id, cancellationToken)) return Forbid();
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
        if (!paymentId.HasValue || !await documentAccess.CanAsync("Payment", "View", paymentId.Value, cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new GetPaymentQuery(paymentId), cancellationToken));
    }

    public async Task<IActionResult> GetAllSupplyPaymentDt(CancellationToken cancellationToken = default)
    {
        if (!await documentAccess.HasViewAsync("Payment", cancellationToken)) return Forbid();
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
        if (!await documentAccess.HasAsync("SupplyFactor", "View", cancellationToken) || !await documentAccess.HasAsync("Supply", "View", cancellationToken) || !await documentAccess.HasAsync("Accountancy", "View", cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new GetSupplierFinancialSummaryQuery(supplierId), cancellationToken));
    }

    public async Task<IActionResult> GetAllSupplyFactorsPartial(string search, Guid? dealId = null, CancellationToken cancellationToken = default)
    {
        if (!await documentAccess.HasViewAsync("SupplyFactor", cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new GetSupplyFactorsQuery(search, dealId), cancellationToken), nameof(GetAllSupplyFactorsPartial));
    }

    [HttpPost]
    public async Task<IActionResult> GetAllSupplyFactorsTablePartial([FromBody] GetAllBySelectedVModel model, CancellationToken cancellationToken = default)
    {
        if (!await documentAccess.HasViewAsync("SupplyFactor", cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new SearchSupplyFactorsQuery(model == null ? null : new SupplySelection(model.Search, model.SelectedIds)), cancellationToken), nameof(GetAllSupplyFactorsTablePartial));
    }

    public async Task<IActionResult> GetSupplyFactor(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await CanSupplyFactor("View", id, cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new GetSupplyFactorQuery(id), cancellationToken));
    }

    public async Task<IActionResult> GetSupplyBaseData(CancellationToken cancellationToken = default)
    {
        if (!await documentAccess.HasViewAsync("SupplyFactor", cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new GetSupplyBaseDataQuery(), cancellationToken));
    }

    public async Task<IActionResult> GetGroupedOrdersBySupplier(CancellationToken cancellationToken = default)
    {
        return Map(await requests.QueryAsync(new GetSupplierOrdersQuery(), cancellationToken), nameof(GetGroupedOrdersBySupplier));
    }

    [HttpGet]
    public async Task<IActionResult> GetSupplies(Guid pfId, CancellationToken cancellationToken = default)
    {
        if (!await documentAccess.HasViewAsync("Supply", cancellationToken)) return Forbid();
        if (!await documentAccess.CanAsync("PreFactor", "View", pfId, cancellationToken)) return Forbid();
        return Map(await requests.QueryAsync(new GetSuppliesQuery(pfId), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> GetSuppliesProfitDt(CancellationToken cancellationToken = default)
    {
        if (!await documentAccess.HasAsync("SupplyFactor", "View", cancellationToken) || !await documentAccess.HasAsync("Supply", "View", cancellationToken) || !await documentAccess.HasAsync("Accountancy", "View", cancellationToken)) return Forbid();
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
        if (!await documentAccess.HasAsync("SupplyFactor", "View", cancellationToken) || !await documentAccess.HasAsync("Supply", "View", cancellationToken) || !await documentAccess.HasAsync("Accountancy", "View", cancellationToken)) return Forbid();
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
        if (!await documentAccess.HasAsync("SupplyFactor", "View", cancellationToken) || !await documentAccess.HasAsync("Supply", "View", cancellationToken) || !await documentAccess.HasAsync("Accountancy", "View", cancellationToken)) return Forbid();
        try
        {
            var request = await ReadTableRequestAsync(false, true, cancellationToken);
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
        if (!await documentAccess.HasViewAsync("Supply", cancellationToken)) return Forbid();
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
        if (!await CanAllocation(model, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        return Map(await requests.CommandAsync(new SaveSupplyAllocationCommand(model, null), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> UpdateSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!await CanAllocation(model, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        return Map(await requests.CommandAsync(new SaveSupplyAllocationCommand(model, model?.Id ?? Guid.Empty), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> AddSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!await CanAllocation(model, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        return Map(await requests.CommandAsync(new SaveSalesAllocationCommand(model, null), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> UpdateSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!await CanAllocation(model, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest("مقدار تخصیص باید مثبت و حداکثر دارای دو رقم اعشار باشد.");
        return Map(await requests.CommandAsync(new SaveSalesAllocationCommand(model, model?.Id ?? Guid.Empty), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteSupplyFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!await CanAllocation(model, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest();
        return Map(await requests.CommandAsync(new DeleteSupplyAllocationCommand(model), cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SalesAllocationRoles)]
    public async Task<IActionResult> DeleteSalesFactorAllocation([FromBody] SupplyFactorAllocationRequest model, CancellationToken cancellationToken = default)
    {
        if (!await CanAllocation(model, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest();
        return Map(await requests.CommandAsync(new DeleteSalesAllocationCommand(model), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddSupplyFactor([FromBody] SupplyFactor model, CancellationToken cancellationToken = default)
    {
        if (!await ValidateSupplyFactor(model, true, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest();
        return Map(await requests.CommandAsync(new AddSupplyFactorCommand(model), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSupplyFactor([FromBody] SupplyFactor model, CancellationToken cancellationToken = default)
    {
        if (!await ValidateSupplyFactor(model, false, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest();
        return Map(await requests.CommandAsync(new UpdateSupplyFactorCommand(model), cancellationToken));
    }

    public async Task<IActionResult> DeleteSupplyFactor(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await CanSupplyFactor("Delete", id, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest();
        return Map(await requests.CommandAsync(new DeleteSupplyFactorCommand(id), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddPayment([FromBody] Payment model, CancellationToken cancellationToken = default)
    {
        if (!await ValidateLegacyPayment(model, true, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest();
        return Map(await requests.CommandAsync(new AddPaymentCommand(model), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePayment([FromBody] Payment model, CancellationToken cancellationToken = default)
    {
        if (!await ValidateLegacyPayment(model, false, cancellationToken)) return Forbid();
        if (!ModelState.IsValid)
            return BadRequest();
        return Map(await requests.CommandAsync(new UpdatePaymentCommand(model), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> AddSupply([FromBody] List<Supply> model, CancellationToken cancellationToken = default)
    {
        if (documentAccess.IsEnforced("Supply"))
        {
            if (model == null || model.Count == 0) return BadRequest();
            foreach (var item in model)
            {
                if (item.Id == Guid.Empty)
                {
                    if (!await documentAccess.HasAsync("Supply", "Create", cancellationToken) || item.IsDeleted) return Forbid();
                }
                else
                {
                    if (!await documentAccess.CanAsync("Supply", item.IsDeleted ? "Delete" : "Edit", item.Id, cancellationToken)) return Forbid();
                    var old = await db.Supplies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
                    if (old == null || old.PfId != item.PfId) return Forbid();
                    item.UserCreator = old.UserCreator;
                    item.UserCreatorID = old.UserCreatorID;
                }
                if (!await documentAccess.CanAsync("PreFactor", "EditSupply", item.PfId, cancellationToken)) return Forbid();
                if (item.PfItemId.HasValue && !await db.PreFactorItems.AsNoTracking().AnyAsync(x => x.Id == item.PfItemId && x.PrefactorId == item.PfId, cancellationToken)) return Forbid();
            }
        }
        if (!ModelState.IsValid)
            return BadRequest();
        if (documentAccess.IsEnforced("PreFactor"))
        {
            if (model == null || model.Count == 0) return BadRequest();
            foreach (var pfId in model.Select(x => x.PfId).Distinct())
                if (!await documentAccess.CanAsync("PreFactor", "EditSupply", pfId, cancellationToken)) return Forbid();
            foreach (var item in model.Where(x => x.Id != Guid.Empty))
            {
                var old = await db.Supplies.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
                if (old == null || old.PfId != item.PfId) return Forbid();
            }
            foreach (var item in model.Where(x => x.PfItemId.HasValue))
                if (!await db.PreFactorItems.AsNoTracking().AnyAsync(x => x.Id == item.PfItemId && x.PrefactorId == item.PfId, cancellationToken)) return Forbid();
        }
        return Map(await requests.CommandAsync(new AddSupplyCommand(model), cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> ProcessSupplyFactorWithAi([FromBody] Attachment attachment, CancellationToken ct)
    {
        if (!await documentAccess.HasAsync("SupplyFactor", "Create", ct)) return Forbid();
        return Map(await requests.CommandAsync(new ProcessSupplyInvoiceCommand(attachment), ct));
    }
    private IActionResult Map(SupplyResult result, string viewName = null)
    {
        if (result.Kind == SupplyResultKind.Unauthorized)
            return Unauthorized();
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
        return new SupplyTableRequest(form["draw"].FirstOrDefault(), start, length, tolerantPaging && string.IsNullOrWhiteSpace(column) ? null : form[$"columns[{column}][name]"].FirstOrDefault(), form["order[0][dir]"].FirstOrDefault(), form["search[value]"].FirstOrDefault()?.Trim(), model?._type, dateFilter, null, null, userFilter, model?.filt?.selFilter?.ToString(), model?.filt?.fromDate, model?.filt?.toDate, dateFilter, model?.filt?.CusFilterChd == true ? model.filt.customerFilter?.ToString() : null);
    }
}
