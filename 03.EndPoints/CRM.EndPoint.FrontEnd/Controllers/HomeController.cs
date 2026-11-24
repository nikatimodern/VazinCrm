using CRM.EndPoint.FrontEnd.Security;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CRM.Core.Domain.SystemWarnings.Enums;
using CRM.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using CRM.Core.Domain.Cashes.Entities;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.Carizo;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.ExternalMappings;
using CRM.EndPoint.FrontEnd.Services.Sms;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class HomeController : Controller {
        private readonly IWebHostEnvironment appEnvironment;
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
        private readonly SmsService smsService;
        private readonly Sepidar2ContextDb s2Db;
        private readonly ExternalTransferOptionsService externalTransferOptions;
        private readonly ISmsIrService smsIrService;
        private readonly DocumentAccessPolicy documentAccess;

        public HomeController(IWebHostEnvironment appEnvironment,
            IConfiguration conf,
            UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db, DateTimeServices dts,
            Sepidar1ContextDb sdb, Service genService, UserContextDb udb, Sepidar1ContextDb S1db, ISmsQueue smsQueue, IssabelService issabelService,
            AiPrompts aiPrompts, ViraAi viraAi, GapgptAi gapgptAi, LogSystemService logSystemService, TaskServices ts, SmsService smsService,
            Sepidar2ContextDb s2db, ExternalTransferOptionsService externalTransferOptions, DocumentAccessPolicy documentAccess)
        {
            this.appEnvironment = appEnvironment;
            this.conf = conf;
            this.documentAccess = documentAccess;
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
            this.smsService = smsService;
            s2Db = s2db;
            this.externalTransferOptions = externalTransferOptions;
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
            try
            {

                ViewBag.SubTitle = "سامانه مدیریت مشتری";
                return View();
            }
            catch (Exception e)
            {
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
        public async Task<IActionResult> GetEntityLogSystem(string entityName, string entityId)
        {
            try
            {
                var ct = HttpContext.RequestAborted;
                var enforced = documentAccess.IsEnforced("LogSystem");
                if (enforced && (!await documentAccess.HasAsync("LogSystem", "View", ct) ||
                    !await AuditLogAccess.CanReadRecordAsync(documentAccess, entityName, entityId, ct))) return Forbid();

                var details = enforced ? await documentAccess.HasAsync("LogSystem", "ViewDetails", ct) : User.IsAdmin();
                var logs = await logSystemService.GetEntityLogSystemAsync(entityName, entityId, details);
                if (!enforced) return Ok(logs);
                var visible = new List<CRM.Core.Domain.LogSystems.Entities.LogSystem>();
                foreach (var log in logs)
                    if (await AuditLogAccess.CanReadAsync(documentAccess, log, ct)) visible.Add(log);
                return Ok(visible.Select(log => new { log.Id, log.EntityName, log.EntityId, log.EntitySerialNumber,
                    log.ParentEntityId, log.ParentEntityName, log.ChangeTime, log.ChangeTimeTxt, log.ChangedBy,
                    log.ChangedById, log.ActionType, log.ActionTypeTxt, log.Description,
                    ChangeDetails = details ? log.ChangeDetails : null, CommentTxt = details ? log.CommentTxt : null }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }


        /* ----------------------------------------------- System Warnings ----------------------------------------------- */

        [HttpGet]
        [LegacyCatalogRoles("SystemWarning", "مدیر ارشد,مدیر مالی,کارشناس مالی")]
        [CatalogPermission("SystemWarning", "View")]
        public async Task<IActionResult> GetAllSystemWarnings(bool isRefresh = false)
        {
            if (isRefresh) return BadRequest();
            return Ok(await genService.GetAllWarnings(User));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [LegacyCatalogRoles("SystemWarning", "مدیر ارشد,مدیر مالی,کارشناس مالی")]
        [CatalogPermission("SystemWarning", "Refresh")]
        public async Task<IActionResult> RefreshSystemWarnings()
        {
            return Ok(await genService.GetAllWarnings(User, true));
        }

        [HttpPost, ValidateAntiForgeryToken]
        [LegacyCatalogRoles("SystemWarning", "مدیر ارشد,مدیر مالی,کارشناس مالی")]
        [CatalogPermission("SystemWarning", "Delete")]
        public async Task<IActionResult> DeleteSystemWarning(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var item = await db.SystemWarnings.Where(f => f.Id == id).FirstOrDefaultAsync();
                if (item == null || !await genService.CanReadWarningAsync(item, User, HttpContext.RequestAborted))
                    return NotFound();


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


        /* ----------------------------------------------- External Softwares ----------------------------------------------- */

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GetAllExternalSoftware([FromBody] ExternalSoftwareRequestDto request, CancellationToken ct)
        {
            if (request?.EntityIds == null || request.EntityIds.Count == 0 ||
                request.EntityIds.Any(id => id == Guid.Empty) ||
                !externalTransferOptions.TryGetRule(request.MapType, out _))
                return BadRequest();

            try
            {
                return Ok(await externalTransferOptions.GetAllExternalSoftwareAsync(
                    request.EntityIds, request.MapType, User, ct));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (System.Data.Common.DbException)
            {
                return StatusCode(503, "بررسی وضعیت مقصدهای مالی انجام نشد.");
            }
            catch (DbUpdateException)
            {
                return StatusCode(503, "بررسی وضعیت مقصدهای مالی انجام نشد.");
            }
        }
    }
}
