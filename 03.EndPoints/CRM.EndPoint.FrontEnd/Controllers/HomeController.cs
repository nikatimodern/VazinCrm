using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services;
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
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.DLs.Entites;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.ExternalSoftwares;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Services.Asterisk;
using CRM.EndPoint.FrontEnd.Services.Carizo;
using CRM.EndPoint.FrontEnd.Services.Extensions;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
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
        private readonly Sepidar1Service sepidar1Service;
        private readonly Sepidar2Service sepidar2Service;
        private readonly ISmsIrService smsIrService;

        public HomeController(IWebHostEnvironment appEnvironment,
            IConfiguration conf,
            UserManager<AppUser> userManager, ILogger<HomeController> logger, CRMContextDb db, DateTimeServices dts,
            Sepidar1ContextDb sdb, Service genService, UserContextDb udb, Sepidar1ContextDb S1db, ISmsQueue smsQueue, IssabelService issabelService,
            AiPrompts aiPrompts, ViraAi viraAi, GapgptAi gapgptAi, LogSystemService logSystemService, TaskServices ts, SmsService smsService,
            Sepidar2ContextDb s2db, Sepidar1Service sepidar1Service, Sepidar2Service sepidar2Service)
        {
            this.appEnvironment = appEnvironment;
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
            this.smsService = smsService;
            s2Db = s2db;
            this.sepidar1Service = sepidar1Service;
            this.sepidar2Service = sepidar2Service;
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
                /*var softs = await db.ExternalSoftwares.AsNoTracking().ToListAsync();
                var baseCenters = await db.BaseCenterCashes.AsNoTracking().ToListAsync();

                var mappings = new List<ExternalMapping>();

                foreach (var soft in softs)
                {
                    if (soft.Code == 1)
                    {
                        var dls = await S1db.DLs.AsNoTracking()
                            .Where(f => f.Type == 3)
                            .ToListAsync();

                        foreach (var center in baseCenters)
                        {
                            var dl = dls.FirstOrDefault(f => f.Code == center.Code);

                            if (dl != null)
                            {
                                mappings.Add(new ExternalMapping {
                                    EntityName = nameof(BaseCenterCash),
                                    EntityId = center.Id,
                                    MapType = ExternalMappingTypeEnum.مرکز_هزینه,
                                    MapTypeTxt = nameof(ExternalMappingTypeEnum.مرکز_هزینه),
                                    ExternalSoftwareId = soft.Id,
                                    ExEntityName = nameof(DL),
                                    ExEntityId = dl.DLId.ToString()
                                });
                            }
                        }
                    } else
                    {
                        var dls = await s2Db.DLs.AsNoTracking()
                            .Where(f => f.Type == 3)
                            .ToListAsync();

                        foreach (var center in baseCenters)
                        {
                            var code = center.Code.StartsWith('0') ? center.Code[1..] : center.Code;
                            var dl = dls.FirstOrDefault(f => f.Code == code);

                            if (dl != null)
                            {
                                mappings.Add(new ExternalMapping {
                                    EntityName = nameof(BaseCenterCash),
                                    EntityId = center.Id,
                                    MapType = ExternalMappingTypeEnum.مرکز_هزینه,
                                    MapTypeTxt = nameof(ExternalMappingTypeEnum.مرکز_هزینه),
                                    ExternalSoftwareId = soft.Id,
                                    ExEntityName = nameof(DL),
                                    ExEntityId = dl.DLId.ToString()
                                });
                            }
                        }
                    }
                }

                db.ExternalMappings.AddRange(mappings);
                await db.SaveChangesAsync();
*/


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
        public async Task<IActionResult> GetAllSystemWarnings(bool isRefresh = false)
        {
            var warnings = await genService.GetAllWarnings(User, isRefresh);

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
                    return BadRequest("مقداری برای حذف یافت نشد");


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


        [HttpPost]
        public async Task<IActionResult> GetAllExternalSoftware([FromBody] ExternalSoftwareRequestDto request)
        {
            if (request.EntityIds.Count == 0 || request.EntityIds.Any(id => id == Guid.Empty))
                return BadRequest();

            var entityName = request.MapType switch {
                ExternalMappingTypeEnum.تنخواه => nameof(Cash),
                ExternalMappingTypeEnum.پیش_فاکتور => nameof(PreFactor),
                _ => null
            };

            if (entityName == null)
                return BadRequest();

            var entityIds = request.EntityIds.Distinct().ToList();

            await ValidateExternalMappingsAsync(entityIds, entityName, request.MapType);

            var softwares = await db.ExternalSoftwares
                .AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => new {
                    x.Id,
                    x.Title,
                    IsDisabled = entityIds.All(entityId =>
                        db.ExternalMappings.Any(m =>
                            m.EntityName == entityName &&
                            m.EntityId == entityId &&
                            m.ExternalSoftwareId == x.Id &&
                            m.MapType == request.MapType))
                })
                .ToListAsync();

            return Ok(softwares);
        }

        private async Task ValidateExternalMappingsAsync(List<Guid> entityIds, string entityName, ExternalMappingTypeEnum mapType)
        {
            var mappings = await db.ExternalMappings
                .Where(x =>
                    entityIds.Contains(x.EntityId) &&
                    x.EntityName == entityName &&
                    x.MapType == mapType)
                .ToListAsync();


            var hasChanges = false;
            foreach (var mapping in mappings)
            {
                var exists = await CheckExternalEntityExistsAsync(mapping.ExternalSoftwareId, mapping.ExEntityId, mapping.ExEntityName);

                if (exists == false)
                {
                    db.ExternalMappings.Remove(mapping);
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await db.SaveChangesAsync();
        }

        private async Task<bool?> CheckExternalEntityExistsAsync(Guid externalSoftwareId, string exEntityId, string exEntityName)
        {
            var service = GetSepidarService(externalSoftwareId);

            if (service == null)
                return null;

            return await service.EntityExistsAsync(exEntityName, exEntityId);
        }

        private static readonly Guid Sepidar1SoftwareId = Guid.Parse("2f3d8024-6c2c-491f-c921-08df26b00138");
        private static readonly Guid Sepidar2SoftwareId = Guid.Parse("d6c3e8a7-1435-47d5-0489-08df26c5e2ca");

        private SepidarService GetSepidarService(Guid softwareId)
        {
            if (softwareId == Sepidar1SoftwareId)
                return sepidar1Service;

            if (softwareId == Sepidar2SoftwareId)
                return sepidar2Service;

            return null;
        }
    }
}