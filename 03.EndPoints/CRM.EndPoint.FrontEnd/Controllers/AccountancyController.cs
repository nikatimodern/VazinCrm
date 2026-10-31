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
using Newtonsoft.Json;
using CRM.Core.ApplicationServices.Accountancies.Queries.GetAccountancyDetails;
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

namespace CRM.EndPoint.FrontEnd.Controllers;
public class AccountancyController(ICqrsDispatcher dispatcher) : Controller
{
    [Authorize(Policy = "NotCustomerOnly")]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await dispatcher.QueryAsync(new GetWorkspacePageQuery("Accountancy"), ct));
    [HttpGet]
    public async Task<IActionResult> getAccountancy(Guid id, bool parent = false, CancellationToken ct = default)
    {
        var data = await dispatcher.QueryAsync(new GetAccountancyDetailsQuery(id, parent), ct);
        return data.AccData == null ? NotFound("رکوردی یافت نشد!") : Ok(new AccViewModel { AccData = data.AccData, ListAttach = data.ListAttach });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAmountPartial(int type, Guid? id, CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetInvoiceTotalsQuery(type, id), ct);
        if (data == null)
            return NotFound("پیش فاکتور یافت نشد");
        return PartialView(new PaymentListViewModel { Discount = data.Discount, PaymentAmount = data.PaymentAmount, ReciveAmount = data.ReciveAmount, BackwardAmount = data.BackwardAmount, SumTotalAmount = data.SumTotalAmount, SumTotalAmountTax = data.SumTotalAmountTax, TotalTax = data.TotalTax });
    }

    public async Task<IActionResult> GetAllPaymentCartablePartial(string serVal, CartableEnum cartable, int type = 1, PayTypeEnum? payType = null, CancellationToken ct = default) => Ok(await dispatcher.QueryAsync(new GetAccountancyCartableQuery(serVal, cartable, type, payType), ct));
    [HttpGet]
    public async Task<IActionResult> GetAllPaymentPartial(int type, Guid id, int passType, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetAccountancyPaymentsQuery(type, id, passType), ct));
    [HttpGet]
    public async Task<IActionResult> CheckWallet(Guid cusId, CancellationToken ct) => Ok((await dispatcher.QueryAsync(new GetCustomerWalletQuery(cusId), ct)).Count > 0);
    [HttpGet]
    public async Task<IActionResult> GetWalletPartialIfAny(Guid cusId, CancellationToken ct) => PartialView("GetAllWalletPartial", await dispatcher.QueryAsync(new GetCustomerWalletQuery(cusId), ct));
    public async Task<IActionResult> GetAllAccount(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<AccFilterVModel>(form["modelF"].FirstOrDefault() ?? "null") ?? new();
        return Ok(await dispatcher.QueryAsync(new GetAccountanciesTableQuery(await ReadTableAsync(ct), model.ToApplicationFilter(), model.AccType, model.PayType), ct));
    }

    public async Task<IActionResult> GetAllBankAccountJson(BankAccountTypeEnum? type = null, CancellationToken ct = default) => Ok(await dispatcher.QueryAsync(new GetBankAccountOptionsQuery(type), ct));
    public async Task<IActionResult> GetAllBankAccount(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<FilterVModel>(form["model"].FirstOrDefault() ?? "null");
        return Ok(await dispatcher.QueryAsync(new GetBankAccountsTableQuery(await ReadTableAsync(ct), model?._type), ct));
    }

    public async Task<IActionResult> GetBankAccount(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetBankAccountDetailsQuery(id), ct));
    public async Task<IActionResult> DelAccountancy(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DelAccountancyCommand(id), ct));
    [HttpGet]
    public async Task<IActionResult> AddInvoiceWalletToPf(Guid id, Guid pfId, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AddInvoiceWalletToPfCommand(id, pfId), ct));
    [HttpPost]
    public async Task<IActionResult> UpBankAccount([FromBody] BankAccount model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new UpBankAccountCommand(model), ct));
    [HttpPost]
    public async Task<IActionResult> AddBankAccount([FromBody] BankAccount model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AddBankAccountCommand(model), ct));
    [HttpPost]
    public async Task<IActionResult> ChangeActiveBankAccount([FromBody] BankAccount model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeActiveBankAccountCommand(model), ct));
    public async Task<IActionResult> DelBankAccountItem(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DelBankAccountItemCommand(id), ct));
    [HttpPost]
    public async Task<IActionResult> AcceptAccountancy([FromQuery] AccStatusEnum status, [FromBody] DescriptionIdVModel model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AcceptAccountancyCommand(status, model.Id, model.Description), ct));
    [HttpGet]
    public async Task<IActionResult> ChangeStatusAllAcc(Guid pfId, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeStatusAllAccCommand(pfId), ct));
    [HttpGet]
    public async Task<IActionResult> ChangeStatusAcc(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeStatusAccCommand(id), ct));
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
