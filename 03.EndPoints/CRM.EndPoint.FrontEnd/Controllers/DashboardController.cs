using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.EndPoint.FrontEnd.Models.Dashboard;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using CRM.EndPoint.FrontEnd.Security;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Reports.Queries.GetReportPartial;
using CRM.Core.Domain.Reports.Models;
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
        private readonly DocumentAccessPolicy documentAccess;
        private readonly ICqrsDispatcher requests;
        private CancellationToken Ct => HttpContext.RequestAborted;
        private bool Enforced => documentAccess.IsEnforced("Dashboard");
        private Task<bool> Can(string key) => DashboardAccess.CanAsync(documentAccess, User, key, Ct);

        public DashboardController(UserManager<AppUser> userManager, DateTimeServices dtService, CRMContextDb db, UserContextDb udb, DocumentAccessPolicy documentAccess, ICqrsDispatcher requests)
        {
            this.userManager = userManager;
            this.dtService = dtService;
            this.db = db;
            this.udb = udb;
            this.documentAccess = documentAccess;
            this.requests = requests;
        }

        //[Route("Dashboard")]
        [Route("Dashboard/Index")]
        public async Task<IActionResult> Index()
        {
            if (!await Can("View"))
            {
                Response.StatusCode = 403;
                return View("AccessDenied");
            }
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> GetDashboardReports([FromBody] ReportFilterVModel model)
        {
            if (!await Can("ViewReports") || !await documentAccess.HasViewAsync("Report", Ct)) return Forbid();
            if (model == null) return BadRequest();
            var result = await requests.QueryAsync(new GetReportPartialQuery(model.ToReportSearch(), null), Ct);
            return Ok(new { count = result.Count, html = result.Html });
        }

        [HttpPost]
        public async Task<IActionResult> GetFinancialReport([FromBody] FilterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);
            if (!Enforced && !roles.Contains("مدیر ارشد") && !roles.Contains("مدیر مالی") && !roles.Contains("کارشناس مالی") && !roles.Contains("مدیر عامل"))
                return NoContent();

            var showBank = await Can("ViewBankBalance");
            var showReceivables = await Can("ViewReceivables");
            var showPayables = await Can("ViewPayables");
            if (!showBank && !showReceivables && !showPayables) return Forbid();


            // These totals combine ledgers; a partial total would misstate the balance.
            foreach (var entity in new[] { "Accountancy", "Factor", "SupplyFactor", "Payment" })
                if (!await documentAccess.HasAsync(entity, "View", HttpContext.RequestAborted)) return NoContent();

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
                supplyFactorQuery = supplyFactorQuery.ApplyFilter(model, null, x => x.DateMiladi);
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
            var supplyFactorAmount = await supplyFactorQuery.SumAsync(x => x.PaymentAmount);


            if (Enforced)
            {
                var permitted = new Dictionary<string, decimal>();
                if (showBank) permitted["bankBalance"] = bankBalance;
                if (showReceivables) permitted["demands"] = factorAmount - (bankBalance + tahatori);
                if (showPayables) permitted["debit"] = supplyFactorAmount - (paymentAmount + tahatori);
                // Breakdown values can reveal independently denied financial cards.
                return Ok(permitted);
            }

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
        [CatalogPermission("LogSystem")]
        public async Task<IActionResult> GetLogSystem([FromBody] FilterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);
            if (!await Can("ViewLogs")) return Forbid();
            var canDetails = Enforced ? await Can("ViewLogDetails") :
                rols.Contains("مدیر ارشد") && await documentAccess.HasAsync("LogSystem", "ViewDetails", Ct);
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
                    ChangeDetails = canDetails ? f.ChangeDetails : null,
                    CommentTxt = canDetails && f.EntityName == nameof(AppUser) ? f.CommentTxt : null
                });

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> GetInfoFactors([FromBody] FilterVModel model)
        {
            if (!await Can("ViewSalesDetails")) return Forbid();
            var user = await userManager.GetUserAsync(HttpContext.User);
            //var rols = await userManager.GetRolesAsync(user);

            var factorsQ = await documentAccess.FactorsAsync(db.Factors.AsNoTracking().Where(f => f.status == FactorStatus.تایید_شده), "View", HttpContext.RequestAborted);

            if (model?.filt != null)
            {
                factorsQ = factorsQ.ApplyFilter(model, null, f => f.DateMiladi, null);
            }

            if (Enforced)
            {
                factorsQ = await documentAccess.ApplyDashboardOwnersAsync(factorsQ, "ViewSales", "SelerId", Ct);
                factorsQ = await documentAccess.ApplyDashboardOwnersAsync(factorsQ, "ViewSalesDetails", "SelerId", Ct);
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
            if (!await Can("ViewCustomers")) return Forbid();
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);


            var factorsQ = await documentAccess.FactorsAsync(db.Factors.AsNoTracking().Where(f => f.status == FactorStatus.تایید_شده), "View", HttpContext.RequestAborted);

            if (model?.filt != null)
            {
                factorsQ = factorsQ.ApplyFilter(model, f => f.CustomerId, f => f.DateMiladi, f => f.SelerId);
            }


            if (!Enforced && !rols.Contains("مدیر ارشد") && !rols.Contains("مدیر فروش") && !rols.Contains("کارشناس مالی") && !rols.Contains("مدیر مالی"))
            {
                factorsQ = factorsQ.Where(f => f.SelerId == user.Id);
            }

            factorsQ = await documentAccess.ApplyDashboardOwnersAsync(factorsQ, "ViewCustomers", "SelerId", Ct);
            var customerIds = factorsQ.Select(f => f.CustomerId).Distinct();
            var cusQ = await documentAccess.CustomersAsync(db.Customers.AsNoTracking().Where(c => customerIds.Contains(c.Id)), "View", Ct);

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
            if (!await Can("ViewReportCount")) return Forbid();
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);

            var reports = await documentAccess.ApplyAsync(db.Reports.AsNoTracking(), "View", HttpContext.RequestAborted);
            if (documentAccess.IsEnforced("CallRecording"))
            {
                var calls = await documentAccess.ApplyAsync(db.CallRecordings.AsNoTracking(), "View", HttpContext.RequestAborted);
                reports = reports.Where(r => !r.CallRecordingId.HasValue || calls.Any(c => c.Id == r.CallRecordingId.Value));
            }
            if (model?.filt != null)
            {
                reports = reports.ApplyFilter(model, f => f.CustomerId, f => f.CreateDate, f => f.UserCreatorID);
            }


            if (!Enforced && !(rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش") || rols.Contains("کارشناس مالی") || rols.Contains("مدیر مالی")))
            {
                reports = reports.Where(f => f.UserCreatorID == user.Id);
            }

            reports = await documentAccess.ApplyDashboardOwnersAsync(reports, "ViewReportCount", "UserCreatorID", Ct);
            var data = await reports.CountAsync(Ct);

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> GetOrderCount([FromBody] FilterVModel _filter)
        {
            if (!await Can("ViewOrders")) return Forbid();
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);
            IQueryable<OrderHead> orderHeadQ;

            if (Enforced || rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش") || rols.Contains("کارشناس مالی") || rols.Contains("مدیر مالی"))
            {
                if (_filter?.filt != null && _filter.filt.SelFilterChd)
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


            orderHeadQ = await documentAccess.ApplyAsync(orderHeadQ.AsNoTracking(), "View", Ct);
            orderHeadQ = await documentAccess.ApplyDashboardOwnersAsync(orderHeadQ, "ViewOrders", "UserCreatorID", Ct);
            if (_filter?.filt != null)
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
            if (!await Can("ViewSalesDetails")) return Forbid();
            if (Enforced)
            {
                var visible = await documentAccess.FactorsAsync(db.Factors.AsNoTracking(), "View", Ct);
                visible = await documentAccess.ApplyDashboardOwnersAsync(visible, "ViewSales", "SelerId", Ct);
                visible = await documentAccess.ApplyDashboardOwnersAsync(visible, "ViewSalesDetails", "SelerId", Ct);
                var ids = await visible.Select(f => f.SelerId).Distinct().ToListAsync(Ct);
                return Ok(await udb.Users.AsNoTracking().Where(u => ids.Contains(u.Id))
                    .Select(u => new { u.FirstName, u.LastName, u.FullName, u.Id }).ToListAsync(Ct));
            }
            var ss = await userManager.GetUsersInRoleAsync("کارشناس فروش");
            return Ok(ss.Select(f => new { f.FirstName, f.LastName, f.FullName, f.Id }).ToList());
        }


        [HttpPost]
        public async Task<IActionResult> GetDashFactors([FromBody] FilterVModel filter)
        {
            if (!await Can("ViewSales")) return Forbid();
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var factorsQ = await documentAccess.FactorsAsync(db.Factors.AsNoTracking().Where(f => f.status == FactorStatus.تایید_شده), "View", HttpContext.RequestAborted);

            if (!Enforced && !roles.Contains("کارشناس مالی") && !roles.Contains("مدیر مالی") && !roles.Contains("مدیر ارشد") && !roles.Contains("مدیر فروش"))
                factorsQ = factorsQ.Where(f => f.SelerId == user.Id);

            if (filter?.filt != null)
            {
                factorsQ = factorsQ.ApplyFilter(filter, f => f.CustomerId, f => f.DateMiladi, f => f.SelerId);
            }

            factorsQ = await documentAccess.ApplyDashboardOwnersAsync(factorsQ, "ViewSales", "SelerId", Ct);
            var factors = await factorsQ.ToListAsync(Ct);


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
            if (!await Can("ViewCustomers")) return Forbid();
            // The old combined partial has no independent card-level rendering.
            // Require full dashboard grants for both aggregates; source record scopes still apply.
            if (Enforced && (!await documentAccess.HasAsync("Dashboard", "ViewCustomers", Ct) ||
                !await documentAccess.HasAsync("Dashboard", "ViewSales", Ct) ||
                !await documentAccess.HasViewAsync("PreFactor", Ct))) return Forbid();
            if (_filter == null) return BadRequest();
            var visibleCustomers = await documentAccess.CustomersAsync(db.Customers.AsNoTracking(), "View", Ct);
            IList<Customer> data;
            IList<PreFactor> allPf;
            decimal SumPayment;
            var visiblePreFactors = await documentAccess.PreFactorsAsync(db.PreFactors.AsNoTracking(), "View", HttpContext.RequestAborted);

            if (_filter.FromDate == null || _filter.ToDate == null)
            {
                data = await visibleCustomers.ToListAsync(Ct);
                allPf = await visiblePreFactors.ToListAsync();
                SumPayment = allPf.Select(f => f.PaymentAmount).ToArray().Sum();
            } else
            {
                var stDate = dtService.GetDateMiladi(_filter.FromDate);
                var enDate = dtService.GetDateMiladi(_filter.ToDate);

                data = await visibleCustomers
                    .Where(f => f.CreateDate >= stDate && f.CreateDate <= enDate).ToListAsync();
                allPf = await visiblePreFactors
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