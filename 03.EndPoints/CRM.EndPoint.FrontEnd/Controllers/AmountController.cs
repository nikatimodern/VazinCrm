using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Products.Entities;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Amount;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.Payments.Enums;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class AmountController : Controller {
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly LogSystemService _logSystemService;
        private readonly IVeiwRenderService vrs;
        private readonly IHubContext<TaskHub> hub;

        public AmountController(UserManager<AppUser> userManager, Service GenService, FileSaver fileSaver,
            DateTimeServices dtService, CRMContextDb db, UserContextDb udb, LogSystemService logSystemService, IVeiwRenderService vrs,
            IHubContext<TaskHub> hub)
        {
            this.userManager = userManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            this.udb = udb;
            _logSystemService = logSystemService;
            this.vrs = vrs;
            this.hub = hub;
        }


        [Authorize(Roles = "مدیر تامین ,کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی, تنخواه دار")]
        public IActionResult RequestAmount()
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
                return BadRequest(e.Message);
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
                if (entity == null) return NotFound();

                await _logSystemService.LogChange(entity, model, LogActionEnum.ویرایش, user, ["RequestAmountSupplyFactor"]);

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
                return BadRequest(e.Message);
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
                await hub.Clients.Groups("مدیر عامل", "مدیر ارشد").SendAsync("countPreFactorToAccept");
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        public async Task<IActionResult> ChangeStatusRequestAmount(Guid id, RequestStatusEnum status)
        {
            if (id == Guid.Empty) return BadRequest();

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                var item = await db.RequestAmounts.Include(f => f.RequestAmountPayments).FirstOrDefaultAsync(f => f.Id == id);
                if (item == null) return NotFound();

                await ChangeRequestAmountStatus(item, status, user);

                await db.SaveChangesAsync();
                await hub.Clients.Groups("مدیر عامل", "مدیر ارشد", "کارشناس مالی").SendAsync("countPreFactorToAccept");
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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
                if (!validation.IsValid) return StatusCode(201, validation.Message);

                var isNewPayment = model.Id == Guid.Empty;
                await AddOrUpdatePayment(model, user, isNewPayment);

                await SyncRequestAmountsPaymentState(model, user, isNewPayment);

                await transaction.CommitAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
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
                await hub.Clients.Groups("مدیر ارشد", "کارشناس مالی").SendAsync("countPreFactorToAccept");
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
                    await db.SaveChangesAsync();

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

                var attachList = fileSaver.saveAttach(model.Attachments, _AttachType.Payment);
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
                if (item == null) return NotFound();

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
                return BadRequest(e.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> GetAllPaymentsByRequestAmount([FromQuery] Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);
                var isFinancial = roles.Contains("کارشناس مالی");

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
                    isFinancial
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        public async Task<IActionResult> GetAllRequestAmountsPartial(int passType, int type = -1, string search = "", Guid? orderHeadId = null)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);
                var isFinancial = roles.Contains("کارشناس مالی");
                var isAdmin = roles.Contains("مدیر ارشد");
                var isManager = roles.Contains("مدیر عامل");

                if (passType == 2 && !isManager && !isAdmin)
                {
                    return NoContent();
                }

                var baseQuery = db.RequestAmounts.OrderByDescending(f => f.CreateDate).AsNoTracking();

                if (!isAdmin && !isFinancial && !isManager)
                {
                    baseQuery = baseQuery.Where(f => f.UserCreatorID == user.Id);
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
                            db.SupplyFactors.Any(sf => sf.Id == raSf.SupplyFactorId && sf.FactorSerial == search)
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


                var data = await query
                    .Select(ra => new RequestAmountVModel {
                        RequestAmount = ra,
                        SupplyFactorSerials = ra.RequestAmountSupplyFactor.Select(x => x.SupplyFactor.FactorSerial).ToList(),
                        OrderHeadSerials = ra.RequestAmountOrderHeads.Select(x => x.OrderHead.OrderNumber).ToList(),
                        CustomerNames = ra.RequestAmountOrderHeads.Select(x => x.OrderHead.CustomerTxt).ToList(),
                        TotalPaymentAmount = ra.RequestAmountPayments.Sum(x => x.Amount)
                    })
                    .ToListAsync();


                var html = await vrs.RenderToStringAsync("GetAllRequestAmountsPartial", data,
                    new Dictionary<string, object> {
                        { "PassType", passType },
                        { "IsFinancial", isFinancial },
                        { "isAdmin", isAdmin },
                        { "UserId", user.Id }
                    }
                );

                return Ok(new { count = data.Count, html, filterCounts });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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
                                (raSf, sf) => sf.FactorSerial
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
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRequestsAmountByFilteredIds([FromBody] GetAllBySelectedVModel model)
        {
            try
            {
                var data = await db.RequestAmounts.AsNoTracking()
                    .Where(f => model.FilterIds.Contains(f.Id))
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestAmountsWithPaidAmountByPayment(Guid paymentId)
        {
            try
            {
                var data = await db.RequestAmountPayments
                    .AsNoTracking()
                    .Where(x => x.PaymentId == paymentId)
                    .Select(x => new {
                        Id = x.RequestAmountId,

                        PaidAmount = x.Amount,

                        x.RequestAmount.RequestNumber,
                        x.RequestAmount.Amount,
                        x.RequestAmount.AcceptedAmount,
                        x.RequestAmount.AcceptedAmountPercent
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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

                if (item == null) return NotFound();

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
                return BadRequest(e.Message);
            }
        }

        public async Task<IActionResult> GetAllRequestAmountsDt()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0");
                var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "کارشناس فروش ,مدیر فروش , مدیر تامین , مدیر ارشد , کارشناس تامین")]
        public async Task<IActionResult> index()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ListProduct(string searchValue, string from = "amountIndex")
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);

                ViewBag.hasAccess = false;
                ViewBag.from = from;
                if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر فروش") || roles.Contains("مدیر تامین") || roles.Contains("کارشناس تامین"))
                {
                    ViewBag.hasAccess = true;
                }

                var pr = await (from mo in db.Products
                    where EF.Functions.Like(mo.Title, "%" + searchValue + "%")
                    select mo).Include(f => f.ProductAmounts).ToListAsync();

                foreach (var item in pr)
                {
                    foreach (var pa in item.ProductAmounts)
                    {
                        if (pa.ModifyDate == null)
                            pa.ElapsTime = dtService.ElapsTime(pa.CreateDate);
                        else
                            pa.ElapsTime = dtService.ElapsTime(pa.ModifyDate.Value);
                    }
                }


                //var products = await db.Products.Include(f=>f.ProductAmounts).ToListAsync();
                return PartialView(pr);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddAmount([FromBody] ProductAmount model)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;

            if (model.Id != Guid.Empty)
            {
                var oldModel = await db.ProductAmounts.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();

                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.ModifyDate = DateTime.Now;
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                db.ProductAmounts.Update(model);

                db.Entry(model).Property(f => f.CreateDate).IsModified = false;
                db.Entry(model).Property(f => f.CreateDateTxt).IsModified = false;
                db.Entry(model).Property(f => f.UserCreator).IsModified = false;
                db.Entry(model).Property(f => f.UserCreatorID).IsModified = false;

                await _logSystemService.LogChange(oldModel, model, LogActionEnum.ویرایش, user);
            } else
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;

                db.ProductAmounts.Add(model);
                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
            }

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> DelAmount(Guid id)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            var pro = await db.ProductAmounts.Where(f => f.Id == id).FirstOrDefaultAsync();
            await _logSystemService.LogChange(pro, null, LogActionEnum.حذف, user);

            pro.IsDeleted = true;
            pro.UserModify = user.FullName;
            pro.UserModifyID = user.Id;
            pro.ModifyDate = DateTime.Now;
            pro.ModifyDateTxt = dtService.GetDateTimePersian();

            await db.SaveChangesAsync();

            return Ok();
        }
    }
}