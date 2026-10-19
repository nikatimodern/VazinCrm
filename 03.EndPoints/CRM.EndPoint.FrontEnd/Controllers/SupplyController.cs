using CRM.Core.Domain.Supplies.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Supplies.Enums;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Supply;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Infrastructures.Sql;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class SupplyController : Controller {
        private readonly CRMContextDb db;
        private readonly DateTimeServices dtService;
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly LogSystemService _logSystemService;
        private readonly FileSaver fileSaver;
        private readonly AiService aiService;

        public SupplyController(CRMContextDb db, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
            LogSystemService logSystemService, FileSaver fileSaver, AiService aiService)
        {
            this.db = db;
            this.dtService = dtService;
            this.userManager = userManager;
            this.genService = genService;
            _logSystemService = logSystemService;
            this.fileSaver = fileSaver;
            this.aiService = aiService;
        }

        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public IActionResult SupplyFactor() // فاکتور تامین
        {
            return View();
        }

        [Authorize(Roles = "مدیر تامین , کارشناس تامین ,مدیر ارشد, مدیر اجرایی, مدیر عامل, مدیر مالی, کارشناس مالی")]
        public IActionResult Payment() // پرداختی
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
        public IActionResult Profit() // بهای تمام شده
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
        public IActionResult Demand() // مطالبات
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddSupplyFactor([FromBody] SupplyFactor model)
        {
            if (model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                model.DateMiladi = dtService.GetDateMiladi(model.Date) ?? DateTime.Now;

                await db.SupplyFactors.AddAsync(model);

                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.SupplyFactor);
                foreach (var att in attach)
                {
                    att.EntityId = model.Id;
                }

                await db.Attachments.AddRangeAsync(attach);

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
        public async Task<IActionResult> UpdateSupplyFactor([FromBody] SupplyFactor model)
        {
            if (model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.DateMiladi = dtService.GetDateMiladi(model.Date) ?? DateTime.Now;
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                var entity = await db.SupplyFactors
                    .Include(x => x.SupplyFactorOrderHeads)
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (entity == null) return NotFound("رکوردی یافت نشد!");

                await _logSystemService.LogChange(entity, model, LogActionEnum.ویرایش, user, ["SupplyFactorOrderHeads", "Items"]);
                db.Entry(entity).CurrentValues.SetValues(model);

                entity.SupplyFactorOrderHeads = model.SupplyFactorOrderHeads;
                entity.Items = model.Items;


                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.SupplyFactor, null, false);
                foreach (var att in attach.Where(f => f.EntityId == null))
                {
                    att.EntityId = model.Id;
                }

                db.Attachments.UpdateRange(attach);

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
        public async Task<IActionResult> AddPayment([FromBody] Payment model)
        {
            if (model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;


                model.PaymentDate = dtService.GetDateMiladi(model.PaymentDateTxt) ?? DateTime.Now;
                model.PaymentTypeTxt = model.PaymentType.ToString().Replace("_", " ");
                await db.Payments.AddAsync(model);
                await db.SaveChangesAsync();

                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.Payment);
                foreach (var att in attach)
                {
                    att.EntityId = model.Id;
                }

                await db.Attachments.AddRangeAsync(attach);

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
        public async Task<IActionResult> UpdatePayment([FromBody] Payment model)
        {
            if (model == null) return BadRequest();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                model.PaymentDate = dtService.GetDateMiladi(model.PaymentDateTxt) ?? DateTime.Now;
                model.PaymentTypeTxt = model.PaymentType.ToString().Replace("_", " ");

                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;

                var entity = await db.Payments.Include(x => x.PaymentSupplyFactors).FirstOrDefaultAsync(x => x.Id == model.Id);
                if (entity == null) return NotFound("رکوردی یافت نشد!");

                await _logSystemService.LogChange(entity, model, LogActionEnum.ویرایش, user, ["PaymentSupplyFactors"]);

                entity.PaymentSupplyFactors = model.PaymentSupplyFactors;
                db.Entry(entity).CurrentValues.SetValues(model);

                var attach = fileSaver.SaveAttachments(model.Attachments, _AttachType.Payment);
                foreach (var att in attach)
                {
                    att.EntityId = model.Id;
                }

                db.Attachments.UpdateRange(attach);

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


        public async Task<IActionResult> GetPayment(Guid? paymentId)
        {
            try
            {
                var query = db.Payments.AsNoTracking().Where(f => f.Id == paymentId);

                var data = await query
                    .Include(f => f.PaymentSupplyFactors)
                    .AsNoTracking()
                    .Select(p => new {
                        Payment = p,
                        SupplyFactorSerials = p.PaymentSupplyFactors
                            .Join(db.SupplyFactors,
                                pSf => pSf.SupplyFactorId,
                                sf => sf.Id,
                                (pSf, sf) => sf.SerialNumber
                            )
                            .ToList(),
                        Attachments = db.Attachments
                            .Where(a => a.EntityId == p.Id && a._AttachType == _AttachType.Payment)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();
                return Ok(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetAllSupplyPaymentDt()
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

                var query = db.Payments.OrderByDescending(f => f.CreateDate).AsNoTracking();

                if (!string.IsNullOrEmpty(searchValue))
                {
                    var searchNumber = TextService.convertToInt(searchValue);
                    query = query.Where(f =>
                        f.Amount == searchNumber ||
                        f.PaymentNumber == searchNumber ||
                        f.FromAccountTxt.Contains(searchValue) ||
                        f.ToAccountTxt.Contains(searchValue)
                    );
                }

                var recordsTotal = query.Count();


                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    var isAscending = sortColumnDirection == "asc";
                    query = sortColumn switch {
                        "PaymentNumber" => isAscending
                            ? query.OrderBy(f => f.PaymentNumber)
                            : query.OrderByDescending(f => f.PaymentNumber),
                        "Amount" => isAscending
                            ? query.OrderBy(f => f.Amount)
                            : query.OrderByDescending(f => f.Amount),
                        "CreateDateTxt" => isAscending
                            ? query.OrderBy(f => f.CreateDate)
                            : query.OrderByDescending(f => f.CreateDate),
                        "PaymentDateTxt" => isAscending
                            ? query.OrderBy(f => f.PaymentDate)
                            : query.OrderByDescending(f => f.PaymentDate),
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

        public async Task<IActionResult> GetSupplierFinancialSummary(Guid supplierId)
        {
            /*var totalAmount = await db.SupplyFactors
                .AsNoTracking()
                .Where(f => f.SupplierId == supplierId)
                .SumAsync(f => f.Amount);*/


            /*var grouped = await db.SupplyFactors
                .AsNoTracking()
                .Where(f => f.SupplierId == supplierId)
                .GroupBy(f => f.OfficialType)
                .Select(g => new {
                    OfficialType = g.Key,
                    TotalAmount = g.Sum(f => f.Amount)
                })
                .ToListAsync();

            var data = new {
                official = grouped.FirstOrDefault(x => x.OfficialType == 1)?.TotalAmount ?? 0,
                notOfficial = grouped.FirstOrDefault(x => x.OfficialType == 0)?.TotalAmount ?? 0
            };*/

            var summary = await db
                .GetSupplierFinancialSummaries()
                .FirstOrDefaultAsync(x => x.SupplierId == supplierId);

            return Ok(summary);
        }

        public async Task<IActionResult> DeleteSupplyFactor(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.SupplyFactors.Where(f => f.Id == id).FirstOrDefaultAsync();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            await _logSystemService.LogChange(item, null, LogActionEnum.حذف, user);
            await db.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> GetAllSupplyFactorsPartial(string search, Guid? dealId = null)
        {
            try
            {
                var query = db.SupplyFactors.Include(f => f.Deal).AsNoTracking();

                if (dealId != null)
                {
                    query = query.Where(f => f.DealId == dealId);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchNumber = TextService.convertToInt(search);
                    query = query.Where(f =>
                        f.SerialNumber == search ||
                        f.SupplierTxt.Contains(search) ||
                        f.SupplyFactorOrderHeads.Any(sfOh =>
                            db.OrderHeads.Any(oh => oh.Id == sfOh.OrderHeadId && oh.OrderNumber == searchNumber)
                        )
                    );
                }

                var data = await query.OrderByDescending(f => f.CreateDate)
                    .Select(sf => new SupplyFactorVModel {
                        SupplyFactor = sf,
                        OrderSerials = sf.SupplyFactorOrderHeads
                            .Join(db.OrderHeads,
                                sfOh => sfOh.OrderHeadId,
                                oh => oh.Id,
                                (sfOh, oh) => oh.OrderNumber
                            ).ToList()
                    })
                    .ToListAsync();

                return PartialView(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAllSupplyFactorsTablePartial([FromBody] GetAllBySelectedVModel model)
        {
            try
            {
                var search = model.Search;

                var query = db.SupplyFactors
                    .OrderByDescending(f => model.SelectedIds.Contains(f.Id))
                    .ThenByDescending(f => f.CreateDate)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        f.SupplierTxt.Contains(search) ||
                        f.SerialNumber == search ||
                        f.PaymentAmount == TextService.convertToInt(search)
                    );
                } else
                {
                    query = query.Take(50);
                }

                var data = await query.ToListAsync();
                return PartialView(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> GetSupplyFactor(Guid id)
        {
            try
            {
                var data = await db.SupplyFactors.AsNoTracking()
                    .Where(f => f.Id == id)
                    .Include(f => f.SupplyFactorOrderHeads)
                    .Include(f => f.Items)
                    .Select(sf => new {
                        SupplyFactor = sf,
                        OrderSerials = sf.SupplyFactorOrderHeads
                            .Join(db.OrderHeads,
                                sfo => sfo.OrderHeadId,
                                oh => oh.Id,
                                (sfo, oh) => oh.OrderNumber
                            ).ToList(),
                        Attachments = db.Attachments
                            .Where(a => a.EntityId == sf.Id && a._AttachType == _AttachType.SupplyFactor)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                return Ok(data);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        public async Task<IActionResult> GetSupplyBaseData()
        {
            var extraBaseItems = await db.BaseItems
                .Where(f => f.TypeCode == BaseItemEnum.هزینه_اضافی_تولید)
                .Select(f => new {
                    Value = f.Id,
                    Text = f.Title
                }).ToListAsync();

            var suppliers = await db.Customers
                .Where(f => f.RoleType == RoleTypeEnum.تامین_کننده || f.RoleType == RoleTypeEnum.تامین_کننده_مشتری)
                .OrderByDescending(f => f.CreateDate)
                .Select(f => new {
                    Value = f.Id,
                    Text = $"{f.CompanyName ?? ""} {f.AgentName ?? ""} ({f.CustomerCode})".Trim(),
                })
                .ToListAsync();

            var model = new { extraBaseItems, suppliers };
            return Ok(model);
        }

        public async Task<IActionResult> GetGroupedOrdersBySupplier()
        {
            try
            {
                var result = (
                        from o in db.Orders
                        join oh in db.OrderHeads on o.OrderHeadId equals oh.Id
                        where oh.Status != OrderHeadStatusEnum.تحویل_شده && oh.SupplierId != null
                        select new { o, oh }
                    )
                    .AsEnumerable()
                    .GroupBy(x => new { x.oh.SupplierId, x.oh.SupplierTxt })
                    .Select(g => new GroupedOrdersBySupplierVModal {
                        SupplierId = g.Key.SupplierId,
                        SupplierTxt = g.Key.SupplierTxt,
                        Products = g.GroupBy(p => p.o.ProductCode).Select(x => new ProductGroupVModel {
                                Orders = x.Select(z => z.o).ToList(),
                                OrderHead = x.Select(z => z.oh).FirstOrDefault(),
                                SumWeight = x.Sum(f => int.TryParse(f.o.Weight, out var w) ? w : 0),
                                ProductSmallName = x.Select(g => g.o.ProductSmallName).FirstOrDefault(),
                                UnitTxt = x.Select(g => g.o.UnitTxt).FirstOrDefault(),
                            })
                            .ToList()
                    })
                    .ToList();

                /*var product = ordersList.GroupBy(f => f.ProductCode)
                    .Select(g =>
                        new {
                            supplierTxt = groupedOh.FirstOrDefault(f => f.Id == g.Select(x => x.OrderHeadId).FirstOrDefault())?.SupplierTxt,
                            product = g.Select(f => f.ProductSmallName).FirstOrDefault(),
                            weight = g.Sum(f => int.Parse(f.Weight)),
                            weightUnit = g.Select(f => f.UnitTxt).FirstOrDefault(),
                            data = g.ToList()
                        }
                    ).ToList();*/


                return PartialView(result);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddSupply([FromBody] List<Supply> model)
        {
            if (model == null) return BadRequest();

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var setting = await db.ASettings.FirstOrDefaultAsync();
                foreach (var item in model)
                {
                    if (item.Id == Guid.Empty)
                    {
                        item.UserCreator = user.FullName;
                        item.UserCreatorID = user.Id;

                        db.Supplies.Add(item);
                        await _logSystemService.LogChange(item, null, LogActionEnum.ثبت, user);
                    } else
                    {
                        item.UserModify = user.FullName;
                        item.UserModifyID = user.Id;
                        item.ModifyDate = DateTime.Now;
                        item.ModifyDateTxt = dtService.GetDateTimePersian();

                        var oldSupply = await db.Supplies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.Id);
                        await _logSystemService.LogChange(oldSupply, item, LogActionEnum.ویرایش, user);

                        db.Supplies.Update(item);
                    }
                }

                if (setting.SetAmountByRoleType != SetAmountRoleEnum.فروش)
                {
                    var pf = await db.PreFactors.Where(f => f.Id == model.FirstOrDefault().PfId).FirstOrDefaultAsync();
                    if (pf.TaminStep != TaminStepEnum.تایید_شده)
                    {
                        pf.TaminStep = TaminStepEnum.در_انتظار_تایید;
                        pf.TaminStepTxt = nameof(TaminStepEnum.در_انتظار_تایید).Replace("_", " ");
                    }
                }

                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplies(Guid pfId)
        {
            var data = await db.Supplies.Where(f => f.PfId == pfId).ToListAsync();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> GetSuppliesProfitDt()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();

                var start = Math.Max(
                    0,
                    Convert.ToInt32(Request.Form["start"].FirstOrDefault() ?? "0"));

                var length = Convert.ToInt32(
                    Request.Form["length"].FirstOrDefault() ?? "10");

                var model = JsonConvert.DeserializeObject<FilterVModel>(
                    Request.Form["model"].FirstOrDefault());

                var searchValue = Request.Form["search[value]"]
                    .FirstOrDefault()?.Trim().ToLowerInvariant();

                var orderColumnIndex =
                    Request.Form["order[0][column]"].FirstOrDefault();

                var sortColumn = !string.IsNullOrEmpty(orderColumnIndex)
                    ? Request.Form[$"columns[{orderColumnIndex}][name]"]
                        .FirstOrDefault()
                    : null;

                var sortDirection =
                    Request.Form["order[0][dir]"].FirstOrDefault();

                var isAscending = sortDirection == "asc";

                // پیش‌فاکتورهای معتبر دارای حداقل یک فاکتور
                var eligiblePreFactors = db.PreFactors
                    .AsNoTracking()
                    .Where(pf =>
                        pf.Pftype != (int)PreFactorEnum.فاقد_اعتبار &&
                        pf.Factors.Any());

                // مجموع مبالغ فاکتورها به تفکیک معامله
                var factorsByDeal = eligiblePreFactors
                    .SelectMany(pf => pf.Factors.Select(f => new
                    {
                        pf.DealId,
                        f.PaymentAmount
                    }))
                    .GroupBy(x => x.DealId)
                    .Select(g => new
                    {
                        DealId = g.Key,
                        TotalFactorsAmount =
                            g.Sum(x => (decimal?)x.PaymentAmount) ?? 0m
                    });

                // مجموع ملزومات و کسر مبالغ ضایعات
                var suppliesByDeal =
                    from pf in eligiblePreFactors
                    join supply in db.Supplies.AsNoTracking()
                        on pf.Id equals supply.PfId
                    group supply by pf.DealId into g
                    select new
                    {
                        DealId = g.Key,
                        TotalSuppliesAmount =
                            g.Sum(s =>
                                (decimal?)(s.SupplyType == SupplyTypeEnum.ضایعات
                                    ? -s.TotalAmount
                                    : s.TotalAmount)) ?? 0m
                    };

                // اطلاعات نمایشی یک پیش‌فاکتور برای هر معامله
                var preFactorInfo = eligiblePreFactors
                    .GroupBy(pf => pf.DealId)
                    .Select(g => new
                    {
                        DealId = g.Key,
                        UserTaminTxt = g
                            .OrderBy(pf => pf.Id)
                            .Select(pf => pf.UserTaminTxt)
                            .FirstOrDefault()
                    });

                // فیلتر معاملات
                var dealQuery = db.Deals.AsNoTracking();

                if (model?.filt != null)
                {
                    dealQuery = dealQuery.ApplyFilter(
                        model,
                        null,
                        f => f.CreateDate,
                        f => f.UserCreatorID);
                }

                // اتصال اطلاعات و محاسبه مبالغ در SQL Server
                var query =
                    from deal in dealQuery

                    join factors in factorsByDeal
                        on deal.Id equals factors.DealId

                    join pfInfo in preFactorInfo
                        on deal.Id equals pfInfo.DealId

                    join supplies in suppliesByDeal
                        on deal.Id equals supplies.DealId into suppliesGroup

                    from supplies in suppliesGroup.DefaultIfEmpty()

                    let totalFactors = factors.TotalFactorsAmount
                    let totalSupplies = supplies == null
                        ? 0m
                        : supplies.TotalSuppliesAmount

                    let profit = totalFactors - totalSupplies

                    select new SuppliesProfitDtVModel
                    {
                        DealId = deal.Id,
                        DealTitle = deal.Title,

                        DealDate = deal.CreateDateTxt == null
                            ? null
                            : deal.CreateDateTxt.Substring(0, 10),

                        DealDateM = deal.CreateDate,
                        DealSerialNumber = deal.SerialNumber,
                        UserCreatorDeal = deal.UserCreator,
                        UserCreatorDealId = deal.UserCreatorID,
                        UserTaminTxt = pfInfo.UserTaminTxt,

                        TotalSuppliesAmount = totalSupplies,
                        TotalFactorsAmount = totalFactors,

                        Profit = totalSupplies != 0m
                            ? profit
                            : -1m,

                        ProfitPercent =
                            totalSupplies != 0m && totalFactors != 0m
                                ? Math.Round(
                                    profit / totalFactors * 100m, 2)
                                : null
                    };

                // جستجو
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(f =>
                        f.DealSerialNumber.ToString() == searchValue ||
                        (f.UserCreatorDeal != null &&
                            f.UserCreatorDeal.Contains(searchValue)) ||
                        (f.UserTaminTxt != null &&
                            f.UserTaminTxt.Contains(searchValue)) ||
                        (f.DealTitle != null &&
                            f.DealTitle.Contains(searchValue)));
                }

                // فیلتر نوع گزارش
                query = model?._type switch
                {
                    1 => query.Where(f => f.Profit != -1m),
                    2 => query.Where(f => f.Profit == -1m),
                    _ => query
                };

                // تعداد کل نتایج فیلترشده
                var recordsTotal = await query.CountAsync();

                // مجموع‌ها برای تمام نتایج فیلترشده
                var totals = await query
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        TotalSuppliesAmountAll =
                            g.Sum(f =>
                                (decimal?)f.TotalSuppliesAmount) ?? 0m,

                        TotalFactorsAmountAll =
                            g.Sum(f =>
                                (decimal?)f.TotalFactorsAmount) ?? 0m,

                        ProfitAll =
                            g.Where(f => f.Profit != -1m)
                                .Sum(f => (decimal?)f.Profit) ?? 0m
                    })
                    .FirstOrDefaultAsync();

                // مرتب‌سازی
                IOrderedQueryable<SuppliesProfitDtVModel> orderedQuery = null;

                switch (sortColumn)
                {
                    case "DealDate":
                        orderedQuery = isAscending
                            ? query
                                .OrderBy(f => f.TotalSuppliesAmount == 0m)
                                .ThenBy(f => f.DealDateM)
                            : query
                                .OrderBy(f => f.TotalSuppliesAmount == 0m)
                                .ThenByDescending(f => f.DealDateM);
                        break;

                    case "TotalSuppliesAmount":
                        orderedQuery = isAscending
                            ? query
                                .OrderBy(f => f.TotalSuppliesAmount == 0m)
                                .ThenBy(f => f.TotalSuppliesAmount)
                            : query
                                .OrderBy(f => f.TotalSuppliesAmount == 0m)
                                .ThenByDescending(f => f.TotalSuppliesAmount);
                        break;

                    case "TotalFactorsAmount":
                        orderedQuery = isAscending
                            ? query
                                .OrderBy(f => f.TotalFactorsAmount == 0m)
                                .ThenBy(f => f.TotalFactorsAmount)
                            : query
                                .OrderBy(f => f.TotalFactorsAmount == 0m)
                                .ThenByDescending(f => f.TotalFactorsAmount);
                        break;

                    case "Profit":
                        orderedQuery = isAscending
                            ? query
                                .OrderBy(f => f.Profit == -1m)
                                .ThenBy(f => f.Profit)
                            : query
                                .OrderBy(f => f.Profit == -1m)
                                .ThenByDescending(f => f.Profit);
                        break;

                    case "ProfitPercent":
                        orderedQuery = isAscending
                            ? query
                                .OrderBy(f => f.ProfitPercent == null)
                                .ThenBy(f => f.ProfitPercent)
                            : query
                                .OrderBy(f => f.ProfitPercent == null)
                                .ThenByDescending(f => f.ProfitPercent);
                        break;
                }

                // مرتب‌سازی پایدار برای صفحه‌بندی
                IQueryable<SuppliesProfitDtVModel> dataQuery =
                    orderedQuery != null
                        ? orderedQuery.ThenBy(f => f.DealId)
                        : query.OrderBy(f => f.DealId);

                // صفحه‌بندی در دیتابیس
                if (length != -1)
                {
                    dataQuery = dataQuery.Skip(start).Take(length);
                }

                var data = await dataQuery.ToListAsync();

                return Json(new
                {
                    draw,
                    recordsTotal,
                    recordsFiltered = recordsTotal,
                    data,

                    totals = new
                    {
                        totalSuppliesAmountAll =
                            totals?.TotalSuppliesAmountAll ?? 0m,

                        totalFactorsAmountAll =
                            totals?.TotalFactorsAmountAll ?? 0m,

                        profitAll =
                            totals?.ProfitAll ?? 0m
                    }
                });
            }
            catch (Exception ex)
            {
                // در صورت وجود ILogger، خطا را ثبت کن.
                // logger.LogError(ex, "Error in GetSuppliesProfitDt");

                return BadRequest();
            }
        }





        [HttpPost]
        public async Task<IActionResult> GetSuppliesDemandsDt() // مطالبات
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


                var factorQuery = db.PreFactors.AsNoTracking()
                    .Where(pf =>
                        pf.Pftype != (int)PreFactorEnum.فاقد_اعتبار
                        && pf.Pftype != (int)PreFactorEnum.بسته_شده
                        && pf.Factors.Any(f => f.status == FactorStatus.تایید_شده)
                    )
                    .GroupBy(pf => pf.DealId)
                    .Select(g => new {
                        DealId = g.Key,
                        TotalFactorsAmount = g.SelectMany(x => x.Factors)
                            .Where(f => f.status == FactorStatus.تایید_شده)
                            .Sum(f => f.ParentReturnedId == null ? f.PaymentAmount : -f.PaymentAmount)
                    });


                var accQuery = db.Accountancies.AsNoTracking()
                    .Where(acc =>
                        acc.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                        && (acc.AccType == AcEnum.واریزی || acc.AccType == AcEnum.برگشتی)
                        && (acc.PayType == PayTypeEnum.نقدی || acc.PayType == PayTypeEnum.تهاتری)
                        && acc.Status == AccStatusEnum.تایید_شده
                    )
                    .GroupBy(acc => acc.DealId)
                    .Select(g => new {
                        DealId = g.Key,
                        TotalAccAmount = g.Sum(x => x.AccType == AcEnum.برگشتی ? -x.BackWard : x.credit)
                    });


                var query =
                    from deal in db.Deals.AsNoTracking()
                    join f in factorQuery on deal.Id equals f.DealId
                    join acc in accQuery on deal.Id equals acc.DealId
                    select new SuppliesDemandsDtVModel {
                        DealId = deal.Id,
                        DealSerialNumber = deal.SerialNumber,
                        DealDate = deal.CreateDateTxt.Substring(0, 10),
                        DealDateM = deal.CreateDate,
                        CustomerName = deal.CustomerName,
                        UserCreatorDeal = deal.UserCreator,
                        UserCreatorDealId = deal.UserCreatorID,

                        TotalFactorsAmount = f.TotalFactorsAmount,
                        TotalAccAmount = acc.TotalAccAmount ?? 0,
                        Demand = f.TotalFactorsAmount - (acc.TotalAccAmount ?? 0)
                    };


                if (model?.filt != null)
                {
                    query = query.ApplyFilter(model, null, f => f.DealDateM, f => f.UserCreatorDealId);
                    /*var fd = dtService.GetDateMiladi(model.filt.fromDate) ?? dtService.GetDateMiladi("1000/01/01");
                    var td = dtService.GetDateMiladi(model.filt.toDate) ?? DateTime.Now;

                    if (model.filt.DateFilterChk)
                        query = query.Where(f => f.DealDateM >= fd && f.DealDateM <= td);
                    if (model.filt.SelFilterChd)
                        query = query.Where(f => f.UserCreatorDealId == model.filt.selFilter.ToString());*/
                }


                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query
                        .Where(f =>
                            f.DealSerialNumber.ToString() == searchValue ||
                            f.CustomerName.Contains(searchValue)
                        );
                }


                var recordsTotal = query.Count();


                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    var isAscending = sortColumnDirection == "asc";
                    query = sortColumn switch {
                        "DealDate" => isAscending
                            ? query.OrderBy(f => f.DealDateM)
                            : query.OrderByDescending(f => f.DealDateM),
                        "TotalAccAmount" => isAscending
                            ? query.OrderBy(f => f.TotalAccAmount == 0).ThenBy(f => f.TotalAccAmount)
                            : query.OrderBy(f => f.TotalAccAmount == 0).ThenByDescending(f => f.TotalAccAmount),
                        "TotalFactorsAmount" => isAscending
                            ? query.OrderBy(f => f.TotalFactorsAmount == 0).ThenBy(f => f.TotalFactorsAmount)
                            : query.OrderBy(f => f.TotalFactorsAmount == 0).ThenByDescending(f => f.TotalFactorsAmount),
                        "Demand" => isAscending
                            ? query.OrderBy(f => f.Demand)
                            : query.OrderByDescending(f => f.Demand),
                        _ => query
                    };
                }

                var totalAccAmountAll = query.Sum(f => f.TotalAccAmount);
                var totalFactorsAmountAll = query.Sum(f => f.TotalFactorsAmount);
                var demandAll = query.Sum(f => f.Demand);

                if (length != -1)
                    query = query.Skip(start).Take(length);

                var data = query.ToList();

                return Json(new {
                    draw,
                    recordsTotal,
                    recordsFiltered = recordsTotal,
                    data,
                    totals = new {
                        totalAccAmountAll,
                        totalFactorsAmountAll,
                        demandAll
                    }
                });
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRpMotalebat() // مطالبات
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


                await using var con = new SqlConnection(db.Database.GetDbConnection().ConnectionString);
                var result = (await con.QueryAsync<Rp_Motalebat>(
                    "Rp_Motalebat",
                    new { CusCode = (int?)null },
                    commandType: CommandType.StoredProcedure
                )).AsQueryable();


                if (!string.IsNullOrEmpty(searchValue))
                {
                    result = result.Where(f =>
                        f.CustomerCode == searchValue ||
                        f.Customer.Contains(searchValue)
                    );
                }

                var recordsTotal = result.Count();


                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    var isAscending = sortColumnDirection == "asc";
                    result = sortColumn switch {
                        "PreFactorCount" => isAscending
                            ? result.OrderBy(f => f.PreFactorCount)
                            : result.OrderByDescending(f => f.PreFactorCount),
                        "PreFactorSum" => isAscending
                            ? result.OrderBy(f => f.PreFactorSum)
                            : result.OrderByDescending(f => f.PreFactorSum),
                        "FactorCount" => isAscending
                            ? result.OrderBy(f => f.FactorCount)
                            : result.OrderByDescending(f => f.FactorCount),
                        "FactorSum" => isAscending
                            ? result.OrderBy(f => f.FactorSum)
                            : result.OrderByDescending(f => f.FactorSum),
                        "AccCount" => isAscending
                            ? result.OrderBy(f => f.AccCount)
                            : result.OrderByDescending(f => f.AccCount),
                        "AccSum" => isAscending
                            ? result.OrderBy(f => f.AccSum)
                            : result.OrderByDescending(f => f.AccSum),
                        "RetAccBackCount" => isAscending
                            ? result.OrderBy(f => f.RetAccBackCount)
                            : result.OrderByDescending(f => f.RetAccBackCount),
                        "RetAccBackSum" => isAscending
                            ? result.OrderBy(f => f.RetAccBackSum)
                            : result.OrderByDescending(f => f.RetAccBackSum),
                        "DebitSum" => isAscending
                            ? result.OrderBy(f => f.DebitSum)
                            : result.OrderByDescending(f => f.DebitSum),
                        _ => result
                    };
                }


                if (length != -1)
                    result = result.Skip(start).Take(length);

                var data = result.ToList();

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


        [HttpGet]
        public async Task<IActionResult> GetSuppliesDetails(Guid dealId)
        {
            try
            {
                var pfIds = await db.PreFactors.AsNoTracking().Where(f => f.DealId == dealId).Select(f => f.Id).ToListAsync();
                var supplies = await db.Supplies.AsNoTracking().Where(f => pfIds.Contains(f.PfId)).ToListAsync();

                /*var query = from supply in supplies
                    join pfItem in db.PreFactorItems.AsNoTracking()
                        on supply.PfItemId equals pfItem.Id
                    group new { supply, pfItem } by pfItem.Id
                    into g
                    select new*/


                return PartialView(supplies);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }


        public async Task<IActionResult> GetOrderHeadsBySupplier(Guid supplierId)
        {
            var result =
                from s in db.Supplies.Where(f => f.SupplierId == supplierId)
                join pf in db.PreFactors on s.PfId equals pf.Id
                join oh in db.OrderHeads on pf.Id equals oh.Prefactor
                select oh.Id;
            /*select new
            {
                oh.Id,
                oh.OrderNumber
            };*/

            var data = await result.Distinct().ToListAsync();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> ProccessSupplyFactorWithAi([FromBody] Attachment attachment, CancellationToken ct)
        {
            var result = await aiService.ProcessSupplyFactor(attachment, ct);

            if (!result.Success)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }
    }
}