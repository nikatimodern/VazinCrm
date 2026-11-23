using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.Querying;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Accountancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.EndPoint.FrontEnd.Security;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyDetails;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyTransfer;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetInvoiceTotals;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyCartable;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyPayments;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetCustomerWallet;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountanciesTable;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountOptions;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountsTable;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetBankAccountDetails;
using CRM.Core.ApplicationServices.Accountancies.Commands.DelAccountancy;
using CRM.Core.ApplicationServices.Accountancies.Commands.AddInvoiceWalletToPf;
using CRM.Core.ApplicationServices.Accountancies.Commands.UpBankAccount;
using CRM.Core.ApplicationServices.Accountancies.Commands.AddBankAccount;
using CRM.Core.ApplicationServices.Accountancies.Commands.ChangeActiveBankAccount;
using CRM.Core.ApplicationServices.Accountancies.Commands.DelBankAccountItem;
using CRM.Core.ApplicationServices.Accountancies.Commands.AcceptAccountancy;
using CRM.Core.ApplicationServices.Accountancies.Commands.ChangeStatusAllAcc;
using CRM.Core.ApplicationServices.Accountancies.Commands.ChangeStatusAcc;
using CRM.Core.ApplicationServices.Accountancies.Commands.AddAccountancyTransfer;
using CRM.Core.ApplicationServices.Accountancies.Commands.RemoveAccountancyTransfer;
using CRM.Core.ApplicationServices.Accountancies.Commands.RemoveVosolInvoice;
using CRM.Core.ApplicationServices.QualityControls.Services;

