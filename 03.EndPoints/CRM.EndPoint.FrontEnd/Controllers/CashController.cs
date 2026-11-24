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
using CRM.EndPoint.FrontEnd.Security;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;
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
[Authorize(Policy = "NotCustomerOnly")]
public class CashController(ICqrsDispatcher dispatcher, DocumentAccessPolicy access, CRMContextDb db) : Controller
{
    [LegacyCatalogRoles("Cash", "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Cash", ct)) return Forbid();
        var data = await dispatcher.QueryAsync(new GetCashPageQuery(), ct);
        return View(new CashIndexVModel { IsTaminUser = data.IsTaminUser, CashCenters = data.CashCenters.Select(o => new SelectOptionDto<Guid> { Value = o.Value, Text = o.Text }).ToList(), CashTitles = data.CashTitles.Select(o => new SelectOptionDto<Guid> { Value = o.Value, Text = o.Text }).ToList(), CashierTitles = data.CashierTitles.Select(o => new SelectOptionDto<Guid> { Value = o.Value, Text = o.Text }).ToList() });
    }

    [LegacyCatalogRoles("Cashier", "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> Cashier(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Cashier", ct)) return Forbid();
        var data = await dispatcher.QueryAsync(new GetCashierPageQuery(), ct);
        return View(data.Select(x => new CashierDto { ExternalSoftware = x.ExternalSoftware, SelectOptions = x.SelectOptions.Select(o => new SelectOptionDto<string> { Value = o.Value, Text = o.Text }).ToList() }).ToList());
    }

    [LegacyCatalogRoles("Cash", "مدیر ارشد,تنخواه دار,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCash([FromBody] Cash model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (model.Id == Guid.Empty ? !await access.HasAsync("Cash", "Create", ct) : !await access.CanAsync("Cash", "Edit", model.Id, ct)) return Forbid();
        if (access.IsEnforced("Cash"))
        {
            if (!await access.CanAsync("BaseCash", "View", model.CostTitleId, ct) ||
                !await access.CanAsync("BaseCenterCash", "View", model.CostCenterId, ct) ||
                !model.CashierId.HasValue || !await access.CanAsync("Cashier", "View", model.CashierId.Value, ct)) return Forbid();
            if (model.Id == Guid.Empty)
            {
                model.Status = CashStatusEnum.ثبت_موقت;
                model.CashGroupId = null;
                model.PaymentVoucherNumber = null;
                model.IsDeleted = false;
            }
            else
            {
                var old = await db.Cashes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id, ct);
                if (old == null) return NotFound();
                if (old.Status != CashStatusEnum.ثبت_موقت) return Conflict("فقط سند موقت قابل ویرایش است.");
                if (!await access.CanAsync("BaseCash", "View", old.CostTitleId, ct) ||
                    !await access.CanAsync("BaseCenterCash", "View", old.CostCenterId, ct) ||
                    (old.CashierId.HasValue && !await access.CanAsync("Cashier", "View", old.CashierId.Value, ct))) return Forbid();
                var oldOrders = await db.CashOrderHeads.AsNoTracking().Where(x => x.CashId == model.Id).Select(x => x.OrderHeadId).ToListAsync(ct);
                foreach (var orderId in oldOrders)
                    if (!await access.CanAsync("OrderHead", "View", orderId, ct)) return Forbid();
                model.Status = old.Status;
                model.CashGroupId = old.CashGroupId;
                model.PaymentVoucherNumber = old.PaymentVoucherNumber;
                model.IsDeleted = old.IsDeleted;
            }
            model.Accountancy = null;
            model.CashGroup = null;
            model.CostCenter = null;
            model.CostTitle = null;
            model.BankAccount = null;
            foreach (var link in model.CashOrderHeads ?? [])
            {
                if (!await access.CanAsync("OrderHead", "View", link.OrderHeadId, ct)) return Forbid();
                link.OrderHead = null;
                link.Cash = null;
                link.CashId = model.Id;
            }
            foreach (var attachment in model.Attachments ?? [])
                if (attachment.Id != Guid.Empty && !await db.Attachments.AsNoTracking().AnyAsync(x => x.Id == attachment.Id && x.EntityId == model.Id && x._AttachType == CRM.Core.Domain.Attachments.Enum._AttachType.Cash, ct)) return Forbid();
        }
        if (!await CanUseBankAccount(model.BankAccountId, ct)) return Forbid();
        if (access.IsEnforced("BankAccount"))
        {
            if (model.Id != Guid.Empty)
            {
                var old = await db.Cashes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id, ct);
                if (old == null) return NotFound();
                if (!await CanUseBankAccount(old.BankAccountId, ct)) return Forbid();
            }
            model.BankAccount = null;
        }
        return Map(await dispatcher.CommandAsync(new AddCashCommand(model), ct));
    }

    private async Task<bool> CanUseBankAccount(Guid? id, CancellationToken ct) =>
        !id.HasValue || await access.CanAsync("BankAccount", "View", id.Value, ct);
    [LegacyCatalogRoles("Cashier", "مدیر ارشد,مدیر مالی,کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddCashier([FromBody] Cashier model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.HasAsync("Cashier", "Create", ct)) return Forbid();
        if (access.IsEnforced("Cashier"))
        {
            if (model.Id != Guid.Empty) return BadRequest();
            model.IsDeleted = false;
            model.Cashes = null;
        }
        if (!await CanUseBankAccount(model.BankAccountId, ct)) return Forbid();
        if (access.IsEnforced("BankAccount")) model.BankAccount = null;
        return Map(await dispatcher.CommandAsync(new AddCashierCommand(model), ct));
    }
    [HttpPost]
    public async Task<IActionResult> UpdateCashier([FromBody] Cashier model, CancellationToken ct)
    {
        if (model == null) return BadRequest();
        if (!await access.CanAsync("Cashier", "Edit", model.Id, ct)) return Forbid();
        if (access.IsEnforced("Cashier"))
        {
            var previous = await db.Cashiers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id, ct);
            if (previous == null) return NotFound();
            if (previous.UserId != model.UserId && !await access.HasAsync("Cashier", "Edit", ct)) return Forbid();
        }
        if (!await CanUseBankAccount(model.BankAccountId, ct)) return Forbid();
        if (access.IsEnforced("BankAccount"))
        {
            var old = await db.Cashiers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id, ct);
            if (old == null) return NotFound();
            if (!await CanUseBankAccount(old.BankAccountId, ct)) return Forbid();
            model.BankAccount = null;
        }
        return Map(await dispatcher.CommandAsync(new UpdateCashierCommand(model), ct));
    }
    private async Task<bool> CanCashRows(IQueryable<Cash> rows, string action, CancellationToken ct)
    {
        if (access.IsEnforced("BankAccount"))
        {
            var banks = await access.ApplyAsync(db.BankAccounts.AsNoTracking(), "View", ct);
            if (await rows.AnyAsync(c => c.BankAccountId.HasValue && !banks.Any(b => b.Id == c.BankAccountId.Value), ct)) return false;
        }
        if (!await access.HasAnyAsync("Cash", action, ct)) return false;
        var permitted = await access.ApplyAsync(await access.ApplyAsync(rows, "View", ct), action, ct);
        return !await rows.AnyAsync(x => !permitted.Any(p => p.Id == x.Id), ct);
    }

    private async Task<bool> CanGroup(Guid id, string action, string childAction, CancellationToken ct)
    {
        if (!await access.CanAsync("CashGroup", action, id, ct)) return false;
        return await CanCashRows(db.Cashes.Where(x => x.CashGroupId == id), childAction, ct);
    }

    public async Task<IActionResult> ChangeStatusCash(Guid id, CashStatusEnum status, CancellationToken ct) {
        if (!Enum.IsDefined(status)) return BadRequest();
        if (!await access.CanAsync("Cash", "ChangeStatus", id, ct) || !await CanCashRows(db.Cashes.Where(x => x.Id == id), "ChangeStatus", ct)) return Forbid();
        if (access.IsEnforced("Cash"))
        {
            var row = await db.Cashes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row == null) return NotFound();
            if (row.Status != CashStatusEnum.ثبت_موقت || status != CashStatusEnum.در_دست_تایید)
            {
                if (!await access.CanAsync("Cash", "Approve", id, ct)) return Forbid();
            }
            if (row.CashGroupId.HasValue && !await access.CanAsync("CashGroup", "View", row.CashGroupId.Value, ct)) return Forbid();
            if (!row.CashGroupId.HasValue && status == CashStatusEnum.در_دست_تایید && !await access.HasAsync("CashGroup", "Create", ct)) return Forbid();
        }
        return Map(await dispatcher.CommandAsync(new ChangeStatusCashCommand(id, status), ct));
    }
    public async Task<IActionResult> ChangeStatusAllCashToPending(CancellationToken ct) {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!await access.HasAsync("CashGroup", "Create", ct) ||
            !await CanCashRows(db.Cashes.Where(x => x.UserCreatorID == userId && x.Status == CashStatusEnum.ثبت_موقت), "ChangeStatus", ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new ChangeStatusAllCashToPendingCommand(), ct));
    }
    [HttpPost]
    public async Task<IActionResult> ChangeStatusGroupCashToPending([FromBody] List<Guid> listIds, CancellationToken ct) {
        if (listIds == null || listIds.Count == 0) return BadRequest();
        if (!await access.HasAsync("CashGroup", "Create", ct)) return Forbid();
        foreach (var id in listIds.Distinct())
            if (!await access.CanAsync("Cash", "ChangeStatus", id, ct)) return Forbid();
        if (!await CanCashRows(db.Cashes.Where(x => listIds.Contains(x.Id)), "ChangeStatus", ct)) return Forbid();
        if (access.IsEnforced("Cash") && await db.Cashes.AnyAsync(x => listIds.Contains(x.Id) && x.Status != CashStatusEnum.ثبت_موقت, ct)) return Conflict();
        return Map(await dispatcher.CommandAsync(new ChangeStatusGroupCashToPendingCommand(listIds), ct));
    }
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> ApproveCashGroup(Guid groupId, CancellationToken ct) {
        if (!await CanGroup(groupId, "Approve", "Approve", ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new ApproveCashGroupCommand(groupId), ct));
    }
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> RejectCashGroup(Guid groupId, CancellationToken ct) {
        if (!await CanGroup(groupId, "Approve", "Approve", ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new RejectCashGroupCommand(groupId), ct));
    }
    public async Task<IActionResult> DeleteCash(Guid id, CancellationToken ct) {
        if (!await access.CanAsync("Cash", "Delete", id, ct) || !await CanCashRows(db.Cashes.Where(x => x.Id == id), "Delete", ct)) return Forbid();
        if (access.IsEnforced("Cash") && !await db.Cashes.AnyAsync(x => x.Id == id && x.Status == CashStatusEnum.ثبت_موقت, ct)) return Conflict();
        return Map(await dispatcher.CommandAsync(new DeleteCashCommand(id), ct));
    }
    [LegacyCatalogRoles("Cashier", "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteCashier(Guid id, CancellationToken ct) {
        if (!await access.CanAsync("Cashier", "Delete", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new DeleteCashierCommand(id), ct));
    }
    [LegacyCatalogRoles("BaseCash", "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    [HttpPost]
    public async Task<IActionResult> AddBaseCash([FromBody] BaseCash model, CancellationToken ct) {
        if (model == null) return BadRequest();
        if (model.Id == Guid.Empty ? !await access.HasAsync("BaseCash", "Create", ct) : !await access.CanAsync("BaseCash", "Edit", model.Id, ct)) return Forbid();
        if (access.IsEnforced("BaseCash"))
        {
            model.Cashes = null;
            if (model.Id == Guid.Empty) model.IsDeleted = false;
            else
            {
                var old = await db.BaseCashes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id, ct);
                if (old == null) return NotFound();
                model.UserCreatorID = old.UserCreatorID;
                model.UserCreator = old.UserCreator;
                model.CreateDate = old.CreateDate;
                model.CreateDateTxt = old.CreateDateTxt;
                model.IsDeleted = old.IsDeleted;
            }
        }
        return Map(await dispatcher.CommandAsync(new AddBaseCashCommand(model), ct));
    }
    [LegacyCatalogRoles("BaseCash", "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> DeleteBaseCash(Guid id, CancellationToken ct) {
        if (!await access.CanAsync("BaseCash", "Delete", id, ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new DeleteBaseCashCommand(id), ct));
    }
    public async Task<IActionResult> GetCash(Guid id, CancellationToken ct) {
        if (!await access.CanAsync("Cash", "View", id, ct)) return Forbid();
        if (access.IsEnforced("BankAccount"))
        {
            var row = await db.Cashes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row == null) return NotFound();
            if (!await CanUseBankAccount(row.BankAccountId, ct)) return Forbid();
        }
        return Ok(await dispatcher.QueryAsync(new GetCashDetailQuery(id), ct));
    }
    public async Task<IActionResult> GetCashier(Guid id, CancellationToken ct) {
        if (!await access.CanAsync("Cashier", "View", id, ct)) return Forbid();
        if (access.IsEnforced("BankAccount"))
        {
            var row = await db.Cashiers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row == null) return NotFound();
            if (!await CanUseBankAccount(row.BankAccountId, ct)) return Forbid();
        }
        return Ok(await dispatcher.QueryAsync(new GetCashierDetailQuery(id), ct));
    }
    public async Task<IActionResult> GetBaseCash(Guid id, CancellationToken ct) {
        if (!await access.CanAsync("BaseCash", "View", id, ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetBaseCashDetailQuery(id), ct));
    }
    public async Task<IActionResult> GetBalanceCashier(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Cashier", ct)) return Forbid();
        if (access.IsEnforced("Cashier") && Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            var visible = await access.ApplyAsync(db.Cashiers.AsNoTracking().Where(c => c.UserId == userId), "View", ct);
            if (!await visible.AnyAsync(ct)) return Forbid();
        }
        return Ok(await dispatcher.QueryAsync(new GetCashierBalanceQuery(), ct));
    }
    public async Task<IActionResult> GetAllCashiers(bool useEntityId = false, CancellationToken ct = default) {
        if (!await access.HasViewAsync("Cashier", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetCashierOptionsQuery(useEntityId), ct));
    }
    public async Task<IActionResult> GetAllTafsiliCodes(CancellationToken ct) => Ok(await dispatcher.QueryAsync(new GetCashDetailedAccountsQuery(), ct));
    public async Task<IActionResult> GetAllBaseCashCenter(CancellationToken ct) {
        if (!await access.HasViewAsync("BaseCenterCash", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetCashCentersQuery(), ct));
    }
    public async Task<IActionResult> GetAllBaseCashTitle(CancellationToken ct) {
        if (!await access.HasViewAsync("BaseCash", ct)) return Forbid();
        return Ok(await dispatcher.QueryAsync(new GetCashTitlesQuery(), ct));
    }
    public async Task<IActionResult> GetAllCashesDt(CancellationToken ct)
    {
        if (!await access.HasViewAsync("Cash", ct)) return Forbid();
        var form = await Request.ReadFormAsync(ct);
        var model = JsonConvert.DeserializeObject<FilterVModel>(form["model"].FirstOrDefault() ?? "null");
        var filter = model.ToApplicationFilter();
        if (Guid.TryParse(filter?.EntityId, out var groupId) && !await CanGroup(groupId, "View", "View", ct)) return Forbid();
        var result = await dispatcher.QueryAsync(new GetCashTableQuery(await ReadTableAsync(ct), filter), ct);
        var permissions = new Dictionary<Guid, object>();
        foreach (var row in result.Data)
            permissions[row.Id] = new
            {
                canEdit = await access.AllowsRowAsync(row, "Edit", ct),
                canDelete = await access.AllowsRowAsync(row, "Delete", ct),
                canChangeStatus = await access.AllowsRowAsync(row, "ChangeStatus", ct),
                canApprove = await access.AllowsRowAsync(row, "Approve", ct),
                canExport = await access.AllowsRowAsync(row, "ExportExternal", ct)
            };
        return Json(new { result.Draw, result.RecordsTotal, result.RecordsFiltered, result.Data, result.IsExported, result.CashierBalance, permissions });
    }

    [HttpPost]
    public async Task<IActionResult> GetCashGroupsDt(CancellationToken ct) {
        if (!await access.HasViewAsync("CashGroup", ct)) return Forbid();
        return Json(await dispatcher.QueryAsync(new GetCashGroupTableQuery(await ReadTableAsync(ct)), ct));
    }
    public async Task<IActionResult> GetAllCashiersDt(CancellationToken ct) {
        if (!await access.HasViewAsync("Cashier", ct)) return Forbid();
        var result = await dispatcher.QueryAsync(new GetCashierTableQuery(await ReadTableAsync(ct)), ct);
        var permissions = new Dictionary<Guid, object>();
        foreach (var row in result.Data)
            permissions[row.Cashier.Id] = new
            {
                canEdit = await access.AllowsRowAsync(row.Cashier, "Edit", ct),
                canDelete = await access.AllowsRowAsync(row.Cashier, "Delete", ct)
            };
        return Json(new { result.Draw, result.RecordsTotal, result.RecordsFiltered, result.Data, permissions });
    }
    public async Task<IActionResult> GetAllBaseCashesDt(CancellationToken ct) {
        if (!await access.HasViewAsync("BaseCash", ct)) return Forbid();
        var result = await dispatcher.QueryAsync(new GetBaseCashTableQuery(await ReadTableAsync(ct)), ct);
        var permissions = new Dictionary<Guid, object>();
        foreach (var row in result.Data)
            permissions[row.Id] = new
            {
                canEdit = await access.AllowsRowAsync(row, "Edit", ct),
                canDelete = await access.AllowsRowAsync(row, "Delete", ct)
            };
        return Json(new { result.Draw, result.RecordsTotal, result.RecordsFiltered, result.Data, permissions });
    }
    [Authorize(Roles = "مدیر ارشد, مدیر مالی, کارشناس مالی")]
    public async Task<IActionResult> SendCashesToExternalSoftware([FromBody] SendToExternalSoftwaresDto model, CancellationToken ct)
    {
        if (model == null)
            return BadRequest();
        foreach (var id in model.Ids ?? [])
            if (!await access.CanAsync("Cash", "ExportExternal", id, ct) || !await CanCashRows(db.Cashes.Where(x => x.Id == id), "ExportExternal", ct)) return Forbid();
        return Map(await dispatcher.CommandAsync(new SendCashesToSoftwareCommand(model.Ids ?? [], model.ExternalSoftwareIds ?? []), ct));
    }

    [HttpGet]
    public async Task<IActionResult> ExportCashGroupToExcel(Guid groupId, int? startVoucherNumber = null, CancellationToken ct = default)
    {
        if (!await CanGroup(groupId, "Export", "ExportExternal", ct)) return Forbid();
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
