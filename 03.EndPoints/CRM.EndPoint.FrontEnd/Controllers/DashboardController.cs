using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.EndPoint.FrontEnd.Models.Dashboard;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using CRM.Infrastructures.Sql.Factors.QueryExtensions;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Policy = "NotCustomerOnly")]
    public class DashboardController : Controller {
        private readonly Sepidar1ContextDb s1Db;
        private readonly Sepidar2ContextDb s2Db;
        private readonly UserManager<AppUser> userManager;
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;

        public DashboardController(Sepidar1ContextDb s1db, Sepidar2ContextDb s2db, UserManager<AppUser> userManager, Service GenService,
            FileSaver fileSaver, DateTimeServices dtService, CRMContextDb db, UserContextDb udb)
        {
            s1Db = s1db;
            s2Db = s2db;
            this.userManager = userManager;
            genService = GenService;
            this.fileSaver = fileSaver;
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
        public async Task<IActionResult> GetLogSystem([FromBody] filterVModel model)
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
        public async Task<IActionResult> GetInfoFactors([FromBody] filterVModel model)
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
        public async Task<IActionResult> getCustomerSuccess([FromBody] filterVModel model)
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
        public async Task<IActionResult> GetReportsCount([FromBody] filterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);

            var reports = db.Reports.AsQueryable();
            if (model.filt != null)
            {
                reports = reports.ApplyFilter(model, f => f.CustomerId, f => f.CreateDate, f => f.UserCreatorID);
                /*var fd = dtService.GetDateMiladi(model.filt.fromDate);
                var td = dtService.GetDateMiladi(model.filt.toDate);
                fd = fd ?? dtService.GetDateMiladi("1000/01/01");
                td = (td ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
                if (model.filt.CusFilterChd == true)
                    reports = reports.Where(f => f.CustomerId == model.filt.customerFilter);
                if (model.filt.DateFilterChk == true)
                    reports = reports.Where(f => f.CreateDate >= fd && f.CreateDate <= td);
                if (model.filt.SelFilterChd == true)
                    reports = reports.Where(f => f.UserCreatorID == model.filt.selFilter.ToString());*/
            }


            if (!(rols.Contains("مدیر ارشد") || rols.Contains("مدیر فروش") || rols.Contains("کارشناس مالی") || rols.Contains("مدیر مالی")))
            {
                reports = reports.Where(f => f.UserCreatorID == user.Id);
            }

            var data = reports.Count();

            return Ok(data);
        }


        [HttpPost]
        public async Task<IActionResult> GetOrderCount([FromBody] filterVModel _filter)
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
        public async Task<IActionResult> GetDashFactors([FromBody] filterVModel filter)
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


        public async Task<IActionResult> getFactorSepidarVazin()
        {
            //var invo = await s1Db.vwInvoice.ToListAsync();

            //foreach (var item in invo)
            //{


            //}


            return Ok();
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