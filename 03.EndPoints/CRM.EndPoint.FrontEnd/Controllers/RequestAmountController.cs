using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.RequestAmount;
using CRM.EndPoint.FrontEnd.Models.Supply;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class RequestAmountController : Controller {
    private readonly UserManager<AppUser> userManager;
    private readonly FileSaver fileSaver;
    private readonly DateTimeServices dtService;
    private readonly CRMContextDb db;
    private readonly LogSystemService _logSystemService;
    private readonly IViewRenderService vrs;
    private readonly IHubContext<TaskHub> hub;

    public RequestAmountController(UserManager<AppUser> userManager, FileSaver fileSaver,
        DateTimeServices dtService, CRMContextDb db, LogSystemService logSystemService, IViewRenderService vrs,
        IHubContext<TaskHub> hub)
    {
        this.userManager = userManager;
        this.fileSaver = fileSaver;
        this.dtService = dtService;
        this.db = db;
        _logSystemService = logSystemService;
        this.vrs = vrs;
        this.hub = hub;
    }


    [Authorize(Roles = "مدیر تامین ,کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی, تنخواه دار")]
    public IActionResult Index()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> AddRequestAmount([FromBody] RequestAmount model)
    {
        if (model == null) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            model.UserCreator = user.FullName;
            model.UserCreatorID = user.Id;
            model.RequestStatusTxt = model.RequestStatus.ToString().Replace("_", " ");
            model.RequestAmountTypeTxt = model.RequestAmountType.ToString().Replace("_", " ");
            model.DocTypeTxt = model.DocType.ToString().Replace("_", " ");
            await db.RequestAmounts.AddAsync(model);
            await db.SaveChangesAsync();

            await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRequestAmount([FromBody] RequestAmount model)
    {
        if (model == null) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            model.RequestStatusTxt = model.RequestStatus.ToString().Replace("_", " ");
            model.DocTypeTxt = model.DocType.ToString().Replace("_", " ");
            model.ModifyDate = DateTime.Now;
            model.ModifyDateTxt = dtService.GetDateTimePersian();
            model.UserModify = user.FullName;
            model.UserModifyID = user.Id;

            var entity = await db.RequestAmounts.Include(x => x.RequestAmountSupplyFactor).Include(x => x.RequestAmountOrderHeads)
                .FirstOrDefaultAsync(x => x.Id == model.Id);
            if (entity == null) return NotFound("رکوردی یافت نشد!");

            await _logSystemService.LogChange(entity, model, LogActionEnum.ویرایش, user, ["RequestAmountOrderHeads"]);

            db.Entry(entity).CurrentValues.SetValues(model);
            entity.RequestAmountSupplyFactor = model.RequestAmountSupplyFactor;
            entity.RequestAmountOrderHeads = model.RequestAmountOrderHeads;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> ChangeDocTypeRequestAmount([FromBody] RequestAmount model)
    {
        if (model == null) return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldItem = await db.RequestAmounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id);
            var item = await db.RequestAmounts.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (item == null) return NotFound("رکوردی یافت نشد!");


            item.DocType = model.DocType;
            item.DocTypeTxt = model.DocType.ToString().Replace("_", " ");
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;


            await _logSystemService.LogChange(oldItem, item, LogActionEnum.ویرایش, user, ["RequestAmountSupplyFactor"]);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }


    [HttpPost]
    public async Task<IActionResult> SetAcceptedRequestAmount([FromBody] RequestAmount model)
    {
        if (model == null) return BadRequest();

        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var oldItem = await db.RequestAmounts.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
            var item = await db.RequestAmounts.Where(f => f.Id == model.Id).FirstOrDefaultAsync();
            item.AcceptedAmount = model.AcceptedAmount;
            item.RequestStatus = RequestStatusEnum.در_انتظار_ارسال_به_مالی;
            item.RequestStatusTxt = nameof(RequestStatusEnum.در_انتظار_ارسال_به_مالی).Replace("_", " ");

            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.AcceptedAmountPercent = model.AcceptedAmountPercent;

            await _logSystemService.LogChange(oldItem, item, LogActionEnum.تایید_و_تغییر_وضعیت, user);
            await db.SaveChangesAsync();
            await hub.Clients.Groups("مدیر عامل", "مدیر ارشد").SendAsync("countOfPendingCartable");
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> ChangeStatusRequestAmount(Guid id, RequestStatusEnum status)
    {
        if (id == Guid.Empty) return BadRequest();

        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var item = await db.RequestAmounts.Include(f => f.RequestAmountPayments).FirstOrDefaultAsync(f => f.Id == id);
            if (item == null) return NotFound("رکوردی یافت نشد!");

            await ChangeRequestAmountStatus(item, status, user);

            await db.SaveChangesAsync();
            await hub.Clients.Groups("مدیر عامل", "مدیر ارشد", "کارشناس مالی").SendAsync("countOfPendingCartable");
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    private async Task ChangeRequestAmountStatus(RequestAmount requestAmount, RequestStatusEnum newStatus, AppUser user)
    {
        try
        {
            if (requestAmount == null) return;

            if (requestAmount.RequestStatus != newStatus)
            {
                var oldItem = await db.RequestAmounts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == requestAmount.Id);

                requestAmount.RequestStatus = newStatus;
                requestAmount.RequestStatusTxt = newStatus.ToString().Replace("_", " ");

                if (newStatus == RequestStatusEnum.ثبت_اولیه)
                {
                    requestAmount.AcceptedAmount = 0;
                    requestAmount.AcceptedAmountPercent = null;
                }

                requestAmount.UserModify = user.FullName;
                requestAmount.UserModifyID = user.Id;
                requestAmount.ModifyDate = DateTime.Now;
                requestAmount.ModifyDateTxt = dtService.GetDateTimePersian();

                await _logSystemService.LogChange(oldItem, requestAmount, LogActionEnum.تغییر_وضعیت, user);
            }

            if (requestAmount.RequestAmountType == RequestAmountTypeEnum.تنخواه)
            {
                await SyncCashAccountancyCredit(requestAmount, user);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Authorize(Roles = "کارشناس مالی, مدیر مالی, مدیر ارشد")]
    [HttpPost]
    public async Task<IActionResult> AddPaymentByRequestAmount([FromBody] Payment model)
    {
        if (model == null) return BadRequest();

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var validation = await ValidateRequestAmountsPayment(model);
            if (!validation.IsValid) return BadRequest(validation.Message);

            var isNewPayment = model.Id == Guid.Empty;
            await AddOrUpdatePayment(model, user, isNewPayment);

            await SyncRequestAmountsPaymentState(model, user, isNewPayment);

            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }

    private async Task SyncRequestAmountsPaymentState(Payment payment, AppUser user, bool changeStatus)
    {
        try
        {
            var idsList = payment?.RequestAmountPayments?.Select(x => x.RequestAmountId).Distinct().ToList();

            var requestAmounts = await db.RequestAmounts
                .Where(x => idsList.Contains(x.Id))
                .Where(x => x.RequestStatus == RequestStatusEnum.پرداخت_شده || x.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت)
                .ToListAsync();

            if (requestAmounts.Count == 0) return;

            foreach (var reqAmount in requestAmounts)
            {
                var newStatus = reqAmount.RequestStatus;

                if (changeStatus)
                {
                    var totalPaid = await db.RequestAmountPayments.Where(x => x.RequestAmountId == reqAmount.Id).SumAsync(x => x.Amount);

                    newStatus = totalPaid > 0 ? RequestStatusEnum.پرداخت_شده : RequestStatusEnum.در_انتظار_پرداخت;
                }

                await ChangeRequestAmountStatus(reqAmount, newStatus, user);
            }

            await db.SaveChangesAsync();
            await hub.Clients.Groups("مدیر ارشد", "کارشناس مالی").SendAsync("countOfPendingCartable");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    private async Task SyncCashAccountancyCredit(RequestAmount requestAmount, AppUser user)
    {
        try
        {
            if (requestAmount == null) return;

            var acc = await db.Accountancies.FirstOrDefaultAsync(x => x.RequestAmountId == requestAmount.Id);

            if (requestAmount.RequestStatus == RequestStatusEnum.پرداخت_شده)
            {
                var totalPaid = await db.RequestAmountPayments.Where(x => x.RequestAmountId == requestAmount.Id).SumAsync(x => x.Amount);
                if (acc == null)
                {
                    acc = new Accountancy {
                        CashierId = requestAmount.CashierId,
                        RequestAmountId = requestAmount.Id,
                        InvoiceType = InvoiceTypeEnum.تنخواه,
                        InvoiceTypeTxt = nameof(InvoiceTypeEnum.تنخواه),
                        AccType = AcEnum.واریزی,
                        AccTypeTxt = nameof(AcEnum.واریزی),
                        credit = totalPaid,
                        UserCreatorID = user.Id,
                        UserCreator = user.FullName,
                        Accepted = true,
                        AcceptedUserId = user.Id,
                        AcceptedUser = user.FullName,
                        PayType = PayTypeEnum.نقدی,
                        PayTypeTxt = nameof(PayTypeEnum.نقدی),
                        Status = AccStatusEnum.تایید_شده,
                    };

                    await db.Accountancies.AddAsync(acc);
                } else
                {
                    acc.credit = totalPaid;
                    acc.UserModifyID = user.Id;
                    acc.UserModify = user.FullName;
                    acc.ModifyDate = DateTime.Now;
                    acc.ModifyDateTxt = dtService.GetDateTimePersian();
                }
            } else
            {
                if (acc != null)
                {
                    db.Accountancies.Remove(acc);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    private async Task AddOrUpdatePayment(Payment model, AppUser user, bool isNewPayment)
    {
        if (model == null) return;

        try
        {
            model.PaymentDate = dtService.GetDateMiladi(model.PaymentDateTxt);
            if (isNewPayment)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                if (model.RequestAmountPayments?.Count == 1)
                    model.PaymentMode = PaymentModeEnum.تکی;

                await db.Payments.AddAsync(model);
                //await db.SaveChangesAsync();

                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            } else
            {
                var oldModel = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == model.Id);

                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                db.Payments.Update(model);
                db.Entry(model).Property(f => f.PaymentNumber).IsModified = false;

                await _logSystemService.LogChange(oldModel, model, LogActionEnum.ویرایش, user);
            }

            var attachList = fileSaver.SaveAttachments(model.Attachments, _AttachType.Payment);
            foreach (var att in attachList ?? [])
            {
                att?.EntityId = model.Id;
            }

            db.Attachments.UpdateRange(attachList);
            await db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    private async Task<(bool IsValid, string Message)> ValidateRequestAmountsPayment(Payment model)
    {
        var requestAmountIds = model.RequestAmountPayments.Select(x => x.RequestAmountId).Distinct().ToList();

        var requestAmounts = await db.RequestAmounts
            .AsNoTracking()
            .Where(x => requestAmountIds.Contains(x.Id))
            .Select(x => new {
                x.Id,
                x.AcceptedAmount,
                PaidAmount = x.RequestAmountPayments.Where(p => p.PaymentId != model.Id).Sum(p => p.Amount)
            })
            .ToDictionaryAsync(x => x.Id);

        foreach (var reqAmountPayment in model.RequestAmountPayments ?? [])
        {
            if (!requestAmounts.TryGetValue(reqAmountPayment.RequestAmountId, out var requestAmount))
                continue;

            var finalAmount = requestAmount.PaidAmount + reqAmountPayment.Amount;

            if (finalAmount > requestAmount.AcceptedAmount)
            {
                return (false, "مبلغ پرداخت بیشتر از مبلغ تایید شده است.");
            }
        }

        return (true, "");
    }


    public async Task<IActionResult> DeletePayment(Guid id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Payments.Include(f => f.RequestAmountPayments).Where(f => f.Id == id).FirstOrDefaultAsync();
            if (item == null) return NotFound("رکوردی یافت نشد!");

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            foreach (var rap in item.RequestAmountPayments ?? [])
            {
                rap.IsDeleted = true;
            }

            await _logSystemService.LogChange(item, null, LogActionEnum.حذف, user);
            await db.SaveChangesAsync();


            /*var requestAmountIds = item.RequestAmountPayments?.Select(x => x.RequestAmountId).Distinct().ToList() ?? [];
            var requestIdsWithPayment = await db.RequestAmountPayments
                .Where(x => requestAmountIds.Contains(x.RequestAmountId))
                .Select(x => x.RequestAmountId)
                .Distinct()
                .ToListAsync();
            var requestIdsWithoutPayment = requestAmountIds.Except(requestIdsWithPayment).ToList();

            var requestWithoutPayment = await db.RequestAmounts
                .Where(f => requestIdsWithoutPayment.Contains(f.Id))
                .Include(f => f.RequestAmountPayments)
                .ToListAsync();
            foreach (var reqAmount in requestWithoutPayment)
            {
                await ChangeRequestAmountStatus(reqAmount, RequestStatusEnum.در_انتظار_پرداخت, user);
            }*/

            await SyncRequestAmountsPaymentState(item, user, true);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Ok();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            return BadRequest();
        }
    }


    [HttpPost]
    public async Task<IActionResult> GetAllPaymentsByRequestAmount([FromQuery] Guid id)
    {
        try
        {
            var hasAccess = User.IsFinancial() || User.IsAdmin();

            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();


            var query = db.Payments
                .Where(p => p.RequestAmountPayments.Any(x => x.RequestAmountId == id))
                .Select(p => new {
                    payment = p,
                    RequestPaymentAmount = p.RequestAmountPayments
                        .Where(x => x.RequestAmountId == id)
                        .Select(x => x.Amount)
                        .FirstOrDefault(),
                })
                .OrderByDescending(f => f.payment.CreateDate)
                .AsNoTracking();


            var recordsTotal = query.Count();


            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "PaymentNumber" => isAscending
                        ? query.OrderBy(f => f.payment.PaymentNumber)
                        : query.OrderByDescending(f => f.payment.PaymentNumber),
                    "RequestPaymentAmount" => isAscending
                        ? query.OrderBy(f => f.RequestPaymentAmount)
                        : query.OrderByDescending(f => f.RequestPaymentAmount),
                    "CreateDateTxt" => isAscending
                        ? query.OrderBy(f => f.payment.CreateDate)
                        : query.OrderByDescending(f => f.payment.CreateDate),
                    "PaymentDateTxt" => isAscending
                        ? query.OrderBy(f => f.payment.PaymentDate)
                        : query.OrderByDescending(f => f.payment.PaymentDate),
                    _ => query
                };
            }

            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data,
                hasAccess
            });
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }


    public async Task<IActionResult> GetAllRequestAmountsPartial(int type = -1, string search = "", Guid? orderHeadId = null)
    {
        try
        {
            var baseQuery = db.RequestAmounts.OrderByDescending(f => f.CreateDate).AsNoTracking();

            if (!User.IsAdmin() && !User.IsFinancial() && !User.IsManagingDirector() && !User.IsTaminAdmin())
            {
                baseQuery = baseQuery.Where(f => f.UserCreatorID == User.GetUserId());
            }

            if (orderHeadId.HasValue)
            {
                baseQuery = baseQuery.Where(x =>
                    x.RequestAmountOrderHeads.Any(ra => ra.OrderHeadId == orderHeadId.Value)
                );
            }

            if (!string.IsNullOrEmpty(search))
            {
                baseQuery = baseQuery.Where(f =>
                    f.RequestNumber == TextService.convertToInt(search) ||
                    f.Description.Contains(search) ||
                    f.SupplierTxt.Contains(search) ||
                    f.UserCreator.Contains(search) ||
                    f.CashierTxt.Contains(search) ||
                    f.RequestAmountSupplyFactor.Any(raSf =>
                        db.SupplyFactors.Any(sf => sf.Id == raSf.SupplyFactorId && sf.SerialNumber == search)
                    ) ||
                    f.RequestAmountOrderHeads.Any(raOh =>
                        db.OrderHeads.Any(oh => oh.Id == raOh.OrderHeadId && oh.OrderNumber.ToString() == search)
                    )
                );
            }

            var filterCounts = new Dictionary<int, int> {
                [-1] = await baseQuery.CountAsync(),
                [1] = await baseQuery.Where(f =>
                    f.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل ||
                    f.RequestStatus == RequestStatusEnum.در_انتظار_ارسال_به_مالی
                ).CountAsync(),
                [2] = await baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت).CountAsync(),
                [3] = await baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.پرداخت_شده).CountAsync(),
            };


            var query = type switch {
                1 => baseQuery.Where(f =>
                    f.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل ||
                    f.RequestStatus == RequestStatusEnum.در_انتظار_ارسال_به_مالی
                ),
                2 => baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت),
                3 => baseQuery.Where(f => f.RequestStatus == RequestStatusEnum.پرداخت_شده),
                _ => baseQuery
            };


            var supplierFinancials = db.GetSupplierFinancialSummaries();

            var data = await query
                .GroupJoin(supplierFinancials,
                    ra => ra.SupplierId,
                    sf => sf.SupplierId,
                    (ra, financials) => new {
                        RequestAmount = ra,
                        Financial = financials.FirstOrDefault()
                    }
                )
                .Select(x => new RequestAmountVModel {
                    RequestAmount = x.RequestAmount,
                    SupplyFactorSerials = x.RequestAmount.RequestAmountSupplyFactor.Select(x => x.SupplyFactor.SerialNumber).ToList(),
                    OrderHeadSerials = x.RequestAmount.RequestAmountOrderHeads.Select(x => x.OrderHead.OrderNumber).ToList(),
                    CustomerNames = x.RequestAmount.RequestAmountOrderHeads.Select(x => x.OrderHead.CustomerTxt).ToList(),
                    TotalPaymentAmount = x.RequestAmount.RequestAmountPayments.Sum(x => x.Amount),
                    SupplierDebt = x.Financial != null ? x.Financial.SupplierDebt : 0,
                    TotalSupplyFactor = x.Financial != null ? x.Financial.TotalSupplyAmount : 0,
                    TotalSupplyPayment = x.Financial != null ? x.Financial.TotalPaidAmount : 0,
                })
                .ToListAsync();


            /*var data = await query
                .Select(ra => new RequestAmountVModel {
                    RequestAmount = ra,
                    SupplyFactorSerials = ra.RequestAmountSupplyFactor.Select(x => x.SupplyFactor.FactorSerial).ToList(),
                    OrderHeadSerials = ra.RequestAmountOrderHeads.Select(x => x.OrderHead.OrderNumber).ToList(),
                    CustomerNames = ra.RequestAmountOrderHeads.Select(x => x.OrderHead.CustomerTxt).ToList(),
                    TotalPaymentAmount = ra.RequestAmountPayments.Sum(x => x.Amount),
                })
                .ToListAsync();*/


            var html = await vrs.RenderToStringAsync("GetAllRequestAmountsPartial", data);

            return Ok(new { count = data.Count, html, filterCounts });
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetRequestAmount(Guid id)
    {
        try
        {
            var data = await db.RequestAmounts.AsNoTracking()
                .Where(f => f.Id == id)
                .Include(f => f.RequestAmountOrderHeads)
                .Include(f => f.RequestAmountSupplyFactor)
                .Select(ra => new RequestAmountVModel {
                    RequestAmount = ra,
                    SupplyFactorSerials = ra.RequestAmountSupplyFactor
                        .Join(db.SupplyFactors,
                            raSf => raSf.SupplyFactorId,
                            sf => sf.Id,
                            (raSf, sf) => sf.SerialNumber
                        )
                        .ToList(),
                    OrderHeadSerials = ra.RequestAmountOrderHeads
                        .Join(db.OrderHeads,
                            raOh => raOh.OrderHeadId,
                            oh => oh.Id,
                            (raOh, oh) => oh.OrderNumber
                        )
                        .ToList(),
                })
                .FirstOrDefaultAsync();

            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [HttpPost]
    public async Task<IActionResult> GetRequestsAmountByFilteredIds([FromBody] GetAllBySelectedVModel model)
    {
        try
        {
            var data = await db.RequestAmounts.Include(f=>f.RequestAmountPayments).AsNoTracking()
                    .Where(f => model.FilterIds.Contains(f.Id))
                    .ToListAsync();

            var SuCount = data.Select(f => f.SupplierId).Distinct().Count();
            if (SuCount > 1)
            {

                return BadRequest("در واریزی گروهی تمام درخواست ها باید برای یک تامیین کننده باشد ");
            }


            


            return Ok(data);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetRequestAmountsWithPaidAmountByPayment(Guid paymentId)
    {
        try
        {

            //var rqAm =await db.RequestAmountPayments.AsNoTracking()
            //    .Where(f => f.PaymentId == paymentId).Select(f => f.RequestAmountId).ToListAsync();

            //var dataa =await db.RequestAmounts.Include(f => f.RequestAmountPayments)
            //    .Where(f => rqAm.Contains(f.Id)).ToListAsync();


            var dataa = await db.RequestAmounts
    .AsNoTracking()
    .Include(f => f.RequestAmountPayments)
    .Where(f => f.RequestAmountPayments
        .Any(p => p.PaymentId == paymentId))
    .ToListAsync();






            //var data = await db.RequestAmountPayments.Include(f=>f.RequestAmount)
            //    .AsNoTracking()
            //    .Where(x => x.PaymentId == paymentId)
            //    .Select(x => new {
            //        Id = x.RequestAmountId,

            //        PaidAmount = x.Amount,

            //        x.RequestAmount.RequestNumber,
            //        x.RequestAmount.Amount,
            //        x.RequestAmount.AcceptedAmount,
            //        x.RequestAmount.AcceptedAmountPercent,
            //        x.RequestAmount
            //    })
            //    .ToListAsync();

            return Ok(dataa);
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }


    public async Task<IActionResult> DeleteRequestAmount(Guid id)
    {
        try
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.RequestAmounts
                .Where(f => f.Id == id)
                .Include(f => f.RequestAmountOrderHeads)
                .Include(f => f.RequestAmountSupplyFactor)
                .FirstOrDefaultAsync();

            if (item == null) return NotFound("رکوردی یافت نشد!");

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            foreach (var i in item.RequestAmountOrderHeads ?? [])
            {
                i.IsDeleted = true;
            }

            foreach (var i in item.RequestAmountSupplyFactor ?? [])
            {
                i.IsDeleted = true;
            }

            await _logSystemService.LogChange(item, null, LogActionEnum.حذف, user);
            await db.SaveChangesAsync();
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest();
        }
    }

    public async Task<IActionResult> GetAllRequestAmountsDt()
    {
        try
        {
            var draw = Request.Form["draw"].FirstOrDefault();
            var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
            var model = JsonConvert.DeserializeObject<FilterVModel>(Request.Form["model"].FirstOrDefault());
            var length = Convert.ToInt32(Request.Form["length"].FirstOrDefault() ?? "10");
            var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = Request.Form["search[value]"].FirstOrDefault()?.ToLower().Trim();

            var query = db.RequestAmounts.OrderByDescending(f => f.CreateDate).AsNoTracking();

            if (!string.IsNullOrEmpty(searchValue))
            {
                query = query.Where(f =>
                    f.Amount == TextService.convertToInt(searchValue)
                );
            }

            var recordsTotal = query.Count();


            if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
            {
                var isAscending = sortColumnDirection == "asc";
                query = sortColumn switch {
                    "Amount" => isAscending
                        ? query.OrderBy(f => f.Amount)
                        : query.OrderByDescending(f => f.Amount),
                    "AcceptedAmount" => isAscending
                        ? query.OrderBy(f => f.AcceptedAmount)
                        : query.OrderByDescending(f => f.AcceptedAmount),
                    "DocType" => isAscending
                        ? query.OrderBy(f => f.DocType)
                        : query.OrderByDescending(f => f.DocType),
                    _ => query
                };
            }


            if (length != -1)
                query = query.Skip(start).Take(length);

            var data = await query.ToListAsync();

            return Json(new {
                draw,
                recordsTotal,
                recordsFiltered = recordsTotal,
                data,
            });
        }
        catch (Exception)
        {
            return BadRequest();
        }
    }
}