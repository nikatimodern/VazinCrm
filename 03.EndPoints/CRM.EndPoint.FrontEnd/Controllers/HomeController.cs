using CRM.Core.Domain.AppUsers.Entities;
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
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.Carizo;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.Sms;

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
        private readonly ISmsQueue smsQueue;
        private readonly IssabelService issabelService;
        private readonly AiPrompts aiPrompts;
        private readonly ViraAi viraAi;
        private readonly GapgptAi gapgptAi;
        private readonly LogSystemService logSystemService;
        private readonly TaskServices ts;
        private readonly ISmsIrService smsIrService;

        public HomeController(IWebHostEnvironment appEnvironment, TimedHostedService hostService, IConfiguration conf,
            UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db, DateTimeServices dts,
            Sepidar1ContextDb sdb, Service genService, UserContextDb udb, Sepidar1ContextDb S1db, ISmsQueue smsQueue, IssabelService issabelService,
            AiPrompts aiPrompts, ViraAi viraAi, GapgptAi gapgptAi, LogSystemService logSystemService, TaskServices ts, ISmsIrService smsIrService)
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
            this.smsQueue = smsQueue;
            this.logSystemService = logSystemService;
            this.ts = ts;
            this.smsIrService = smsIrService;
            this.issabelService = issabelService;
            this.aiPrompts = aiPrompts;
            this.viraAi = viraAi;
            this.gapgptAi = gapgptAi;
            this.genService = genService;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public async Task<IActionResult> Index()
        {
            await using var trans = await db.Database.BeginTransactionAsync();
            try
            {
//                var bulkSendResult = await smsIrService.SendTextAsync(["09369100273"], "سلام");

                return Ok();
                /*
                var baseCashes = await db.BaseCashes.IgnoreQueryFilters().ToListAsync();
                foreach (var baseCash in baseCashes)
                {
                    if ((int)baseCash.BaseCashType == 2)
                    {
                        baseCash.BaseCashType = BaseCashTypeEnum.تامین;
                    }
                    baseCash.BaseCashTypeTxt = Enum.IsDefined(baseCash.BaseCashType) ? baseCash.BaseCashType.ToString() : "-";
                }
*/


                /*const string path = "/var/spool/asterisk/monitor/2026/07/18/q-0006-9124606687-20260718-145823-1784370449.414873.wav";
                var stream = await issabelService.GetAudioWithRetry(path);
                stream = await issabelService.ConvertToFlac(stream);*/
                //await db.SaveChangesAsync();
                await trans.CommitAsync();
                ViewBag.SubTitle = "سامانه مدیریت مشتری";
                return View();
            }
            catch (Exception e)
            {
                await trans.RollbackAsync();
                return BadRequest();
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
        public async Task<IActionResult> GetEntityLogSystem(string entityName, string entityId)
        {
            try
            {
                var isAdmin = User.IsAdmin();

                var logs = await logSystemService.GetEntityLogSystemAsync(entityName, entityId, isAdmin);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
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
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}