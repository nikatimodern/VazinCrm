using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Cashes.Dtos;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Querying;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Cash;
using CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;
using CRM.EndPoint.FrontEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using CRM.Core.ApplicationServices.Cashes.Commands.AddCash;
using CRM.Core.ApplicationServices.Cashes.Commands.AddCashier;
using CRM.Core.ApplicationServices.Cashes.Commands.UpdateCashier;
using CRM.Core.ApplicationServices.Cashes.Commands.ChangeStatusCash;
using CRM.Core.ApplicationServices.Cashes.Commands.ChangeStatusAllCashToPending;
using CRM.Core.ApplicationServices.Cashes.Commands.ChangeStatusGroupCashToPending;
using CRM.Core.ApplicationServices.Cashes.Commands.ApproveCashGroup;
using CRM.Core.ApplicationServices.Cashes.Commands.RejectCashGroup;
using CRM.Core.ApplicationServices.Cashes.Commands.DeleteCash;
using CRM.Core.ApplicationServices.Cashes.Commands.DeleteCashier;
using CRM.Core.ApplicationServices.Cashes.Commands.AddBaseCash;
using CRM.Core.ApplicationServices.Cashes.Commands.DeleteBaseCash;
using CRM.Core.ApplicationServices.Cashes.Commands.PrepareCashGroupExport;
using CRM.Core.ApplicationServices.Cashes.Commands.SendCashesToSoftware;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashDetail;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashierDetail;
using CRM.Core.ApplicationServices.Cashes.Queries.GetBaseCashDetail;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashCenters;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashTitles;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashierOptions;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashierBalance;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashTable;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashGroupTable;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashierTable;
using CRM.Core.ApplicationServices.Cashes.Queries.GetBaseCashTable;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashDetailedAccounts;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashPage;
using CRM.Core.ApplicationServices.Cashes.Queries.GetCashierPage;

