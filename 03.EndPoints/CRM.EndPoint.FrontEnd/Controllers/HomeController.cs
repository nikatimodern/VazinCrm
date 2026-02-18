using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SetInterval;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using WebPush;


namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class HomeController : Controller {
        private readonly IWebHostEnvironment appEnvironment;
        private readonly TimedHostedService hostService;
        private readonly IConfiguration conf;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<HomeController> _logger;
        private readonly CRMContextDb db;
        private readonly DateTimeServices dts;
        private readonly Sepidar1ContextDb sdb;
        private readonly Service genService;
        private readonly UserContextDb udb;
        private readonly Sepidar1ContextDb S1db;

        public HomeController(IWebHostEnvironment appEnvironment, TimedHostedService hostService, IConfiguration conf,
            UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db, DateTimeServices dts,
            Sepidar1ContextDb sdb, Service genService, UserContextDb udb, Sepidar1ContextDb S1db)
        {
            this.appEnvironment = appEnvironment;
            this.hostService = hostService;
            this.conf = conf;
            this.userManager = userManager;
            _logger = logger;
            this.db = db;
            this.dts = dts;
            this.sdb = sdb;
            this.udb = udb;
            this.S1db = S1db;
            this.genService = genService;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> Index()
        {
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {




                await trans.CommitAsync();
                ViewBag.SubTitle = "سامانه مدیریت مشتری";
                return View();
            }
            catch (Exception e)
            {
                await trans.RollbackAsync();
                return BadRequest(e.Message);
            }
        }


        public IActionResult sigR()
        {
            return View();
        }

        public IActionResult notif()
        {
            return View();
        }

        public IActionResult GetDate(int type)
        {
            switch (type)
            {
                case 1: //getCurrentWeek
                    return Ok(dts.GetCurrentWeek());
                case 2: //getCurrentMonth
                    return Ok(dts.GetCurrentMounth());
                case 3: //getCurrentYear
                    return Ok(dts.GetCurrentYear());
                case 4: //getToday_Date
                    return Ok(dts.GetDatePersian());
                case 5: //getToday_DateTime
                    return Ok(dts.GetDateTimePersianWithDayOfWeek());
                case 6: //getTowmarrowDate
                    return Ok(dts.GetTowmarrow());
            }

            return Ok();
        }


        public IActionResult GetTodayDateTime()
        {
            var dt = dts.GetDateTimePersianWithDayOfWeek();
            return Ok(dt);
        }

        public IActionResult GetTodayDate()
        {
            return Ok(dts.GetDatePersian());
        }

        public string GetTomorrowDate()
        {
            return dts.GetTowmarrow();
        }


        [HttpGet]
        public async Task<IActionResult> WarningsSepidarCrmFactors()
        {
            await hostService.CompareSepidarCrmFactors();
            var data = await db.SystemWarnings.ToListAsync();
            return Ok(data);
        }

        public async Task<IActionResult> GetAllSystemWarnings()
        {
            var warnings = await db.SystemWarnings.ToListAsync();
            return Ok(warnings);
        }


        [HttpGet]
        public async Task<IActionResult> DeleteSystemWarning(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var item = await db.SystemWarnings.Where(f => f.Id == id).FirstOrDefaultAsync();
                if (item == null)
                    return BadRequest(new { message = "مقداری برای حذف یافت نشد" });


                item.IsDeleted = true;
                item.ModifyDate = DateTime.Now;
                item.ModifyDateTxt = dts.GetDateTimePersian();
                item.UserModifyID = user.Id;
                item.UserModify = user.FullName;
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}