using CRM.Core.Domain.Supplies.Entities;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Supplies.Enums;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Reports;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Supply;
using CRM.EndPoint.FrontEnd.Services;
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

        public SupplyController(CRMContextDb db, UserManager<AppUser> userManager, DateTimeServices dtService, Service genService,
            LogSystemService logSystemService, FileSaver fileSaver)
        {
            this.db = db;
            this.dtService = dtService;
            this.userManager = userManager;
            this.genService = genService;
            _logSystemService = logSystemService;
            this.fileSaver = fileSaver;
        }

        [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
        public IActionResult SupplyFactor() // فاکتور تامین
        {
            return View();
        }

        [Authorize(Roles = "مدیر ارشد, مدیر اجرایی")]
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


                model.FactorDate = dtService.GetDateMiladi(model.FactorDateTxt) ?? DateTime.Now;
                await db.SupplyFactors.AddAsync(model);

                var attach = fileSaver.saveAttach(model.Attachments, _AttachType.SupplyFactor);
                foreach (var att in attach)
                {
                    att.EntityId = model.Id;
                }

                await db.Attachments.AddRangeAsync(attach);


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

                var attach = fileSaver.saveAttach(model.Attachments, _AttachType.Payment);
                foreach (var att in attach)
                {
                    att.EntityId = model.Id;
                }

                await db.Attachments.AddRangeAsync(attach);


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


        public async Task<IActionResult> DeleteSupplyFactor(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.SupplyFactors.Where(f => f.Id == id).FirstOrDefaultAsync();

            item.IsDeleted = true;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;

            await db.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> GetAllSupplyFactorsPartial()
        {
            var data = await db.SupplyFactors.OrderByDescending(f => f.CreateDate).AsNoTracking()
                .Select(sf => new SupplyFactorVModel {
                    SupplyFactor = sf,
                    OrderSerials = sf.SupplyFactorOrderHeads
                        .Join(db.OrderHeads,
                            sfOh => sfOh.OrderHeadId,
                            oh => oh.Id,
                            (sfOh, oh) => oh.OrderNumber
                        )
                        .ToList()
                })
                .ToListAsync();
            return PartialView(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSupplyFactorsTablePartial(string search = "")
        {
            try
            {
                var query = db.SupplyFactors.AsNoTracking().OrderByDescending(f => f.CreateDate).AsQueryable();
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        f.SupplierTxt.Contains(search) ||
                        f.FactorSerial == search ||
                        f.Amount == TextService.convertToInt(search)
                    );
                }

                var data = await query.ToListAsync();
                return PartialView(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> GetSupplyFactor(Guid id)
        {
            var data = await db.SupplyFactors.AsNoTracking()
                .Where(f => f.Id == id)
                .Include(f => f.SupplyFactorOrderHeads)
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSupplies(Guid pfId)
        {
            var data = await db.Supplies.Where(f => f.PfId == pfId).ToListAsync();

            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> GetSuppliesProfitDt() // بهای تمام شده
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


                /*var suppliesQ = db.Supplies.AsNoTracking()
                    .GroupBy(s => s.PfId)
                    .Select(g => new {
                        PfId = g.Key,
                        TotalSuppliesAmount =
                            g.Where(f => f.SupplyType != SupplyTypeEnum.ضایعات).Sum(f => f.TotalAmount)
                            - g.Where(f => f.SupplyType == SupplyTypeEnum.ضایعات).Sum(f => f.TotalAmount),
//                        SuppliersNameList = g.Where(f => f.SupplierTxt != null).Select(x => x.SupplierTxt).ToList()
                    });*/

                /*var query = from p in db.PreFactors.AsNoTracking()
                    where p.Pftype != (int)PreFactorEnum.فاقد_اعتبار && p.Factors.Any(f => f.status == FactorStatus.تایید_شده)
                    let totalFactors = p.Factors.Where(f => f.status == FactorStatus.تایید_شده).Sum(f => f.SumTotalAmount)
                    let totalSupplies =
                        db.Supplies.Where(s => s.PfId == p.Id && s.SupplyType != SupplyTypeEnum.ضایعات).Sum(s => s.TotalAmount)
                        - db.Supplies.Where(s => s.PfId == p.Id && s.SupplyType == SupplyTypeEnum.ضایعات).Sum(s => s.TotalAmount)
                    let profit = totalSupplies != 0 ? totalFactors - totalSupplies : -1
                    select new SuppliesDtVModel {
                        PfId = p.Id,
                        CustomerId = p.CustomerId,
                        CustomerCode = p.CustomerEntity.CustomerCode,
                        CustomerName = $"{p.Customer ?? ""} {p.AgentName ?? ""}".Trim(),
                        PfDate = p.Date,
                        PfDateM = p.DateMiladi,
                        PfSerialNumber = p.SerialNumber,
                        DealSerialNumber = p.DealSerialNumber,
                        UserCreator = p.UserCreator,
                        UserCreatorId = p.UserCreatorID,
                        UserTaminTxt = p.UserTaminTxt,
                        TotalSuppliesAmount = totalSupplies,
                        TotalFactorsAmount = totalFactors,
                        Profit = profit,
                        ProfitPercent = profit != -1 && totalFactors != 0 ? Math.Round(profit / totalFactors * 100, 2) : null
                    };*/


                /*var query = from deal in db.Deals.AsNoTracking()
                    join pf in db.PreFactors.AsNoTracking()
                        on deal.Id equals pf.DealId
                    where pf.Pftype != (int)PreFactorEnum.فاقد_اعتبار
//                          && pf.Factors.Any(f => f.status == FactorStatus.تایید_شده)

                    group new { deal, pf } by deal.Id into g

                    let totalFactors = g.SelectMany(x => x.pf.Factors).Where(f => f.status == FactorStatus.تایید_شده)
                            .Sum(f => f.SumTotalAmount)
/*

                    let totalSupplies = g.SelectMany(x => db.Supplies.Where(s => s.PfId == x.pf.Id))
                            .Sum(s => s.SupplyType == SupplyTypeEnum.ضایعات ? -s.TotalAmount :  s.TotalAmount)#1#

//                    let profit = totalSupplies != 0 ? totalFactors - totalSupplies : -1

//                    let deal = g.Select(x => x.deal).FirstOrDefault()
                    select new SuppliesDtVModel {
                        DealId = g.Select(x => x.deal).FirstOrDefault().Id,
                        DealTitle = g.Select(x => x.deal).FirstOrDefault().Title,
                        DealDate = g.Select(x => x.deal).FirstOrDefault().CreateDateTxt.Substring(0, 10),
                        DealDateM = g.Select(x => x.deal).FirstOrDefault().CreateDate,
                        DealSerialNumber = g.Select(x => x.deal).FirstOrDefault().SerialNumber,
                        UserCreatorDeal = g.Select(x => x.deal).FirstOrDefault().UserCreator,
                        UserCreatorDealId = g.Select(x => x.deal).FirstOrDefault().UserCreatorID,
                        UserTaminTxt = g.Select(x => x.pf.UserTaminTxt).FirstOrDefault(),
//                        TotalSuppliesAmount = totalSupplies,
                        TotalFactorsAmount = totalFactors,
//                        Profit = profit,
//                        ProfitPercent = profit != -1 && totalFactors != 0 ? Math.Round(profit / totalFactors * 100, 2) : null
                    };*/


                var queryFirst = from deal in db.Deals.AsNoTracking()
                    join pf in db.PreFactors.AsNoTracking()
                        on deal.Id equals pf.DealId
                    where pf.Pftype != (int)PreFactorEnum.فاقد_اعتبار && pf.Factors.Any() //f => f.status == FactorStatus.تایید_شده
                    group new { deal, pf } by deal.Id
                    into g
                    select new {
                        PreFactor = g.Select(f => f.pf).FirstOrDefault(),
                        Deal = g.Select(f => f.deal).FirstOrDefault(),
                        totalFactors = g.Sum(x => x.pf.Factors.Sum(f => f.PaymentAmount)),
                        totalSupplies = db.Supplies
                            .Where(s => g.Select(x => x.pf.Id).Contains(s.PfId))
                            .Sum(s => s.SupplyType == SupplyTypeEnum.ضایعات ? -s.TotalAmount : s.TotalAmount),
                    };


                var query = queryFirst.AsEnumerable().Select(q =>
                    new SuppliesProfitDtVModel {
                        DealId = q.Deal.Id,
                        DealTitle = q.Deal.Title,
                        DealDate = q.Deal.CreateDateTxt?.Substring(0, 10),
                        DealDateM = q.Deal.CreateDate,
                        DealSerialNumber = q.Deal.SerialNumber,
                        UserCreatorDeal = q.Deal.UserCreator,
                        UserCreatorDealId = q.Deal.UserCreatorID,
                        UserTaminTxt = q.PreFactor.UserTaminTxt,
                        TotalSuppliesAmount = q.totalSupplies,
                        TotalFactorsAmount = q.totalFactors,
                        Profit = q.totalSupplies != 0 ? q.totalFactors - q.totalSupplies : -1,
                        ProfitPercent = q.totalSupplies != 0 && q.totalFactors != 0
                            ? Math.Round((q.totalFactors - q.totalSupplies) / q.totalFactors * 100, 2)
                            : null
                    });


                if (model?.filt != null)
                {
                    var fd = dtService.GetDateMiladi(model.filt.fromDate) ?? dtService.GetDateMiladi("1000/01/01");
                    var td = dtService.GetDateMiladi(model.filt.toDate) ?? DateTime.Now;

                    if (model.filt.DateFilterChk)
                        query = query.Where(f => f.DealDateM >= fd && f.DealDateM <= td);
                    if (model.filt.SelFilterChd)
                        query = query.Where(f => f.UserCreatorDealId == model.filt.selFilter.ToString());
                }


                if (!string.IsNullOrEmpty(searchValue))
                {
                    query = query
                        .Where(f =>
//                            f.CustomerName.Contains(searchValue) ||
//                            f.SuppliersNameList.Any(s => s.Contains(searchValue)) ||
//                            f.CustomerCode.ToString() == searchValue ||
//                            f.PfSerialNumber.ToString() == searchValue ||
                            f.DealSerialNumber.ToString() == searchValue ||
                            f.UserCreatorDeal.Contains(searchValue) ||
                            f.UserTaminTxt.Contains(searchValue) ||
                            f.DealTitle.Contains(searchValue)
                        );
                }

                query = model?._type switch {
                    1 => query.Where(f => f.Profit != -1),
                    2 => query.Where(f => f.Profit == -1),
                    _ => query
                };

                var recordsTotal = query.Count();


                if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
                {
                    var isAscending = sortColumnDirection == "asc";
                    query = sortColumn switch {
                        "DealDate" => isAscending
                            ? query.OrderBy(f => f.TotalSuppliesAmount == 0).ThenBy(f => f.DealDateM)
                            : query.OrderBy(f => f.TotalSuppliesAmount == 0).ThenByDescending(f => f.DealDateM),
                        "TotalSuppliesAmount" => isAscending
                            ? query.OrderBy(f => f.TotalSuppliesAmount == 0).ThenBy(f => f.TotalSuppliesAmount)
                            : query.OrderBy(f => f.TotalSuppliesAmount == 0).ThenByDescending(f => f.TotalSuppliesAmount),
                        "TotalFactorsAmount" => isAscending
                            ? query.OrderBy(f => f.TotalFactorsAmount == 0).ThenBy(f => f.TotalFactorsAmount)
                            : query.OrderBy(f => f.TotalFactorsAmount == 0).ThenByDescending(f => f.TotalFactorsAmount),
                        "Profit" => isAscending
                            ? query.OrderBy(f => f.Profit == -1).ThenBy(f => f.Profit)
                            : query.OrderBy(f => f.Profit == -1).ThenByDescending(f => f.Profit),
                        "ProfitPercent" => isAscending
                            ? query.OrderBy(f => f.ProfitPercent == null).ThenBy(f => f.ProfitPercent)
                            : query.OrderBy(f => f.ProfitPercent == null).ThenByDescending(f => f.ProfitPercent),
                        _ => query
                    };
                }

                var totalSuppliesAmountAll = query.Sum(f => f.TotalSuppliesAmount);
                var totalFactorsAmountAll = query.Sum(f => f.TotalFactorsAmount);
                var profitAll = query.Where(f => f.Profit != -1).Sum(f => f.Profit);

                if (length != -1)
                    query = query.Skip(start).Take(length);

                var data = query.ToList();

                return Json(new {
                    draw,
                    recordsTotal,
                    recordsFiltered = recordsTotal,
                    data,
                    totals = new {
                        totalSuppliesAmountAll,
                        totalFactorsAmountAll,
                        profitAll
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSuppliesDemandsDt() // مطالبات
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
                            .Sum(f => f.PaymentAmount)
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
                    var fd = dtService.GetDateMiladi(model.filt.fromDate) ?? dtService.GetDateMiladi("1000/01/01");
                    var td = dtService.GetDateMiladi(model.filt.toDate) ?? DateTime.Now;

                    if (model.filt.DateFilterChk)
                        query = query.Where(f => f.DealDateM >= fd && f.DealDateM <= td);
                    if (model.filt.SelFilterChd)
                        query = query.Where(f => f.UserCreatorDealId == model.filt.selFilter.ToString());
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetRpMotalebat() // مطالبات
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}