using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.EndPoint.FrontEnd.Models.Dashboard;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Orders.Enums;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class DashboardController : Controller {
        private readonly UserManager<AppUser> userManager;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;

        public DashboardController(UserManager<AppUser> userManager, DateTimeServices dtService, CRMContextDb db, UserContextDb udb)
        {
            this.userManager = userManager;
            this.dtService = dtService;
            this.db = db;
            this.udb = udb;
        }

        //[Route("Dashboard")]
        [Route("Dashboard/Index")]
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> GetFinancialReport([FromBody] FilterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی") && !roles.Contains("مدیر عامل"))
                return NoContent();


            var hasFilter = model?.filt != null;

            var accountancyQuery = db.Accountancies
                .AsNoTracking()
                .ForPreFactor()
                .Where(a =>
                    a.Status == AccStatusEnum.تایید_شده &&
                    (a.PayType == PayTypeEnum.نقدی || a.PayType == PayTypeEnum.تهاتری)
                );

            var factorQuery = db.Factors.AsNoTracking().Where(f => f.status == FactorStatus.تایید_شده);
            var paymentQuery = db.Payments.AsNoTracking();
            var supplyFactorQuery = db.SupplyFactors.AsNoTracking();

            if (hasFilter)
            {
                accountancyQuery = accountancyQuery.ApplyFilter(model, null, x => x.VosolDateM);
                factorQuery = factorQuery.ApplyFilter(model, null, x => x.DateMiladi);
                paymentQuery = paymentQuery.ApplyFilter(model, null, x => x.PaymentDate);
                supplyFactorQuery = supplyFactorQuery.ApplyFilter(model, null, x => x.FactorDate);
            }

            var accountancies = await accountancyQuery
                .GroupBy(x => x.PayType)
                .Select(g => new {
                    PayType = g.Key,
                    Amount = g.Sum(x => x.AccType == AcEnum.واریزی ? x.credit : -x.BackWard)
                })
                .ToListAsync();

            var bankBalance = accountancies.Where(x => x.PayType == PayTypeEnum.نقدی).Sum(x => x.Amount);
            var tahatori = accountancies.Where(x => x.PayType == PayTypeEnum.تهاتری).Sum(x => x.Amount);

            var factorAmount = await factorQuery.SumAsync(f => f.ParentReturnedId == null ? f.PaymentAmount : -f.PaymentAmount);
            var paymentAmount = await paymentQuery.SumAsync(x => x.Amount);
            var supplyFactorAmount = await supplyFactorQuery.SumAsync(x => x.Amount);


            var data = new {
                bankBalance,
                tahatori,

                factorAmount,
                paymentAmount,
                supplyFactorAmount,

                demands = factorAmount - (bankBalance + tahatori),

                debit = supplyFactorAmount - (paymentAmount + tahatori)
            };
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> GetLogSystem([FromBody] FilterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);
            var logSystem = db.logSystems.Where(f => !string.IsNullOrEmpty(f.Description)).AsQueryable();

            if (model?.filt != null)
            {
                logSystem = logSystem.ApplyFilter(model, null, f => f.ChangeTime, f => f.ChangedById);
            }

            var data = logSystem
                .OrderByDescending(f => f.ChangeTime)
                .Take(100)
                .Select(f => new {
                    f.Description,
                    f.ChangeTimeTxt,
                    f.ActionType,
                    ChangeDetails = rols.Contains("مدیر ارشد") ? f.ChangeDetails : null,
                    CommentTxt = rols.Contains("مدیر ارشد") && f.EntityName == nameof(AppUser) ? f.CommentTxt : null
                });

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> GetInfoFactors([FromBody] FilterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            //var rols = await userManager.GetRolesAsync(user);

            var factorsQ = db.Factors.Where(f => f.status == FactorStatus.تایید_شده).AsQueryable();

            if (model.filt != null)
            {
                factorsQ = factorsQ.ApplyFilter(model, null, f => f.DateMiladi, null);
            }

            var factorsBySeller = await factorsQ
                .GroupBy(f => new { f.SelerId })
                .Select(g => new {
                    SellerId = g.Key.SelerId,
                    SellerName = g.First().SelerFullName,
                    factorCount = g.Count(),
                    TotalAmount = g.Sum(f => f.ParentReturnedId == null ? f.PaymentAmount : -f.PaymentAmount),
                })
                .OrderByDescending(f => f.TotalAmount)
                .ToListAsync();

            var users = await udb.Users.Select(f => new { f.Id, f.PersonImageUrl }).ToListAsync();

            var result = factorsBySeller
                .Select(item => new {
                    item.SellerId,
                    item.SellerName,
                    item.factorCount,
                    item.TotalAmount,
                    UserImage = users.FirstOrDefault(f => f.Id == item.SellerId)?.PersonImageUrl
                })
                .ToList();

            return Ok(result);
        }


        [HttpPost]
        public async Task<IActionResult> getCustomerSuccess([FromBody] FilterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);


            var factorsQ = db.Factors.Where(f => f.status == FactorStatus.تایید_شده).AsQueryable();

            if (model.filt != null)
            {
                factorsQ = factorsQ.ApplyFilter(model, f => f.CustomerId, f => f.DateMiladi, f => f.SelerId);
            }


            if (!rols.Contains("مدیر ارشد") && !rols.Contains("مدیر فروش") && !rols.Contains("کارشناس مالی") && !rols.Contains("مدیر مالی"))
            {
                factorsQ = factorsQ.Where(f => f.SelerId == user.Id);
            }

            var customerIds = await factorsQ.Select(f => f.CustomerId).Distinct().ToListAsync();

            var cusQ = db.Customers.Where(c => customerIds.Contains(c.Id));

            var count = await cusQ.CountAsync();
            var export = await cusQ.Where(c => c.Export).CountAsync();
            var notExport = await cusQ.Where(c => !c.Export).CountAsync();

            return Ok(new {
                Count = count,
                Export = export,
                NotExport = notExport
            });
        }


        [HttpPost]
        public async Task<IActionResult> GetReportsCount([FromBody] FilterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);

            var reports = db.Reports.AsQueryable();
            if (model.filt != null)
            {
                reports = reports.ApplyFilter(model, f => f.CustomerId, f => f.CreateDate, f => f.UserCreatorID);
            }


            if (!(rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش") || rols.Contains("کارشناس مالی") || rols.Contains("مدیر مالی")))
            {
                reports = reports.Where(f => f.UserCreatorID == user.Id);
            }

            var data = reports.Count();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> GetOrderCount([FromBody] FilterVModel _filter)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            var rols = await userManager.GetRolesAsync(user);
            IQueryable<OrderHead> orderHeadQ;

            if (rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش") || rols.Contains("کارشناس مالی") || rols.Contains("مدیر مالی"))
            {
                if (_filter.filt != null && _filter.filt.SelFilterChd)
                {
                    orderHeadQ = db.OrderHeads.Where(f => f.UserCreatorID == _filter.filt.selFilter.ToString()).AsQueryable();
                } else
                {
                    orderHeadQ = db.OrderHeads.AsQueryable();
                }
            } else
            {
                orderHeadQ = db.OrderHeads.Where(f => f.UserCreatorID == user.Id).AsQueryable();
            }


            if (_filter.filt != null)
            {
                orderHeadQ = orderHeadQ.ApplyFilter(_filter, f => f.Customer, f => f.OrderDateMildi, f => f.UserCreatorID);
            }


            var model = new OrderVModelDash {
                InOrder = orderHeadQ.Count(f => f.Status == OrderHeadStatusEnum.ثبت_اولیه),
                InProgress = orderHeadQ.Count(f => f.Status == OrderHeadStatusEnum.تولید),
                Done = orderHeadQ.Count(f => f.Status == OrderHeadStatusEnum.آماده_تحویل),
                Deliver = orderHeadQ.Count(f => f.Status == OrderHeadStatusEnum.تحویل_شده)
            };
            return Ok(model);
        }


        [HttpGet]
        public async Task<IActionResult> GetAllUserForosh()
        {
            var ss = await userManager.GetUsersInRoleAsync("کارشناس فروش");
            return Ok(ss.Select(f => new { f.FirstName, f.LastName, f.FullName, f.Id }).ToList());
        }


        [HttpPost]
        public async Task<IActionResult> GetDashFactors([FromBody] FilterVModel filter)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var factorsQ = db.Factors.Where(f => f.status == FactorStatus.تایید_شده);

            if (!roles.Contains("کارشناس مالی") && !roles.Contains("مدیر مالی") && !roles.Contains("مدیر ارشد") && !roles.Contains("مدیر فروش"))
                factorsQ = factorsQ.Where(f => f.SelerId == user.Id);

            if (filter?.filt != null)
            {
                factorsQ = factorsQ.ApplyFilter(filter, f => f.CustomerId, f => f.DateMiladi, f => f.SelerId);
            }

            var factors = await factorsQ.ToListAsync();


            var model = new DashFactorsVModel {
                Count = factors.Count(f => f.ParentReturnedId == null),
                SumAmount = factors.Sum(f => f.ParentReturnedId == null ? f.PaymentAmount : -f.PaymentAmount),
                Export = factors.Where(f => f.ParentReturnedId == null).Count(f => f.Export == true),
                NoExport = factors.Where(f => f.ParentReturnedId == null).Count(f => f.Export != true)
            };

            return Ok(model);
        }
        

        [HttpPost]
        public async Task<IActionResult> GetDashboardPartial(FilterViewModel _filter)
        {
            IList<Customer> data;
            IList<PreFactor> allPf;
            decimal SumPayment;

            if (_filter.FromDate == null || _filter.ToDate == null)
            {
                data = await db.Customers.Include(f => f.PreFactors).ToListAsync();
                allPf = await db.PreFactors.ToListAsync();
                SumPayment = allPf.Select(f => f.PaymentAmount).ToArray().Sum();
            } else
            {
                var stDate = dtService.GetDateMiladi(_filter.FromDate);
                var enDate = dtService.GetDateMiladi(_filter.ToDate);

                data = await db.Customers.Include(f => f.PreFactors)
                    .Where(f => f.CreateDate >= stDate && f.CreateDate <= enDate).ToListAsync();
                allPf = await db.PreFactors
                    .Where(f => f.DateMiladi >= stDate && f.DateMiladi <= enDate).ToListAsync();
                SumPayment = allPf.Select(f => f.PaymentAmount).ToArray().Sum();
            }


            DashboardViewModel model = new DashboardViewModel() {
                AllCustomers = data.Count,
                CustomerJur = data.Where(f => f.Juridical == 1).ToList().Count,
                CustomerNotJur = data.Where(f => f.Juridical != 1).ToList().Count,
                AllPrefactor = allPf.Count,
                AllPrefactorAmount = allPf.Select(f => f.PaymentAmount).ToArray().Sum(),
                AllPrefactorAccepted = allPf.Where(f => f.Accepted == true).ToList().Count,
                AllPrefactorAcceptedAmount = allPf.Where(f => f.Accepted == true).Select(f => f.PaymentAmount).ToArray().Sum()
            };
            return PartialView(model);
        }
    }
}