namespace CRM.EndPoint.FrontEnd.Controllers;
public class AccountancyController(ICqrsDispatcher dispatcher, DocumentAccessPolicy access, CRMContextDb db) : Controller
{
    private Task<bool> CanAcc(string action, Guid id, CancellationToken ct) => access.CanAsync("Accountancy", action, id, ct);
    private async Task<bool> CanAllRows(IQueryable<Accountancy> rows, string action, CancellationToken ct)
    {
        if (!access.IsEnforced("Accountancy")) return true;
        if (!await access.HasAnyAsync("Accountancy", action, ct)) return false;
        var permitted = await access.ApplyAsync(rows, "View", ct);
        if (action != "View") permitted = await access.ApplyAsync(permitted, action, ct);
        return !await rows.AnyAsync(x => !permitted.Any(p => p.Id == x.Id), ct);
    }

    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Accountancy", ct)) return Forbid();
        return View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("Accountancy"), ct));
    }





    [HttpGet]
    public async Task<IActionResult> getAccountancy(Guid id, bool parent = false, CancellationToken ct = default)
    {
        if (access.IsEnforced("Accountancy"))
        {
            if (parent)
            {
                if (!await CanAcc("View", id, ct) || !await CanAllRows(db.Accountancies.Where(x => x.ParentId == id), "View", ct)) return Forbid();
            }
            else if (!await CanAcc("View", id, ct)) return Forbid();
        }
        var data = await dispatcher.QueryAsync(new GetAccountancyDetailsQuery(id, parent), ct);
        return data.AccData == null ? NotFound("رکوردی یافت نشد!") : Ok(new AccViewModel { AccData = data.AccData, ListAttach = data.ListAttach });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAmountPartial(int type, Guid? id, CancellationToken ct)
    {
        if (access.IsEnforced("Accountancy"))
        {
            if (!id.HasValue || type is not (1 or 2 or 3 or 4 or 5 or 11 or 12)) return BadRequest();
            var rows = db.Accountancies.AsNoTracking().Where(x =>
                (type == 1 || type == 11 || type == 12) ? x.pfid == id :
                type == 2 ? x.fid == id : type == 3 ? x.DealId == id : x.CustomerId == id);
            if (!await CanAllRows(rows, "View", ct)) return Forbid();
            if ((type is 1 or 11 or 12) && !await access.CanAsync("PreFactor", "View", id.Value, ct)) return Forbid();
            if (type == 2 && !await access.CanAsync("Factor", "View", id.Value, ct)) return Forbid();
            // Unscoped aggregate totals must not expose hidden documents.
            if ((type is 3 or 4 or 5) && !await access.HasAsync("PreFactor", "View", ct)) return Forbid();
        }
        var data = await dispatcher.QueryAsync(new GetInvoiceTotalsQuery(type, id), ct);
        if (data == null)
            return NotFound("پیش فاکتور یافت نشد");
        return PartialView(new PaymentListViewModel { Discount = data.Discount, PaymentAmount = data.PaymentAmount, ReciveAmount = data.ReciveAmount, BackwardAmount = data.BackwardAmount, SumTotalAmount = data.SumTotalAmount, SumTotalAmountTax = data.SumTotalAmountTax, TotalTax = data.TotalTax });
    }

    public async Task<IActionResult> GetAllPaymentCartablePartial(string serVal, CartableEnum cartable, int type = 1, PayTypeEnum? payType = null, CancellationToken ct = default)
    {
        if (!await access.HasViewAsync("Accountancy", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetAccountancyCartableQuery(serVal, cartable, type, payType), ct));
    }
    [HttpGet]
    public async Task<IActionResult> GetAllPaymentPartial(int type, Guid id, int passType, bool depositsOnly = false, bool realizedDepositsOnly = false, bool returnsOnly = false, CancellationToken ct = default)
    {
        if (!await access.HasViewAsync("Accountancy", ct)) return Forbid();
        // The specialized realized-deposit view is used by quality control only.
        if (realizedDepositsOnly)
        {
            if (!QualityControlAccessPolicy.Roles.Split(',').Any(User.IsInRole))
                return Forbid();
            if (!depositsOnly || returnsOnly || type is not (3 or 5))
                return BadRequest(new { message = "فیلتر واریزی معتبر نیست." });
        }
        return Ok(await dispatcher.QueryAsync(
            new GetAccountancyPaymentsQuery(type, id, passType, depositsOnly, realizedDepositsOnly, returnsOnly), ct));
    }
    [HttpPost]
    public async Task<IActionResult> AddAccountancyTransfer([FromBody] AccountancyTransfer model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await CanAcc("Transfer", model.AccountancyId, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new AddAccountancyTransferCommand(model), ct));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveTransferInvoice(Guid id, CancellationToken ct)
    {
        if (!await CanAcc("Transfer", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new RemoveAccountancyTransferCommand(id), ct));
    }

    [HttpGet]
    public async Task<IActionResult> RemoveVosolInvoice(Guid id, CancellationToken ct)
    {
        if (!await CanAcc("Settle", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new RemoveVosolInvoiceCommand(id), ct));
    }

    [HttpGet]
    public async Task<IActionResult> GetAccountancyTransfer(Guid id, CancellationToken ct)
    {
        if (!await CanAcc("View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetAccountancyTransferQuery(id), ct));
    }
    [HttpGet]
    public async Task<IActionResult> CheckWallet(Guid cusId, CancellationToken ct)
    {
        if (!await access.HasViewAsync("Accountancy", ct)) return Forbid();
        return Ok((await dispatcher.QueryAsync(new GetCustomerWalletQuery(cusId), ct)).Count > 0);
    }
    [HttpGet]
    public async Task<IActionResult> GetWalletPartialIfAny(Guid cusId, CancellationToken ct)
    {
        if (!await access.HasViewAsync("Accountancy", ct)) return Forbid();
        return PartialView("GetAllWalletPartial", await dispatcher.QueryAsync(new GetCustomerWalletQuery(cusId), ct));
    }
    public async Task<IActionResult> GetAllAccount(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Accountancy", ct)) return Forbid();
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<AccFilterVModel>(form["modelF"].FirstOrDefault() ?? "null") ?? new();
        return Ok(await dispatcher.QueryAsync(new GetAccountanciesTableQuery(await ReadTableAsync(ct), model.ToApplicationFilter(), model.AccType, model.PayType), ct));
    }

    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> GetAllBankAccountJson(BankAccountTypeEnum? type = null, CancellationToken ct = default)
    {
        if (!await access.HasViewAsync("BankAccount", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetBankAccountOptionsQuery(type), ct));
    }
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> GetAllBankAccount(CancellationToken ct)
    {
        if (!await access.HasViewAsync("BankAccount", ct)) return Forbid();
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<FilterVModel>(form["model"].FirstOrDefault() ?? "null");
        return Ok(await dispatcher.QueryAsync(new GetBankAccountsTableQuery(await ReadTableAsync(ct), model?._type), ct));
    }

    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> GetBankAccount(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("BankAccount", "View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetBankAccountDetailsQuery(id), ct));
    }
    public async Task<IActionResult> DelAccountancy(Guid id, CancellationToken ct)
    {
        if (!await CanAcc("Delete", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new DelAccountancyCommand(id), ct));
    }
    [HttpGet]
    public async Task<IActionResult> AddInvoiceWalletToPf(Guid id, Guid pfId, CancellationToken ct)
    {
        if (!await CanAcc("UseWallet", id, ct)) return Forbid();
        if (!await access.CanAsync("PreFactor", "View", pfId, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new AddInvoiceWalletToPfCommand(id, pfId), ct));
    }

    // All mutations of a bank account require the same financial-management roles.
    [HttpPost, Authorize(Roles = "مدیر مالی, کارشناس مالی, مدیر ارشد, مدیر عامل")]
    public async Task<IActionResult> UpBankAccount([FromBody] BankAccount model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.CanAsync("BankAccount", "Edit", model.Id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new UpBankAccountCommand(model), ct));
    }
    [HttpPost, Authorize(Roles = "مدیر مالی, کارشناس مالی, مدیر ارشد, مدیر عامل")]
    public async Task<IActionResult> AddBankAccount([FromBody] BankAccount model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.HasAsync("BankAccount", "Create", ct)) return Forbid();
        if (access.IsEnforced("BankAccount"))
        {
            if (model.Id != Guid.Empty) return BadRequest();
            model.IsDeleted = false;
            model.Customer = null;
            model.Accountancies = null;
            model.Checkbooks = null;
            model.Cashes = null;
            model.Cashiers = null;
        }
        return Map(await dispatcher.CommandAsync(new AddBankAccountCommand(model), ct));
    }
    [HttpPost, Authorize(Roles = "مدیر مالی, کارشناس مالی, مدیر ارشد, مدیر عامل")]
    public async Task<IActionResult> ChangeActiveBankAccount([FromBody] BankAccount model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.CanAsync("BankAccount", "ChangeActive", model.Id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new ChangeActiveBankAccountCommand(model), ct));
    }
    [Authorize(Roles = "مدیر مالی, کارشناس مالی, مدیر ارشد, مدیر عامل")]
    public async Task<IActionResult> DelBankAccountItem(Guid id, CancellationToken ct)
    {
        if (!await access.CanAsync("BankAccount", "Delete", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new DelBankAccountItemCommand(id), ct));
    }
    [HttpPost]
    public async Task<IActionResult> AcceptAccountancy([FromQuery] AccStatusEnum status, [FromBody] DescriptionIdVModel model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await CanAcc("Approve", model.Id, ct)) return Forbid();
        if (!Enum.IsDefined(status)) return BadRequest();
        return Map(await dispatcher.CommandAsync(new AcceptAccountancyCommand(status, model.Id, model.Description), ct));
    }
    [HttpGet]
    public async Task<IActionResult> ChangeStatusAllAcc(Guid pfId, CancellationToken ct)
    {
        if (!await CanAllRows(db.Accountancies.Where(x => x.pfid == pfId), "ChangeStatus", ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new ChangeStatusAllAccCommand(pfId), ct));
    }
    [HttpGet]
    public async Task<IActionResult> ChangeStatusAcc(Guid id, CancellationToken ct)
    {
        if (!await CanAcc("ChangeStatus", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new ChangeStatusAccCommand(id), ct));
    }
    private IActionResult Map(AccountancyOutcome result) => result.Message == null ? StatusCode(result.Status) : StatusCode(result.Status, result.Message);
    private async Task<TableRequest> ReadTableAsync(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        int.TryParse(form["start"], out var start);
        int.TryParse(form["length"], out var length);
        var column = form["order[0][column]"].FirstOrDefault();
        return new(form["draw"].FirstOrDefault(), start, length, form[$"columns[{column}][name]"].FirstOrDefault(), form["order[0][dir]"].FirstOrDefault(), form["search[value]"].FirstOrDefault());
    }
}
