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
using CRM.Core.Domain.Payments.Entities;

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


        [Authorize(Roles = "مدیر تامین ,کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
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

                var oldItem = await db.RequestAmounts.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                var item = await db.RequestAmounts.Where(f => f.Id == id).FirstOrDefaultAsync();

                item.RequestStatus = status;
                item.RequestStatusTxt = item.RequestStatus.ToString().Replace("_", " ");

                if (status == RequestStatusEnum.ثبت_اولیه)
                {
                    item.AcceptedAmount = 0;
                    item.AcceptedAmountPercent = null;
                } else if (status == RequestStatusEnum.در_انتظار_پرداخت)
                {
                    var attaches = await db.Attachments.Where(f => f.EntityId == item.Id).ToListAsync();
                    foreach (var attach in attaches)
                        attach?.IsDeleted = true;
                    /*item.FromAccountId = Guid.Empty;
                    item.FromAccountTxt = null;
                    item.ToAccountId = Guid.Empty;
                    item.ToAccountTxt = null;*/
                }

                item.UserModify = user.FullName;
                item.UserModifyID = user.Id;
                item.ModifyDate = DateTime.Now;
                item.ModifyDateTxt = dtService.GetDateTimePersian();

                await _logSystemService.LogChange(oldItem, item, LogActionEnum.تغییر_وضعیت, user);
                await db.SaveChangesAsync();
                await hub.Clients.Groups("مدیر عامل", "مدیر ارشد", "کارشناس مالی").SendAsync("countPreFactorToAccept");
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AttachAndVosolReqAmount([FromBody] Payment model)
        {
            try
            {
                //var reqAmountData = model.RequestAmountData;
                //var paymentData = model.PaymentData;

                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldReqAmount = await db.RequestAmounts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.RequestAmountId);
                var reqAmount = await db.RequestAmounts.FirstOrDefaultAsync(f => f.Id == model.RequestAmountId);
                if (reqAmount == null) return NotFound();

                reqAmount.RequestStatus = RequestStatusEnum.پرداخت_شده;
                reqAmount.RequestStatusTxt = nameof(RequestStatusEnum.پرداخت_شده).Replace("_", " ");
                reqAmount.UserModify = user.FullName;
                reqAmount.UserModifyID = user.Id;
                reqAmount.ModifyDate = DateTime.Now;
                reqAmount.ModifyDateTxt = dtService.GetDateTimePersian();

                db.Payments.Update(model);


                var attachList = fileSaver.saveAttach(model.Attachments, _AttachType.Payment, false);
                foreach (var att in attachList ?? [])
                {
                    att?.EntityId = model.Id;
                }
                db.Attachments.UpdateRange(attachList);

                await _logSystemService.LogChange(oldReqAmount, reqAmount, LogActionEnum.تغییر_وضعیت, user);
                await db.SaveChangesAsync();
                await hub.Clients.Groups("مدیر ارشد", "کارشناس مالی").SendAsync("countPreFactorToAccept");
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        public async Task<IActionResult> GetAllRequestAmountsPartial(int passType, int type = -1, string search = "")
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);
                var isFinancial = roles.Contains("کارشناس مالی");

                var query = db.RequestAmounts.AsNoTracking();


                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        f.RequestNumber == TextService.convertToInt(search) ||
                        f.Description.Contains(search) ||
                        f.SupplierTxt.Contains(search) ||
                        f.RequestAmountSupplyFactor.Any(raSf =>
                            db.SupplyFactors.Any(sf => sf.Id == raSf.SupplyFactorId && sf.FactorSerial == search)
                        ) ||
                        f.RequestAmountOrderHeads.Any(raOh =>
                            db.OrderHeads.Any(oh => oh.Id == raOh.OrderHeadId && oh.OrderNumber.ToString() == search)
                        )
                    );
                }


                query = type switch {
                    1 => query.Where(f => // در انتظار تایید
                        f.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل ||
                        f.RequestStatus == RequestStatusEnum.در_انتظار_ارسال_به_مالی
                    ),
                    2 => query.Where(f => // در انتظار پرداخت
                        f.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت
                    ),
                    3 => query.Where(f => // پرداخت شده
                        f.RequestStatus == RequestStatusEnum.پرداخت_شده
                    ),
                    _ => query
                };


                var res = await query.OrderByDescending(f => f.CreateDate).ToListAsync();


                var ids = res.Select(x => x.Id).ToList();

                var supplySerials = await db.RequestAmountSupplyFactors.AsNoTracking()
                    .Where(x => ids.Contains(x.RequestAmountId))
                    .Join(db.SupplyFactors,
                        x => x.SupplyFactorId,
                        sf => sf.Id,
                        (x, sf) => new { x.RequestAmountId, sf.FactorSerial }
                    )
                    .ToListAsync();

                var orderSerials = await db.RequestAmountOrderHeads.AsNoTracking()
                    .Where(x => ids.Contains(x.RequestAmountId))
                    .Join(db.OrderHeads,
                        x => x.OrderHeadId,
                        oh => oh.Id,
                        (x, oh) => new { x.RequestAmountId, oh.OrderNumber, oh.CustomerTxt }
                    )
                    .ToListAsync();


                var data = res.Select(ra => new RequestAmountVModel {
                    RequestAmount = ra,
                    SupplyFactorSerials = supplySerials.Where(x => x.RequestAmountId == ra.Id).Select(x => x.FactorSerial).ToList(),
                    OrderHeadSerials = orderSerials.Where(x => x.RequestAmountId == ra.Id).Select(x => x.OrderNumber).ToList(),
                    CustomerNames = orderSerials.Where(x => x.RequestAmountId == ra.Id).Select(x => x.CustomerTxt).ToList(),
                }).ToList();


                var html = await vrs.RenderToStringAsync("GetAllRequestAmountsPartial", data,
                    new Dictionary<string, object> {
                        { "PassType", passType },
                        { "IsFinancial", isFinancial },
                        { "UserId", user.Id }
                    }
                );

                return Ok(new { count = data.Count, html });
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


        public async Task<IActionResult> DeleteRequestAmount(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var item = await db.RequestAmounts.Where(f => f.Id == id).FirstOrDefaultAsync();
                if (item == null) return NotFound();

                item.IsDeleted = true;
                item.ModifyDate = DateTime.Now;
                item.ModifyDateTxt = dtService.GetDateTimePersian();
                item.UserModify = user.FullName;
                item.UserModifyID = user.Id;

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