namespace CRM.EndPoint.FrontEnd.Controllers;
public class CashController(ICqrsDispatcher dispatcher) : Controller
{
    [Authorize(Roles = "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetCashPageQuery(), ct);
        return View(new CashIndexVModel { IsTaminUser = data.IsTaminUser, CashCenters = data.CashCenters.Select(o => new SelectOptionDto<Guid> { Value = o.Value, Text = o.Text }).ToList(), CashTitles = data.CashTitles.Select(o => new SelectOptionDto<Guid> { Value = o.Value, Text = o.Text }).ToList(), CashierTitles = data.CashierTitles.Select(o => new SelectOptionDto<Guid> { Value = o.Value, Text = o.Text }).ToList() });
    }

    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> Cashier(CancellationToken ct)
    {
        var data = await dispatcher.QueryAsync(new GetCashierPageQuery(), ct);
        return View(data.Select(x => new CashierDto { ExternalSoftware = x.ExternalSoftware, SelectOptions = x.SelectOptions.Select(o => new SelectOptionDto<string> { Value = o.Value, Text = o.Text }).ToList() }).ToList());
    }

    [Authorize(Roles = "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCash([FromBody] Cash model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AddCashCommand(model), ct));
    [Authorize(Roles = "مدیر ارشد,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCashier([FromBody] Cashier model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AddCashierCommand(model), ct));
    [HttpPost]
    public async Task<IActionResult> UpdateCashier([FromBody] Cashier model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new UpdateCashierCommand(model), ct));
    public async Task<IActionResult> ChangeStatusCash(Guid id, CashStatusEnum status, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeStatusCashCommand(id, status), ct));
    public async Task<IActionResult> ChangeStatusAllCashToPending(CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeStatusAllCashToPendingCommand(), ct));
    [HttpPost]
    public async Task<IActionResult> ChangeStatusGroupCashToPending([FromBody] List<Guid> listIds, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ChangeStatusGroupCashToPendingCommand(listIds), ct));
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> ApproveCashGroup(Guid groupId, CancellationToken ct) => Map(await dispatcher.CommandAsync(new ApproveCashGroupCommand(groupId), ct));
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> RejectCashGroup(Guid groupId, CancellationToken ct) => Map(await dispatcher.CommandAsync(new RejectCashGroupCommand(groupId), ct));
    public async Task<IActionResult> DeleteCash(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DeleteCashCommand(id), ct));
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteCashier(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DeleteCashierCommand(id), ct));
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddBaseCash([FromBody] BaseCash model, CancellationToken ct) => Map(await dispatcher.CommandAsync(new AddBaseCashCommand(model), ct));
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteBaseCash(Guid id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DeleteBaseCashCommand(id), ct));
    public async Task<IActionResult> GetCash(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashDetailQuery(id), ct));
    public async Task<IActionResult> GetCashier(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashierDetailQuery(id), ct));
    public async Task<IActionResult> GetBaseCash(Guid id, CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetBaseCashDetailQuery(id), ct));
    public async Task<IActionResult> GetBalanceCashier(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashierBalanceQuery(), ct));
    public async Task<IActionResult> GetAllCashiers(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashierOptionsQuery(), ct));
    public async Task<IActionResult> GetAllTafsiliCodes(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashDetailedAccountsQuery(), ct));
    public async Task<IActionResult> GetAllBaseCashCenter(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashCentersQuery(), ct));
    public async Task<IActionResult> GetAllBaseCashTitle(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashTitlesQuery(), ct));
    public async Task<IActionResult> GetAllCashesDt(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<FilterVModel>(form["model"].FirstOrDefault() ?? "null");
        return Json(await dispatcher.QueryAsync(new GetCashTableQuery(await ReadTableAsync(ct), model.ToApplicationFilter()), ct));
    }

    [HttpPost]
    public async Task<IActionResult> GetCashGroupsDt(CancellationToken ct) => Json(await dispatcher.QueryAsync(new GetCashGroupTableQuery(await ReadTableAsync(ct)), ct));
    public async Task<IActionResult> GetAllCashiersDt(CancellationToken ct) => Json(await dispatcher.QueryAsync(new GetCashierTableQuery(await ReadTableAsync(ct)), ct));
    public async Task<IActionResult> GetAllBaseCashesDt(CancellationToken ct) => Json(await dispatcher.QueryAsync(new GetBaseCashTableQuery(await ReadTableAsync(ct)), ct));
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> SendCashesToExternalSoftware([FromBody] SendToExternalSoftwaresDto model, CancellationToken ct)
    {
        if (model == null)
            return BadRequest();
        return Map(await dispatcher.CommandAsync(new SendCashesToSoftwareCommand(model.Ids ?? [], model.ExternalSoftwareIds ?? []), ct));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCashGroupToExcel(Guid groupId, int? startVoucherNumber = null, CancellationToken ct = default)
    {
        var data = await dispatcher.CommandAsync(new PrepareCashGroupExportCommand(groupId, startVoucherNumber), ct);
        if (data.Status != 200)
            return StatusCode(data.Status, data.Error);
        var cashItems = data.Items;
        var columns = new List<ExcelColumnConfig<Cash>>
        {
            new()
            {
                Header = "نوع قلم",
                ValueSelector = _ => "PaymentDraft"
            },
            new()
            {
                Header = "اعلاميه پرداخت نوع پرداخت",
                ValueSelector = _ => 2
            },
            new()
            {
                Header = "اعلاميه پرداخت طرف مقابل",
                ValueSelector = x => x.CostCenter?.Title
            },
            new()
            {
                Header = "اعلاميه پرداخت كد تفصيل",
                ValueSelector = x => x.CostCenter?.Code
            },
            new()
            {
                Header = "اعلاميه پرداخت شماره",
                ValueSelector = x => x.PaymentVoucherNumber
            },
            new()
            {
                Header = "اعلاميه پرداخت تاريخ",
                ValueSelector = x => x.CostDate
            },
            new()
            {
                Header = "اعلاميه پرداخت كد معين",
                ValueSelector = x => x.CostTitle?.MoeinCode
            },
            new()
            {
                Header = "اعلاميه پرداخت صندوق",
                ValueSelector = _ => "صندوق"
            },
            new()
            {
                Header = "اعلاميه پرداخت مبلغ پرداخت",
                ValueSelector = x => x.Amount
            }, // TODO
            new()
            {
                Header = "اعلاميه پرداخت مبلغ نقد",
                ValueSelector = _ => 0
            },
            new()
            {
                Header = "اعلاميه پرداخت شرح",
                ValueSelector = x => x.CostDescription
            },
            new()
            {
                Header = "اعلاميه پرداخت شرح(2)",
                ValueSelector = x => x.CostDescription
            },
            new()
            {
                Header = "پرداخت چك حساب بانكي1",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك شماره",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك پشت نمره",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك مبلغ",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك تاريخ سررسيد",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك نوع مدت",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك شرح",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك شرح(2)",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "پرداخت چك وضعيت",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "اعلاميه برداشت شماره",
                ValueSelector = x => string.IsNullOrEmpty(x.ReferenceNumber) ? "0" : x.ReferenceNumber
            },
            new()
            {
                Header = "اعلاميه برداشت تاريخ",
                ValueSelector = x => x.CostDate
            },
            new()
            {
                Header = "اعلاميه برداشت مبلغ",
                ValueSelector = x => x.Amount
            },
            new()
            {
                Header = "اعلاميه برداشت حساب بانكي",
                ValueSelector = x => x.BankAccountTxt
            },
            new()
            {
                Header = "اعلاميه برداشت شرح",
                ValueSelector = x => x.CostDescription
            },
            new()
            {
                Header = "اعلاميه برداشت شرح(2)",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "اعلاميه برداشت تفصيل حساب بانكي",
                ValueSelector = _ => ""
            },
            new()
            {
                Header = "اعلاميه برداشت نرخ كارمزد بانكي",
                ValueSelector = _ => 1
            },
            new()
            {
                Header = "اعلاميه برداشت كارمزد بانكي",
                ValueSelector = x => x.Fee == 0 ? "" : x.Fee
            },
            new()
            {
                Header = "اعلاميه برداشت كارمزد بانكي به ارز پايه",
                ValueSelector = x => x.Fee == 0 ? "" : x.Fee
            },
            new()
            {
                Header = "اعلاميه پرداخت كد معين كارمزد بانكي",
                ValueSelector = x => x.Fee == 0 ? "" : 7704
            },
            new()
            {
                Header = "اعلاميه پرداخت تخفيف",
                ValueSelector = _ => 0
            },
            new()
            {
                Header = "اعلاميه پرداخت استقراري",
                ValueSelector = _ => "False"
            }
        };
        return ExcelExportService.ExportToExcel(cashItems, columns, "تنخواه", rightToLeft: false, includeRowNumber: false);
    }

    private IActionResult Map(CashOutcome result) => result.Message == null ? StatusCode(result.Status) : StatusCode(result.Status, result.Message);
    private async Task<TableRequest> ReadTableAsync(CancellationToken ct)
    {
        var form = await Request.ReadFormAsync(ct);
        int.TryParse(form["start"], out var start);
        var length = int.TryParse(form["length"], out var value) ? value : 10;
        var column = form["order[0][column]"].FirstOrDefault();
        return new(form["draw"].FirstOrDefault(), start, length, form[$"columns[{column}][data]"].FirstOrDefault(), form["order[0][dir]"].FirstOrDefault(), form["search[value]"].FirstOrDefault()?.ToLower().Trim());
    }
}
