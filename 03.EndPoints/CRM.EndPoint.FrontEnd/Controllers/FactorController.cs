using CRM.Core.Domain;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.Models.Payment;
using CRM.EndPoint.FrontEnd.Models.ViewModel.RemainedPf;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Sepidar;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.SaveChangeCustom;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.EndPoint.FrontEnd.Services.TaskServices;
using CRM.Infrastructures.Sql;
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
using CRM.Core.Domain.BaseItems.Enums;
using CRM.Core.Domain.Supplies.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.Core.Domain.Supplies.Enums;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Accountency;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using CRM.EndPoint.FrontEnd.Services.SmsServices;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;

namespace CRM.EndPoint.FrontEnd.Controllers {
    public class FactorController : Controller {
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly Sepidar1ContextDb S1db;
        private readonly IHubContext<TaskHub> hub;
        private readonly LogSystemService _logSystemService;
        private readonly IPushQueue pushQueue;
        private readonly CrmSaveCahngeAsync crmSaveCahngeAsync;
        private readonly TaskServices ts;
        private readonly SepidarService sps;
        private readonly SmsService smsServ;
        private readonly UserManager<AppUser> userManager;
        private readonly SignInManager<AppUser> signInManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public FactorController(CrmSaveCahngeAsync crmSaveCahngeAsync, TaskServices ts, SepidarService sps, SmsService SmsServ,
            UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager, Service GenService,
            FileSaver fileSaver,
            DateTimeServices dtService, CRMContextDb db, UserContextDb Udb, Sepidar1ContextDb S1db,
            IHubContext<TaskHub> hub, LogSystemService logSystemService, IPushQueue pushQueue)
        {
            this.crmSaveCahngeAsync = crmSaveCahngeAsync;
            this.ts = ts;
            this.sps = sps;
            smsServ = SmsServ;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            udb = Udb;
            this.S1db = S1db;
            this.hub = hub;
            _logSystemService = logSystemService;
            this.pushQueue = pushQueue;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetFactorCount(Guid pfid, string desc)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            List<Guid> pf = null;


            if (userManager.IsInRoleAsync(user, "کارشناس مالی").Result
                || userManager.IsInRoleAsync(user, "مدیر مالی").Result
                || userManager.IsInRoleAsync(user, "مدیر ارشد").Result
                || userManager.IsInRoleAsync(user, "مدیر فروش").Result
               )
            {
                pf = await (from items in db.PreFactors
                    where (items.Pftype == (int)PreFactorEnum.در_انتظار_تولید || items.Pftype == (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد)
                    select items.Id).ToListAsync();
            } else
            {
                pf = await (from items in db.PreFactors
                    where (items.Pftype == (int)PreFactorEnum.در_انتظار_تولید || items.Pftype == (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد) &&
                          items.UserCreatorID == user.Id
                    select items.Id).ToListAsync();
            }

            return Ok(10);
        }

        public async Task UpdateAllCustomerSuccess()
        {
            var customers = await db.Customers.ToListAsync();

            foreach (var customer in customers)
            {
                var isActive = await genService.IsCustomerActive(customer.Id);

                customer.Success = isActive;
            }

            await db.SaveChangesAsync();
        }

        public async Task UpdateAllCustomerExport()
        {
            var customers = await db.Customers.ToListAsync();

            foreach (var customer in customers)
            {
                var isExport = await genService.IsCustomerExport(customer.Id);

                customer.Export = isExport;
            }

            await db.SaveChangesAsync();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AcceptFactor(Guid id, int type)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            var oldFac = await db.Factors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var fac = await db.Factors.Where(f => f.Id == id).FirstOrDefaultAsync();

            switch (type)
            {
                case 1:
                    fac.status = FactorStatus.در_دست_تایید;
                    fac.statusTxt = nameof(FactorStatus.در_دست_تایید).Replace("_", " ");
                    break;
                case 2:
                    if (string.IsNullOrEmpty(fac.SnFinancial))
                        return NoContent();

                    fac.status = FactorStatus.تایید_شده;
                    fac.statusTxt = nameof(FactorStatus.تایید_شده).Replace("_", " ");
                    break;
                case 3:
                    fac.status = FactorStatus.ابطال_شده;
                    fac.statusTxt = nameof(FactorStatus.ابطال_شده).Replace("_", " ");
                    break;
            }

            fac.UserModify = user.FullName;
            fac.UserModifyID = user.Id;
            fac.ModifyDate = DateTime.Now;
            fac.ModifyDateTxt = dtService.GetDateTimePersian();

            await db.SaveChangesAsync();

            if (fac.CustomerId != null)
            {
                var cus = await db.Customers.Where(f => f.Id == fac.CustomerId).FirstOrDefaultAsync();
                cus.Success = await genService.IsCustomerActive(fac.CustomerId);
                cus.Export = await genService.IsCustomerExport(fac.CustomerId);
            }

            var action = fac.status switch {
                FactorStatus.تایید_شده => LogActionEnum.تایید_و_تغییر_وضعیت,
                FactorStatus.ابطال_شده => LogActionEnum.رد_و_تغییر_وضعیت,
                _ => LogActionEnum.تغییر_وضعیت
            };

            await _logSystemService.LogChange(oldFac, fac, action, user);

            await db.SaveChangesAsync();
            return Ok();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AcceptSalesAdmin(Guid id)
        {
            if (id == Guid.Empty) return null;

            var user = await userManager.GetUserAsync(HttpContext.User);

            var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var pf = await db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync();
            var set = await db.ASettings.FirstOrDefaultAsync();

            if (set.AccountancyRequired == true)
            {
                var isAnyAcc = db.Accountancies.Any(f => f.pfid == id && f.AccType == AcEnum.واریزی);
                if (!isAnyAcc) return StatusCode(201, "برای این پیش فاکتور هنوز پرداختی ثبت نشده است!");
            }

            var accS = await db.Accountancies.ForPreFactor(pf.Id).Where(acc => acc.Status != AccStatusEnum.تایید_شده).ToListAsync();
            foreach (var acc in accS)
                acc.Status = AccStatusEnum.در_دست_تایید;

            pf.AcceptedFinancial = null;
            pf.CommentText = null;

            pf.UserModify = user.FullName;
            pf.UserModifyID = user.Id;
            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();

            pf = await genService.AcceptOrRejectPreFactor(pf, 1);

            await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تایید_و_تغییر_وضعیت, user);

            await db.SaveChangesAsync();

            string cusMobile;
            string cusText;

            if (pf.CustomerId != null)
            {
                var cus = await db.Customers.AsNoTracking().Where(f => f.Id == pf.CustomerId).FirstOrDefaultAsync();
                cusMobile = cus.Mobile;
                cusText = $"{cus.CustomerCode} {cus.CompanyName} ({cus.AgentName})".Trim();
            } else
            {
                cusMobile = pf.Cus_Mobile;
                cusText = pf.AgentName;
            }

            await smsServ.SendAcceptedSalesAdminToCustomer(cusMobile, pf.SerialNumber);

            await hub.Clients.User(pf.UserCreatorID).SendAsync("notifInfo",
                $"پیش‌فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusText} توسط <b>{user.FullName}</b> تایید شد و در انتظار تایید مالی قرار گرفت",
                $"/Customer/CustomerProfile?Id={pf.CustomerId}");
            pushQueue.Enqueue(new PushQueueItem {
                UserId = pf.UserCreatorID,
                Title = "تایید پیش‌فاکتور",
                Body = $"پیش‌فاکتور {pf.SerialNumber} توسط {user.FullName} تایید شد.",
                Url = $"/Customer/CustomerProfile?Id={pf.CustomerId}",
                Tag = "pf" + pf.SerialNumber
            });

            await hub.Clients.Group("کارشناس مالی").SendAsync("notifActionRequired",
                $"پیش‌فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusText} توسط <b>{user.FullName}</b> تایید شد و در کارتابل شما قرار گرفت.",
                "/Cartable/Index?tab=TFinancialExpertId");
            pushQueue.Enqueue(new PushQueueItem {
                RoleName = "کارشناس مالی",
                Title = "تایید پیش‌فاکتور",
                Body = $"پیش‌فاکتور {pf.SerialNumber} برای تایید در کارتابل شما قرار گرفت.",
                Url = "/Cartable/Index?tab=TFinancialExpertId",
                Tag = "pf" + pf.SerialNumber
            });

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> NotAcceptSalesAdmin([FromBody] IdDescriptionVModel model)
        {
            if (model == null) return BadRequest();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldPf = await db.PreFactors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
                var pf = await db.PreFactors.Include(f => f.CustomerEntity).FirstOrDefaultAsync(f => f.Id == model.Id);

                if (pf == null) return StatusCode(201, "خطا! پیش فاکتور یافت نشد.");

                pf.CommentText = string.IsNullOrWhiteSpace(pf.CommentText)
                    ? model.Description?.Trim()
                    : pf.CommentText + "\n" + model.Description?.Trim();

                pf.ModifyDate = DateTime.Now;
                pf.ModifyDateTxt = dtService.GetDateTimePersian();
                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;

                pf.AcceptedFinancial = null;
                pf.Accepted = false;
                pf.Signature = null;
                pf.SignatureDes = "";
                var att = await db.Attachments.Where(f => f.EntityId == pf.Id).ToListAsync();
                if (att.Count != 0)
                {
                    foreach (var item in att)
                    {
                        fileSaver.DelFile(item.Url);
                        item.IsDeleted = true;
                    }
                }

                pf = await genService.AcceptOrRejectPreFactor(pf, 2);

                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.رد_و_تغییر_وضعیت, user);
                await db.SaveChangesAsync();
                var cusText = string.IsNullOrWhiteSpace(pf.Customer)
                    ? $"{pf.CustomerCode} ({pf.AgentName})"
                    : $"{pf.CustomerCode} {pf.Customer} ({pf.AgentName})";
                await hub.Clients.User(pf.UserCreatorID).SendAsync("notifInfo",
                    $"پیش فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusText} توسط <b>{user.FullName}</b>، <b style='font-size: 15px; color:darkred'>عدم تایید</b> خورد!",
                    $"/Customer/CustomerProfile?Id={pf.CustomerId}"
                );
                pushQueue.Enqueue(new PushQueueItem {
                    UserId = pf.UserCreatorID,
                    Title = "عدم تایید پیش‌فاکتور",
                    Body = $"پیش فاکتور {pf.SerialNumber} توسط {user.FullName}، عدم تایید خورد!",
                    Url = $"/Customer/CustomerProfile?Id={pf.CustomerId}",
                    Tag = "pf" + pf.SerialNumber
                });
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Backward(Guid id)
        {
            await using var tran = await db.Database.BeginTransactionAsync();
            var user = await userManager.GetUserAsync(HttpContext.User);

            var set = await db.ASettings.FirstOrDefaultAsync();

            var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var pf = await db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync();

            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();
            pf.UserModify = user.FullName;
            pf.UserModifyID = user.Id;

            pf.PrePftype = pf.Pftype;
            pf.Pftype = set.SmallOffice ? (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری : (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین;
            pf.PftypeTxt = Service.GetTypeTxtPfactor(pf.Pftype);

            if (set.SetAmountByRoleType != SetAmountRoleEnum.فروش)
            {
                pf.TaminStep = TaminStepEnum.تعیین_مبلغ_فروش;
                pf.TaminStepTxt = nameof(TaminStepEnum.تعیین_مبلغ_فروش).Replace("_", " ");
            }

            pf.Accepted = false;
            pf.Signature = null;
            pf.SignatureDes = "";
            pf.AcceptedFinancial = null;
            pf.CommentText = null;
            pf.UserTaminId = null;
            pf.UserTaminTxt = null;

            /*if (pf.ValidityDateMiladi < dtService.GetDateMiladi())
            {
                pf.ValidityDateMiladi = dtService.GetDateMiladi();
                pf.ValidityDade = dtService.GetDatePersian();
            }*/

            var att = await db.Attachments.Where(f => f.EntityId == pf.Id).ToListAsync();
            if (att.Count != 0)
            {
                foreach (var item in att)
                {
                    fileSaver.DelFile(item.Url);
                    item.IsDeleted = true;
                }
            }

            /*var accS = await db.Accountancies.Where(f =>
                f.pfid == pf.Id &&
                ((f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور && f.AccType != AcEnum.بدهکاری) || f.InvoiceType == InvoiceTypeEnum.فاکتور)
            ).ToListAsync();
            if (accS.Count != 0)
            {
                foreach (var item in accS)
                {
                    item.IsDeleted = true;
                }
            }*/

            await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user);

            await db.SaveChangesAsync();
            await tran.CommitAsync();
            return Ok();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> getFactor(Guid id)
        {
            var data = await db.Factors
                .Include(f => f.FactorItems)
                .Include(f => f.Descriptions)
                .Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(data);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPartialAllItemFactor(Guid pfId)
        {
            var pFac = await db.PreFactors.Include(f => f.PreFactorItems)
                .Where(f => f.Id == pfId)
                .FirstOrDefaultAsync();

            var Fac = await db.Factors
                .Include(f => f.FactorItems)
                .Where(f => f.PfactorID == pfId && f.status == FactorStatus.تایید_شده)
                .ToListAsync();

            IList<AllItemsViewModel> model = new List<AllItemsViewModel>();

            foreach (var item in pFac.PreFactorItems)
            {
                decimal Sum_WeightCount = 0;
                foreach (var f in Fac)
                {
                    foreach (var fitem in f.FactorItems)
                    {
                        if (fitem.PfItemId == item.Id)
                        {
                            Sum_WeightCount = Sum_WeightCount + decimal.Parse(fitem.WeightCount);
                        }
                    }
                }

                model.Add(new AllItemsViewModel() {
                    PfId = pfId,
                    PfItemid = item.Id,
                    Product = item.ProductSmallName + " " + item.Product,
                    PfWeightCount = item.WeightCount + " " + item.UnitTxt,
                    FWeightCount = Sum_WeightCount.ToString() + " " + item.UnitTxt,
                    Remained = (Int32.Parse(item.WeightCount) - Sum_WeightCount).ToString() + " " + item.UnitTxt,
                    RemainedFlag = (Int32.Parse(item.WeightCount) - Sum_WeightCount) <= 0 ? true : false
                });
            }

            return PartialView(model);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPartialAllItemFactorDetail(Guid pfId, Guid pfItemId)
        {
            var factors = await db.Factors.Include(f => f.FactorItems)
                .Where(f => f.PfactorID == pfId).ToListAsync();


            IList<AllItemsDetailViewModel> model = new List<AllItemsDetailViewModel>();

            foreach (var f in factors)
            {
                foreach (var FItem in f.FactorItems)
                {
                    if (FItem.PfItemId == pfItemId)
                    {
                        model.Add(new AllItemsDetailViewModel() {
                            facId = f.Id,
                            Product = FItem.ProductSmallName + " " + FItem.Product,
                            FserialNumber = f.SerialNumber,
                            Date = f.Date,
                            FWeightCount = FItem.WeightCount + " " + FItem.UnitTxt,
                            Amount = FItem.TotalAmount
                        });
                    }
                }
            }

            return PartialView(model);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCustomerPhone(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            var pf = db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync().Result;
            string cusMobile;
            if (pf.CustomerId != null)
            {
                var cus = await db.Customers.Where(f => f.Id == pf.CustomerId).FirstOrDefaultAsync();
                cusMobile = cus.Mobile?.Split("\n")[0];
            } else
            {
                cusMobile = pf.Cus_Mobile?.Split("\n")[0];
            }

            return Ok(cusMobile);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AcceptTaminExpert(Guid id)
        {
            if (id == Guid.Empty) return null;

            var setting = await db.ASettings.FirstOrDefaultAsync();
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var pf = await db.PreFactors.Where(f => f.Id == id).Include(f => f.CustomerEntity).FirstOrDefaultAsync();

            pf.UserModify = user.FullName;
            pf.UserModifyID = user.Id;
            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();

            pf.CommentText = null;

            if (setting?.SetAmountByRoleType != SetAmountRoleEnum.فروش)
            {
                pf.TaminStep = TaminStepEnum.تایید_شده;
                pf.TaminStepTxt = nameof(TaminStepEnum.تایید_شده).Replace("_", " ");
            }

            pf = await genService.AcceptOrRejectPreFactor(pf, 1);

            await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تایید_و_تغییر_وضعیت, user);
            await db.SaveChangesAsync();

            string cusTxt = pf.CustomerEntity != null
                ? $"{pf.CustomerEntity.CustomerCode} {pf.CustomerEntity.CompanyName} ({pf.CustomerEntity.AgentName})"
                : pf.AgentName;

            await hub.Clients.User(pf.UserCreatorID).SendAsync("notifInfo",
                $"پیش‌فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusTxt} توسط <b>{user.FullName}</b> تایید شد",
                pf.CustomerId != null ? $"/Customer/CustomerProfile?Id={pf.CustomerId}" : ""
            );

            pushQueue.Enqueue(new PushQueueItem {
                UserId = pf.UserCreatorID,
                Title = "تایید پیش‌فاکتور",
                Body = $"پیش‌فاکتور {pf.SerialNumber} توسط {user.FullName} تایید شد",
                Url = pf.CustomerId != null ? $"/Customer/CustomerProfile?Id={pf.CustomerId}" : "/Deal/Index",
                Tag = "pf" + pf.SerialNumber
            });
            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> NotAcceptTaminExpert([FromBody] IdDescriptionVModel model)
        {
            if (model == null) return BadRequest();
            try
            {
                await using var tran = await db.Database.BeginTransactionAsync();
                var user = await userManager.GetUserAsync(HttpContext.User);

                var set = await db.ASettings.FirstOrDefaultAsync();

                var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                var pf = await db.PreFactors.Where(f => f.Id == model.Id).Include(f => f.CustomerEntity).FirstOrDefaultAsync();

                /*pf.PrePftype = pf.Pftype;
                pf.Pftype = (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین;
                pf.PftypeTxt = Service.GetTypeTxtPfactor(pf.Pftype);*/

                if (set.SetAmountByRoleType != SetAmountRoleEnum.فروش)
                {
                    pf.TaminStep = TaminStepEnum.تعیین_مبلغ_فروش;
                    pf.TaminStepTxt = nameof(TaminStepEnum.تعیین_مبلغ_فروش).Replace("_", " ");
                }

                pf.UserTaminId = null;
                pf.UserTaminTxt = null;
                pf.ModifyDate = DateTime.Now;
                pf.ModifyDateTxt = dtService.GetDateTimePersian();
                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;
                pf.CommentText = string.IsNullOrWhiteSpace(pf.CommentText)
                    ? model.Description?.Trim()
                    : pf.CommentText + "\n" + model.Description?.Trim();


                /*var accS = await db.Accountancies.Where(f =>
                    f.pfid == pf.Id &&
                    ((f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور && f.AccType != AcEnum.بدهکاری) || f.InvoiceType == InvoiceTypeEnum.فاکتور)
                ).ToListAsync();
                if (accS.Count != 0)
                {
                    foreach (var item in accS)
                        item.IsDeleted = true;
                }*/

                pf = await genService.AcceptOrRejectPreFactor(pf, 2);

                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.رد_و_تغییر_وضعیت, user);

                await db.SaveChangesAsync();
                await tran.CommitAsync();


                var cusTxt = pf.CustomerEntity != null
                    ? $"{pf.CustomerEntity.CustomerCode} {pf.CustomerEntity.CompanyName} ({pf.CustomerEntity.AgentName})"
                    : pf.AgentName;
                await hub.Clients.User(pf.UserCreatorID).SendAsync("notifInfo",
                    $"پیش فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusTxt} توسط <b>{user.FullName}</b>، <b style='font-size: 15px; color:darkred'>عدم تایید</b> خورد!",
                    pf.CustomerId != null ? $"/Customer/CustomerProfile?Id={pf.CustomerId}" : ""
                );

                pushQueue.Enqueue(new PushQueueItem {
                    UserId = pf.UserCreatorID,
                    Title = "عدم تایید پیش‌فاکتور",
                    Body = $"پیش فاکتور {pf.SerialNumber} توسط {user.FullName}، عدم تایید خورد!",
                    Url = pf.CustomerId != null ? $"/Customer/CustomerProfile?Id={pf.CustomerId}" : "/Deal/Index",
                    Tag = "pf" + pf.SerialNumber
                });
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Authorize]
        public async Task<IActionResult> RejectTaminExpert(Guid id) // رد تایید
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                var pf = await db.PreFactors.Where(f => f.Id == id).Include(f => f.CustomerEntity).FirstOrDefaultAsync();


                pf.PrePftype = pf.Pftype;
                pf.Pftype = (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین;
                pf.PftypeTxt = nameof(PreFactorEnum.در_انتظار_تایید_مسئول_تامین).Replace("_", " ");

                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;
                pf.ModifyDate = DateTime.Now;
                pf.ModifyDateTxt = dtService.GetDateTimePersian();

                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.رد_و_تغییر_وضعیت, user);

                await db.SaveChangesAsync();

                await hub.Clients.Groups("کارشناس تامین", "مدیر ارشد").SendAsync("countPreFactorToAccept");
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AcceptFinancial(Guid id)
        {
            if (id == Guid.Empty) return null;

            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var pf = await db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync();

            var hasNotAcceptedAcc = await db.Accountancies.ForPreFactor(id).AnyAsync(f => f.Status != AccStatusEnum.تایید_شده);
            if (hasNotAcceptedAcc)
                return StatusCode(201, "ابتدا واریزی های این پیش فاکتور را تایید نمایید");


            pf.AcceptedFinancial = true;

            pf.CommentText = null;
            pf.UserModify = user.FullName;
            pf.UserModifyID = user.Id;
            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();

            pf = await genService.AcceptOrRejectPreFactor(pf, 1);

            await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تایید_و_تغییر_وضعیت, user);
            await db.SaveChangesAsync();

            var cusText = string.IsNullOrWhiteSpace(pf.Customer)
                ? $"{pf.CustomerCode} ({pf.AgentName})"
                : $"{pf.CustomerCode} {pf.Customer} ({pf.AgentName})";

            await hub.Clients.User(pf.UserCreatorID).SendAsync("notifInfo",
                $"پیش فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusText} توسط <b>{user.FullName}</b> تایید شد.",
                $"/Customer/CustomerProfile?Id={pf.CustomerId}");

            pushQueue.Enqueue(new PushQueueItem {
                UserId = pf.UserCreatorID,
                Title = "تایید مالی پیش‌فاکتور",
                Body = $"پیش‌فاکتور {pf.SerialNumber} توسط {user.FullName} تایید شد.",
                Url = $"/Customer/CustomerProfile?Id={pf.CustomerId}",
                Tag = "pf" + pf.SerialNumber
            });

            return Ok();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> NotAcceptFinancial([FromBody] IdDescriptionVModel model)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                var pf = await db.PreFactors.Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                pf.AcceptedFinancial = false;

                pf.CommentText = string.IsNullOrWhiteSpace(pf.CommentText)
                    ? model.Description?.Trim()
                    : pf.CommentText + "\n" + model.Description?.Trim();

                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;
                pf.ModifyDate = DateTime.Now;
                pf.ModifyDateTxt = dtService.GetDateTimePersian();

                pf = await genService.AcceptOrRejectPreFactor(pf, 2);

                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.رد_و_تغییر_وضعیت, user);
                await db.SaveChangesAsync();

                var cusText = string.IsNullOrWhiteSpace(pf.Customer)
                    ? $"{pf.CustomerCode} ({pf.AgentName})"
                    : $"{pf.CustomerCode} {pf.Customer} ({pf.AgentName})";
                var msg =
                    $"پیش فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusText} توسط <b>{user.FullName}</b>، <b style='font-size: 15px; color:darkred'>عدم تایید</b> خورد!";

                await hub.Clients.User(pf.UserCreatorID).SendAsync("notifInfo", msg, "/Cartable/Index?tab=TSalesExpertId");
                await hub.Clients.Group("مدیر فروش").SendAsync("notifInfo", msg, "/Cartable/Index?tab=TSalesAdminId");

                pushQueue.Enqueue(new PushQueueItem {
                    UserId = pf.UserCreatorID,
                    Title = "عدم تایید پیش‌فاکتور",
                    Body = $"پیش فاکتور {pf.SerialNumber} توسط {user.FullName}، عدم تایید خورد!",
                    Url = $"/Customer/CustomerProfile?Id={pf.CustomerId}",
                    Tag = "pf" + pf.SerialNumber
                });
                pushQueue.Enqueue(new PushQueueItem {
                    RoleName = "مدیر فروش",
                    Title = "عدم تایید پیش‌فاکتور",
                    Body = $"پیش فاکتور {pf.SerialNumber} توسط {user.FullName}، عدم تایید خورد!",
                    Url = "/Cartable/Index?tab=TSalesAdminId",
                    Tag = "pf" + pf.SerialNumber
                });
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [Authorize]
        public async Task<IActionResult> RejectFinancial(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                var pf = await db.PreFactors.Where(f => f.Id == id).Include(f => f.CustomerEntity).FirstOrDefaultAsync();

                pf.AcceptedFinancial = null;

                pf.PrePftype = pf.Pftype;
                pf.Pftype = (int)PreFactorEnum.در_انتظار_تایید_مالی;
                pf.PftypeTxt = Service.GetTypeTxtPfactor(pf.Pftype);

                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;
                pf.ModifyDate = DateTime.Now;
                pf.ModifyDateTxt = dtService.GetDateTimePersian();

                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.رد_و_تغییر_وضعیت, user);

                await db.SaveChangesAsync();

                await hub.Clients.Groups("کارشناس مالی", "مدیر ارشد").SendAsync("countPreFactorToAccept");
                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [HttpGet]
        public async Task<IActionResult> PreviewFactorSepidarPrint(int id)
        {
            try
            {
                //var factor = await db.Factors
                //                    .Include(f => f.FactorItems)
                //                    .Include(f => f.Descriptions)
                //                    .Where(f => f.Id == id).FirstOrDefaultAsync();
                //var customer = await db.Customers.Where(f => f.Id == factor.CustomerId).FirstOrDefaultAsync();

                var s1 = await S1db.vwInvoice.Include(f => f.InvoiceItems).Where(f => f.InvoiceId == id).FirstOrDefaultAsync();

                if (s1 == null) return NotFound();

                s1.ExtraDescription = sps.getNoteFactor(s1.InvoiceId);

                var myCompany = await db.Companies.Include(f => f.CompanyFiles).FirstOrDefaultAsync();
                SepidarFactorViewModel model = new SepidarFactorViewModel() {
                    invoice = s1,
                    Company = myCompany,
                    Siq = null,
                    Mohr = fileSaver.GetStringImage(myCompany.CompanyFiles.Where(f => f.Filename == "Mohr").FirstOrDefault() == null
                        ? null
                        : myCompany.CompanyFiles.Where(f => f.Filename == "Mohr").FirstOrDefault().file),
                    MohrR = fileSaver.GetStringImage(myCompany.CompanyFiles.Where(f => f.Filename == "MohrR").FirstOrDefault() == null
                        ? null
                        : myCompany.CompanyFiles.Where(f => f.Filename == "MohrR").FirstOrDefault().file),
                    Logo = fileSaver.GetStringImage(myCompany.CompanyFiles.Where(f => f.Filename == "Logo").FirstOrDefault() == null
                        ? null
                        : myCompany.CompanyFiles.Where(f => f.Filename == "Logo").FirstOrDefault().file),
                    priceTxt = TextService.ConvertDecimalToWords(s1.NetPrice)
                };


                return View(model);
            }
            catch
            {
                return BadRequest();
            }
        }


        [HttpGet]
        public async Task<IActionResult> PreviewFactorPrint(Guid id)
        {
            try
            {
                var factor = await db.Factors
                    .Include(f => f.FactorItems)
                    .Include(f => f.Descriptions)
                    .Where(f => f.Id == id).FirstOrDefaultAsync();

                if (factor == null) return NotFound();

                var customer = await db.Customers.Where(f => f.Id == factor.CustomerId).FirstOrDefaultAsync();
                var myCompany = await db.Companies.Include(f => f.CompanyFiles).FirstOrDefaultAsync();
                FactorViewModel model = new FactorViewModel() {
                    Factor = factor,
                    Company = myCompany,
                    _Customer = customer,
                    Siq = fileSaver.GetStringImage(factor.Signature),
                    Mohr = fileSaver.GetStringImage(myCompany.CompanyFiles.FirstOrDefault(f => f.Filename == "Mohr") == null
                        ? null
                        : myCompany.CompanyFiles.FirstOrDefault(f => f.Filename == "Mohr").file),
                    MohrR = fileSaver.GetStringImage(myCompany.CompanyFiles.FirstOrDefault(f => f.Filename == "MohrR") == null
                        ? null
                        : myCompany.CompanyFiles.FirstOrDefault(f => f.Filename == "MohrR").file),
                    Logo = fileSaver.GetStringImage(myCompany.CompanyFiles.FirstOrDefault(f => f.Filename == "Logo") == null
                        ? null
                        : myCompany.CompanyFiles.FirstOrDefault(f => f.Filename == "Logo").file)
                };
                return View(model);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> PreviewPfactorPrint(Guid id)
        {
            try
            {
                var pfactor = await db.PreFactors
                    .IgnoreQueryFilters()
                    .Include(f => f.PreFactorItems.Where(f => f.IsDeleted == false))
                    .Include(f => f.Descriptions.Where(f => f.IsDeleted == false))
                    .Where(f => f.Id == id).FirstOrDefaultAsync();


                if (pfactor == null) return NotFound();


                var set = await db.ASettings.FirstOrDefaultAsync();
                var myCompany = await db.Companies.Include(f => f.CompanyFiles).FirstOrDefaultAsync();

                pfactor.ReciveAmount = await db.Accountancies
                    .Where(f =>
                        f.pfid == pfactor.Id
                        && f.AccType == AcEnum.واریزی
                        && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                        && (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی)
                        && f.Status == AccStatusEnum.تایید_شده
                    ).SumAsync(f => f.credit.Value);

                pfactor.PaymentAmount = pfactor.SumTotalAmount + pfactor.TotalTax - pfactor.Discount - pfactor.ReciveAmount;
                pfactor.SumTotalAmountTax = pfactor.SumTotalAmount + pfactor.TotalTax - pfactor.Discount;

                var model = new PfactorViewModel {
                    PreFactor = pfactor,
                    Company = myCompany,
                    _Setting = set,
                    Siq = fileSaver.GetStringImage(pfactor.Signature),
                    Mohr = fileSaver.GetStringImage(myCompany?.CompanyFiles?.FirstOrDefault(f => f.Filename == "Mohr")?.file),
                    MohrR = fileSaver.GetStringImage(myCompany?.CompanyFiles?.FirstOrDefault(f => f.Filename == "MohrR")?.file),
                    Logo = fileSaver.GetStringImage(myCompany?.CompanyFiles?.FirstOrDefault(f => f.Filename == "Logo")?.file),
                };

                return View(model);
            }
            catch
            {
                return null;
            }
        }

        //[HttpGet]
        //public async Task<IActionResult> AcceptingSMS(Guid id)
        //{
        //    if (id == Guid.Empty)
        //    {
        //        return null;
        //    }

        //    PreFactor preFactors = await db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync();

        //    var model = new UpdateCustomerVModel() {
        //        PreFactor = preFactors,
        //        WorkSenf = null
        //    };
        //    return View(model);
        //}


        private async Task<UpdateCustomerVModel> BuildAcceptingSmsViewModel(PreFactor pf, Customer cus, ASetting setting)
        {
            var workSenf = await db.BaseItems.Where(f => f.TypeCode == BaseItemEnum.صنف).OrderBy(f => f.Title).ToListAsync();
            var bankAccList = await db.BankAccounts.Where(f => f.OfficialType == pf.OfficialType && f.Active).ToListAsync();

            ViewBag.isLogin = User.Identity?.Name == pf.Cus_Mobile ? 1 : 0;

            if (ViewBag.isLogin == 0)
                await signInManager.SignOutAsync();

            return new UpdateCustomerVModel {
                PreFactor = pf,
                WorkSenf = workSenf,
                Customer = cus,
                BankAccounts = bankAccList,
                Setting = setting
            };
        }

        [AllowAnonymous]
        [Route("{se}")]
        [HttpGet("{se}")]
        public async Task<IActionResult> AcceptingSMS(string se)
        {
                if (se == null) return BadRequest();
            try
            {
                var pf = await db.PreFactors.FirstOrDefaultAsync(f => f.Id.ToString().StartsWith(se));
                if (pf == null) return NotFound();

                var cus = await db.Customers.Where(f => f.Id == pf.CustomerId).FirstOrDefaultAsync();
                var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

                var model = await BuildAcceptingSmsViewModel(pf, cus, setting);
                return View(model);
            }
            catch
            {
                return BadRequest("خطای سرور");
            }
        }


        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AcceptingSMS([FromBody] PreFactor model)
        {
            await using var transAction = await db.Database.BeginTransactionAsync();
            try
            {
                var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();
                var oldPf = await db.PreFactors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
                var pf = await db.PreFactors.FirstOrDefaultAsync(f => f.Id == model.Id);

                pf.AgentName = model.AgentName?.Trim();

                (pf, var cus) = await AcceptCustomerPreFactor(pf, setting.SmallOffice);
                var user = await CreateUserForCustomer(cus, pf.Cus_Mobile);

                pf.Signature = Convert.FromBase64String(model.Emza?.Split(',')[1]);
                pf.SignatureDes = $"پیش فاکتور شماره {pf.SerialNumber} توسط اینجانب {model.AgentName} مورد تایید می باشد";
                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;

                if (setting.CustomerCanChooseInvoiceType)
                {
                    pf.OfficialType = model.OfficialType;
                    pf = Service.UpdateTaxAmountPreFactor(pf);
                }

                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تایید_و_تغییر_وضعیت, user, null, "تایید پیش‌فاکتور توسط مشتری");

                cus.AgentName = model.AgentName;

                await db.SaveChangesAsync();
                await transAction.CommitAsync();

                if (pf.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش)
                {
                    var cusText = string.IsNullOrWhiteSpace(cus.CompanyName) ? $"({cus.AgentName})" : $"{cus.CompanyName} ({cus.AgentName})";
                    await hub.Clients.Group("مدیر فروش").SendAsync("notifActionRequired",
                        $"پیش فاکتور <b>{pf.SerialNumber}</b> توسط مشتری <b>{cusText}</b> تایید شد و در کارتابل شما قرار گرفت",
                        "/Cartable/Index?tab=TSalesAdminId"
                    );
                    pushQueue.Enqueue(new PushQueueItem {
                        RoleName = "مدیر فروش",
                        Title = "تایید پیش‌فاکتور",
                        Body = $"پیش‌فاکتور {pf.SerialNumber} برای تایید در کارتابل شما قرار گرفت.",
                        Url = "/Cartable/Index?tab=TSalesAdminId",
                        Tag = "pf" + pf.SerialNumber
                    });

                    await hub.Clients.User(pf.UserCreatorID).SendAsync("notifInfo",
                        $"پیش فاکتور <b>{pf.SerialNumber}</b> توسط مشتری <b>{cusText}</b> تایید شد و در انتظار تایید مدیر فروش قرار گرفت",
                        $"/Customer/CustomerProfile?Id={pf.CustomerId}"
                    );
                    pushQueue.Enqueue(new PushQueueItem {
                        UserId = pf.UserCreatorID,
                        Title = "تایید پیش‌فاکتور",
                        Body = $"پیش فاکتور {pf.SerialNumber} توسط مشتری {cusText} تایید شد.",
                        Url = $"/Customer/CustomerProfile?Id={pf.CustomerId}",
                        Tag = "pf" + pf.SerialNumber
                    });
                }

                var modelView = await BuildAcceptingSmsViewModel(pf, cus, setting);
                return View(modelView);
            }
            catch (Exception e)
            {
                await transAction.RollbackAsync();
                Console.WriteLine(e);
                throw;
            }
        }


        [HttpGet]
        public async Task<IActionResult> AcceptPreFactorManual(Guid id)
        {
            await using var transAction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var set = await db.ASettings.FirstOrDefaultAsync();
                var oldPf = await db.PreFactors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
                var pf = await db.PreFactors.FirstOrDefaultAsync(f => f.Id == id);


                (pf, var cus) = await AcceptCustomerPreFactor(pf, set.SmallOffice);
                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;


                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تایید_و_تغییر_وضعیت, user, null, "تایید پیش‌فاکتور به صورت دستی");
                await db.SaveChangesAsync();
                await transAction.CommitAsync();


                if (pf.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش)
                {
                    var cusText = string.IsNullOrWhiteSpace(cus.CompanyName) ? $"({cus.AgentName})" : $"{cus.CompanyName} ({cus.AgentName})";
                    await hub.Clients.Group("مدیر فروش").SendAsync("notifActionRequired",
                        $"پیش فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusText} توسط <b>{user.FullName}</b> به صورت دستی تایید و در کارتابل شما قرار گرفت",
                        "/Cartable/Index?tab=TSalesAdminId"
                    );
                    pushQueue.Enqueue(new PushQueueItem {
                        RoleName = "مدیر فروش",
                        Title = "تایید پیش‌فاکتور",
                        Body = $"پیش‌فاکتور {pf.SerialNumber} برای تایید در کارتابل شما قرار گرفت.",
                        Url = "/Cartable/Index?tab=TSalesAdminId",
                        Tag = "pf" + pf.SerialNumber
                    });
                }

                return Ok();
            }
            catch (Exception ex)
            {
                await transAction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }


        private async Task<(PreFactor, Customer)> AcceptCustomerPreFactor(PreFactor pf, bool isSmallOffice)
        {
            var cus = await db.Customers.FirstOrDefaultAsync(x => x.Id == pf.CustomerId);
            var deal = pf.DealId != null ? await db.Deals.FirstOrDefaultAsync(f => f.Id == pf.DealId) : null;

            if (cus == null)
            {
                cus = await CreateCustomer(pf, deal, isSmallOffice);

                if (deal != null)
                {
                    await genService.UpdateChildEntityDeal(pf.DealId, cus);
                    deal.CustomerId = cus.Id;
                    deal.CustomerName = cus.AgentName;
                    deal.CustomerCode = cus.CustomerCode;
                }

                pf.CustomerId = cus.Id;
                pf.CustomerCode = cus.CustomerCode;
            } else if (cus.RoleType == RoleTypeEnum.تامین_کننده)
            {
                cus.RoleType = RoleTypeEnum.تامین_کننده_مشتری;
                cus.RoleTypeTxt = cus.RoleType.ToString().Replace("_", " ");
            }

            pf.Accepted = true;
            pf.AcceptedDate = DateTime.Now;
            pf.AcceptedDateTxt = dtService.GetDateTimePersian();

            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();

            deal?.CustomerName = pf.AgentName;

            if (isSmallOffice)
            {
                pf.PrePftype = pf.Pftype;
                pf.Pftype = (int)PreFactorEnum.در_انتظار_تولید;
                pf.PftypeTxt = Service.GetTypeTxtPfactor(pf.Pftype);
            } else
            {
                pf = await genService.AcceptOrRejectPreFactor(pf, 1);
            }

            await regAccountancy(pf);
            return (pf, cus);
        }


        private async Task<AppUser> CreateUserForCustomer(Customer cus, string mobile)
        {
            var user = await userManager.FindByNameAsync(mobile);
            if (user != null) return user;

            user = new AppUser {
                UserName = mobile,
                PhoneNumber = mobile,
                FirstName = cus.CompanyName,
                LastName = cus.AgentName,
                PersonId = cus.Id
            };

            var result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "مشتری");
                await _logSystemService.LogChange(user, null, LogActionEnum.ثبت, user, null, "ثبت یوزر مشتری");
            }

            return user;
        }

        private async Task<Customer> CreateCustomer(PreFactor pf, Deal deal, bool isSmallOffice)
        {
            var cus = new Customer {
                AgentName = pf.AgentName,
                NormalizedAgentName = TextService.NormalizeText(pf.AgentName),
                Mobile = pf.Cus_Mobile,
                RoleType = RoleTypeEnum.مشتری,
                RoleTypeTxt = Service.GetTypeTxtRoleType((int)RoleTypeEnum.مشتری),
                UserCreator = pf.UserCreator,
                UserCreatorID = pf.UserCreatorID,
                Accepted = isSmallOffice,

                NumberRegister = deal?.CusNumberRegister ?? pf.Cus_NumberRegister,
                EghtesadiCode = deal?.CusEghtesadiCode ?? pf.Cus_EghtesadiCode,
                ShenaseMeli = deal?.CusShenaseMeli ?? pf.Cus_ShenaseMeli,
                AgentMeliCode = deal?.CusMelliCode ?? pf.AgentMeliCode,
                PostalCode = deal?.CusPostalCode ?? pf.Cus_PostalCode,
                Juridical = deal?.CusJuridical ?? pf.Cus_Juridical,
                CompanyName = deal?.CusCompanyName ?? pf.Customer,
                NormalizedCompanyName = TextService.NormalizeText(deal?.CusCompanyName ?? pf.Customer),
                Address = deal?.CusAddress ?? pf.Cus_Address,
                Tel = deal?.CusTel ?? pf.Cus_Tel,
                Sex = deal?.CusSex ?? 1,
                Province = deal?.CusProvince,
                City = deal?.CusCity,
                WorkSenf = deal?.CusWorkSenf,
                WorkSenfTxt = deal?.CusWorkSenfTxt,
                WorkSenfDescription = deal?.CusWorkSenfDescription,
                AcquaintanceWay = deal?.CusAcquaintanceWay,
                AcquaintanceWayTxt = deal?.CusAcquaintanceWayTxt,
            };

            db.Customers.Add(cus);
            await db.SaveChangesAsync();

            var userCustomer = new UserCustomer {
                UserId = Guid.Parse(pf.UserCreatorID),
                CustomerId = cus.Id
            };

            await _logSystemService.LogChange(cus, null, LogActionEnum.ثبت, new AppUser { FirstName = "سیستم" });
            db.UserCustomers.Add(userCustomer);
            return cus;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SendToCustomer(Guid id, string toNumber)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldPfactor = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var pfactor = await db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync();

            pfactor.PrePftype = pfactor.Pftype;
            pfactor.Pftype = (int)PreFactorEnum.در_انتظار_تایید_مشتری;
            pfactor.PftypeTxt = Service.GetTypeTxtPfactor(pfactor.Pftype);
            pfactor.Accepted = false;
            pfactor.CommentText = null;


            await _logSystemService.LogChange(oldPfactor, pfactor, LogActionEnum.تغییر_وضعیت, user);
            await db.SaveChangesAsync();
            var res = await smsServ.SendAcceptPfactorToCustomer(toNumber, pfactor.SerialNumber, id);


            if (res.code == 200)
            {
                return Ok(res);
            } else
            {
                return BadRequest(res);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> changeTypePfactor(Guid? id, int type, string des)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldPf = await db.PreFactors.AsNoTracking().Where(F => F.Id == id).FirstOrDefaultAsync();
                PreFactor pf = null;

                switch (type)
                {
                    case 1:
                        pf = await db.PreFactors.Where(F => F.Id == id).FirstOrDefaultAsync();

                        pf.PrePftype = pf.Pftype;
                        pf.Pftype = type;
                        pf.PftypeTxt = Service.GetTypeTxtPfactor(type);
                        pf.PftypeChangeDateTxt = dtService.GetDateTimePersian(DateTime.Now);
                        pf.PftypeChangeDate = DateTime.Now;
                        pf.ExpireValidateDesc = des;
                        pf.AcceptedFinancial = null;

                        await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user);
                        await db.SaveChangesAsync();
                        break;
                    case 2:
                        pf = await db.PreFactors.Where(F => F.Id == id).FirstOrDefaultAsync();

                        pf.PrePftype = pf.Pftype;
                        pf.Pftype = type;
                        pf.PftypeTxt = Service.GetTypeTxtPfactor(type);
                        pf.PftypeChangeDateTxt = dtService.GetDateTimePersian(DateTime.Now);
                        pf.PftypeChangeDate = DateTime.Now;
                        pf.ExpireValidateDesc = des;
                        pf.AcceptedFinancial = null;

                        await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user);
                        await db.SaveChangesAsync();
                        break;
                    case 3:
                        pf = await db.PreFactors.Where(F => F.Id == id).FirstOrDefaultAsync();

                        pf.PrePftype = pf.Pftype;
                        pf.Pftype = type;
                        pf.PftypeTxt = Service.GetTypeTxtPfactor(type);
                        pf.PftypeChangeDateTxt = dtService.GetDateTimePersian(DateTime.Now);
                        pf.PftypeChangeDate = DateTime.Now;
                        pf.ExpireValidateDesc = des;

                        pf.AcceptedFinancial = null;

                        await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user);
                        await db.SaveChangesAsync();
                        break;
                    case 6:

                        pf = await db.PreFactors.Include(f => f.PreFactorItems).Include(f => f.Descriptions).Where(F => F.Id == id)
                            .FirstOrDefaultAsync();


                        pf.PrePftype = pf.Pftype;
                        pf.Pftype = (int)PreFactorEnum.در_انتظار_تولید;
                        pf.PftypeTxt = "تکمیل وجه شده";
                        pf.PftypeChangeDateTxt = dtService.GetDateTimePersian(DateTime.Now);
                        pf.PftypeChangeDate = DateTime.Now;
                        var js = JsonConvert.SerializeObject(pf);
                        var fac = JsonConvert.DeserializeObject<Factor>(js);
                        fac.Id = Guid.Empty;
                        fac.SerialNumber = 0;
                        fac.PfactorSerialNumber = pf.SerialNumber;
                        fac.PfactorID = pf.Id;
                        fac.DateCompleted = DateTime.Now;
                        fac.DateCompletedTxt = dtService.GetDatePersian();

                        IList<FactorItem> Fitems = new List<FactorItem>();

                        foreach (var item in db.PreFactorItems.Where(f => f.PrefactorId == pf.Id).ToList())
                        {
                            Fitems.Add(new FactorItem() {
                                Product = item.Product,
                                ProductSmallName = item.ProductSmallName,
                                Row = item.Row,
                                TotalAmount = item.TotalAmount,
                                Unit = item.Unit,
                                UnitAmount = item.UnitAmount,
                                UnitTxt = item.UnitTxt,
                                WeightCount = item.WeightCount,
                                CreateDate = DateTime.Now,
                                CreateDateTxt = dtService.GetDateTimePersian(DateTime.Now),
                                UserCreator = item.UserCreator,
                                UserCreatorID = item.UserCreatorID,
                            });
                        }

                        IList<FactorDescription> FitemDes = new List<FactorDescription>();
                        foreach (var item in db.PreFactorDescriptions.Where(f => f.PrefactorId == pf.Id).ToList())
                        {
                            FitemDes.Add(new FactorDescription() {
                                Order = item.Order,
                                Text = item.Text,
                                Dir = item.Dir
                            });
                        }

                        fac.FactorItems = Fitems;
                        fac.Descriptions = FitemDes;
                        await db.AddAsync(fac);

                        await db.SaveChangesAsync();
                        break;

                    case 8:
                        pf = await db.PreFactors.Where(F => F.Id == id).FirstOrDefaultAsync();

                        pf.PrePftype = pf.Pftype;
                        pf.Pftype = (int)PreFactorEnum.بسته_شده;
                        pf.PftypeTxt = Service.GetTypeTxtPfactor(type);
                        pf.PftypeChangeDateTxt = dtService.GetDateTimePersian(DateTime.Now);
                        pf.PftypeChangeDate = DateTime.Now;
                        pf.ExpireValidateDesc = des;

                        pf.UserModify = user.FullName;
                        pf.UserModifyID = user.Id;
                        pf.ModifyDate = DateTime.Now;
                        pf.ModifyDateTxt = dtService.GetDateTimePersian();

                        await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user);
                        await db.SaveChangesAsync();
                        break;
                }

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAttachment(Guid id, int? type = null)
        {
            var query = db.Attachments.Where(f => f.EntityId == id);

            if (type.HasValue)
            {
                query = query.Where(f => f._AttachType == (_AttachType)type.Value);
            }

            var attachments = await query.ToListAsync();
            return Ok(attachments);
        }


        [HttpGet]
        public async Task<IActionResult> GetAttachmentsInvoiceUploaded(Guid id)
        {
            try
            {
                var accountancies = await db.Accountancies.Where(f => f.pfid == id).ToListAsync();

                var accIdsList = accountancies.Select(g => g.Id);
                var attachments = await db.Attachments.Where(f =>
                        (f.EntityId == id && (f._AttachType == _AttachType.UploadCustomerPf || f._AttachType == _AttachType.UploadSalesExpertPf))
                        || accIdsList.Contains((Guid)f.EntityId) && f._AttachType == _AttachType.DocPayPreFactor)
                    .ToListAsync();

                var accCredits = accountancies.Where(f => f.PayType == PayTypeEnum.اعتباری);
                var model = new { attachments, accCredits };

                return Ok(model);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddPayAttachmentBack(Accountancy model, List<Attachment> ListAtt)
        {
            using var transAction = db.Database.BeginTransaction(); //شروع
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User); //یوزر جاری میخواند

                string fileN = ""; //اگر تصویر واریزی باشه ایجا نام جدید با ساختار جدید برایش ساخته میشود


                if (model.InvoiceType == InvoiceTypeEnum.پیش_فاکتور) //پیش فاکتور
                {
                    var pf = await db.PreFactors.Where(f => f.Id == model.pfid).FirstOrDefaultAsync();
                    model.pfSerialNumber = pf.SerialNumber;
                    model.CustomerId = pf.CustomerId;
                    //pf.ReciveAmount += model.ReciveAmount;

                    fileN = pf.SerialNumber + "_BPayPrefactor_";
                } else if (model.InvoiceType == InvoiceTypeEnum.فاکتور) // فاکتور
                {
                    var f = await db.Factors.Where(f => f.Id == model.fid).FirstOrDefaultAsync();
                    model.fSerialNumber = f.SerialNumber;
                    model.CustomerId = f.CustomerId;
                    //f.ReciveAmount += model.ReciveAmount;

                    fileN = f.SerialNumber + "_BPayfactor_";
                }

                var cus = await db.Customers.Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
                //صورت حساب ثبت میشود
                var accountancyID = await regAccountancy(model, user);


                // فایل ها ذخیره میشود
                foreach (var item in ListAtt)
                {
                    if (item.IsDeleted)
                    {
                        fileSaver.DelFile(item.Url);
                    } else
                    {
                        if (item.Id == Guid.Empty)
                        {
                            item.Filename = fileN +
                                            dtService.GetDateTimeMilisecondsPersian().Replace("/", "").Replace("/", "").Replace(":", "")
                                                .Replace(":", "").Replace(":", "").Replace(" ", "") + "_" + Guid.NewGuid().ToString().Substring(0, 4);
                            item.EntityId = accountancyID;
                            item.Url = fileSaver.SaveAcceptedPreFactorImg(item.File, item.Filename, item.fileExten);
                            item.File = null;
                            item.CreateDate = DateTime.Now;
                            item.CreateDateTxt = dtService.GetDateTimePersian();
                            item._AttachType = model.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                                ? _AttachType.DocPayPreFactor
                                : _AttachType.DocPayFactor;
                            item._AttachTypeTxt = model.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                                ? _AttachType.DocPayPreFactor.ToString()
                                : _AttachType.DocPayFactor.ToString();
                            item.AttachDate = DateTime.Now;
                            item.AttachDateTxt = dtService.GetDateTimePersian();
                            item.UserCreatorID = user.Id;
                            item.UserCreator = user.FullName;
                        }
                    }
                }

                db.Attachments.UpdateRange(ListAtt);


                cus.ModifyDate = DateTime.Now;
                cus.ModifyDateTxt = dtService.GetDateTimePersian();


                await crmSaveCahngeAsync.SaveChange();
                //await db.SaveChangesAsync();


                //await UpdatefAmount(model.FactorId);
                await transAction.CommitAsync();
                return Ok(new { status = "200", error = "", data = "" });
            }
            catch
            {
                await transAction.RollbackAsync();
                return BadRequest();
            }
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddPayAttachment([FromBody] AccViewModel model)
        {
            if (model?.AccData == null) return BadRequest();
            await using var transAction = await db.Database.BeginTransactionAsync(); //شروع
            try
            {
                var acc = model.AccData;
                var user = await userManager.GetUserAsync(HttpContext.User); //یوزر جاری میخواند
                var fileN = ""; //اگر تصویر واریزی باشه ایجا نام جدید با ساختار جدید برایش ساخته میشود
                Deal deal = null;

                if (acc.InvoiceType == InvoiceTypeEnum.پیش_فاکتور)
                {
                    var pf = await db.PreFactors.AsNoTracking().Where(f => f.Id == acc.pfid).FirstOrDefaultAsync();
                    if (pf != null)
                    {
                        deal = await db.Deals.Where(f => f.Id == pf.DealId).FirstOrDefaultAsync();

                        acc.CustomerId = pf.CustomerId;
                        acc.pfSerialNumber = pf.SerialNumber;
                        acc.DealId = pf.DealId;
                        acc.DealSerialNumber = pf.DealSerialNumber;

                        pf.ModifyDate = DateTime.Now;
                        pf.ModifyDateTxt = dtService.GetDateTimePersian();

                        fileN = pf.SerialNumber + "_APayPrefactor_";
                    }
                } else if (acc.InvoiceType == InvoiceTypeEnum.فاکتور)
                {
                    var f = await db.Factors.Where(f => f.Id == acc.fid).FirstOrDefaultAsync();
                    acc.fSerialNumber = f.SerialNumber;
                    acc.CustomerId = f.CustomerId;

                    fileN = f.SerialNumber + "_APayfactor_";
                } else if (acc.InvoiceType == InvoiceTypeEnum.مشتری)
                {
                    acc.fSerialNumber = null;
                    acc.pfSerialNumber = null;
                    acc.pfid = null;
                    acc.fid = null;
                }

                var cus = await db.Customers.Where(f => f.Id == acc.CustomerId).FirstOrDefaultAsync();
                acc.CustomerCode = cus?.CustomerCode;
                acc.CustomerName = cus != null
                    ? $"{cus.AgentName} {cus.CompanyName}".Trim()
                    : $"{deal?.CustomerName} {deal?.CusCompanyName}".Trim();


                if (acc.InvoiceType == InvoiceTypeEnum.مشتری)
                    fileN = cus?.CustomerCode + "_APayCustomer_";

                //صورت حساب ثبت میشود
                var accountancyId = await regAccountancy(acc, user);
                if (accountancyId == null)
                {
                    throw new Exception();
                }

                // فایل ها ذخیره میشود
                foreach (var item in model.ListAttach ?? [])
                {
                    if (item.IsDeleted)
                    {
                        db.Attachments.Update(item);
                        fileSaver.DelFile(item.Url);
                    } else
                    {
                        if (item.Id == Guid.Empty)
                        {
                            item.Id = Guid.NewGuid();
                            item.Filename = fileN +
                                            dtService.GetDateTimeMilisecondsPersian().Replace("/", "").Replace(":", "")
                                                .Replace(" ", "") + "_" + Guid.NewGuid().ToString().Substring(0, 4);
                            item.EntityId = accountancyId;
                            item.Url = fileSaver.SaveAcceptedPreFactorImg(item.File, item.Filename, item.fileExten);
                            item.File = null;
                            item.CreateDate = DateTime.Now;
                            item.CreateDateTxt = dtService.GetDateTimePersian();
                            item._AttachType = acc.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                                ? _AttachType.DocPayPreFactor
                                : _AttachType.DocPayFactor;
                            item._AttachTypeTxt = acc.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                                ? nameof(_AttachType.DocPayPreFactor)
                                : nameof(_AttachType.DocPayFactor);
                            item.AttachDate = DateTime.Now;
                            item.AttachDateTxt = dtService.GetDateTimePersian();
                            item.UserCreatorID = user.Id;
                            item.UserCreator = user.FullName;

                            db.Attachments.Add(item);
                        } else if (item._AttachType == _AttachType.UploadCustomerPf || item._AttachType == _AttachType.UploadSalesExpertPf)
                        {
                            item.EntityId = accountancyId;
                            item.ModifyDate = DateTime.Now;
                            item.File = null;
                            item.ModifyDateTxt = dtService.GetDateTimePersian();
                            item._AttachType = _AttachType.DocPayPreFactor;
                            item._AttachTypeTxt = nameof(_AttachType.DocPayPreFactor);
                            item.UserModifyID = user.Id;
                            item.UserModify = user.FullName;
                            db.Attachments.Update(item);
                        }
                    }
                }


                if (cus != null)
                {
                    cus.ModifyDate = DateTime.Now;
                    cus.ModifyDateTxt = dtService.GetDateTimePersian();
                }

                if (deal != null)
                {
                    deal.ModifyDate = DateTime.Now;
                    deal.ModifyDateTxt = dtService.GetDateTimePersian();
                }

                await crmSaveCahngeAsync.SaveChange();


                await transAction.CommitAsync();
                return Ok();
            }
            catch
            {
                await transAction.RollbackAsync();
                return BadRequest();
            }
        }

        public async Task<IActionResult> HasAnyEtebari(Guid pfId)
        {
            var hasAnyEtebari = await db.Accountancies.AnyAsync(a =>
                a.pfid == pfId
                && a.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                && (a.AccType == AcEnum.واریزی || a.AccType == AcEnum.برگشتی)
                && a.PayType == PayTypeEnum.اعتباری
                && a.Vosoled == false
            );

            return hasAnyEtebari ? StatusCode(201) : Ok();
        }

        private async Task CheckPayment(Guid preFactor)
        {
            var ac = await db.Accountancies
                .Where(f => f.pfid == preFactor && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور && f.AccType == AcEnum.واریزی).ToListAsync();
            var factors = await db.Factors.Where(f => f.PfactorID == preFactor).ToListAsync();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> addAcPrefacAttachment(List<Attachment> list, Guid pfId)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var pf = await db.PreFactors.AsNoTracking().Where(f => f.Id == pfId).FirstOrDefaultAsync();
                if (pf != null)
                {
                    foreach (var item in list)
                    {
                        if (item.Id == Guid.Empty)
                        {
                            item.Id = Guid.NewGuid();
                            item.Filename = pf.SerialNumber + "_AcPfactor_" +
                                            dtService.GetDateTimeMilisecondsPersian().Replace("/", "").Replace("/", "").Replace(":", "")
                                                .Replace(":", "").Replace(":", "").Replace(" ", "") + "_" + Guid.NewGuid().ToString().Substring(0, 4);
                            item.EntityId = pfId;
                            item.Url = fileSaver.SaveAcceptedPreFactorImg(item.File, item.Filename, item.fileExten);
                            item.File = null;
                            item.CreateDate = DateTime.Now;
                            item.CreateDateTxt = dtService.GetDatePersian();
                            item._AttachType = _AttachType.AcceptedCusPreFactor;
                            item._AttachTypeTxt = _AttachType.AcceptedCusPreFactor.ToString();
                            item.AttachDate = DateTime.Now;
                            item.AttachDateTxt = dtService.GetDateTimePersian();
                            item.UserCreatorID = user.Id;
                            item.UserCreator = user.FullName;
                            db.Attachments.Add(item);
                            await _logSystemService.LogChange(item, null, LogActionEnum.ثبت, user, null,
                                $"پیوست تایید برای پیش‌فاکتور {pf.SerialNumber}");
                        } else if (item.IsDeleted)
                        {
                            await _logSystemService.LogChange(item, null, LogActionEnum.حذف, user, null,
                                $"پیوست تایید برای پیش‌فاکتور {pf.SerialNumber}");
                            db.Attachments.Update(item);
                        }
                    }
                }

                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        public async Task<IActionResult> PreviewPfactor(Guid id)
        {
            var data = await db.OrderHeads.Where(f => f.Id == id).Include(f => f.Orders).FirstOrDefaultAsync();

            return View(data);
        }

        /*private async Task<Guid?> regAccountancy(PaymentVm model, Customer cus, AppUser user)
        {
            try
            {
                var setting = await db.ASettings.FirstOrDefaultAsync();
                var pf = await db.PreFactors.AsNoTracking().Where(f => f.Id == model.PFactorId).FirstOrDefaultAsync();

                if (model.Id != Guid.Empty)
                {
                    var oldAcc = await db.Accountancies.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
                    var acc = await db.Accountancies.Where(f => f.Id == model.Id).FirstOrDefaultAsync();


                    acc.AccType = model.AccType;
                    acc.AccTypeTxt = Enum.GetName(typeof(AcEnum), model.AccType);

                    acc.CustomerId = model.CustomerId != null ? Guid.Parse(model.CustomerId.ToString()) : null;
                    acc.CustomerCode = cus?.CustomerCode;
                    acc.CustomerName = cus != null ? $"{cus.AgentName} {cus.CompanyName}".Trim() : pf.AgentName.Trim();

                    if (model.AccType == AcEnum.واریزی)
                        acc.credit = model.ReciveAmount;
                    if (model.AccType == AcEnum.بدهکاری)
                        acc.debit = model.ReciveAmount;
                    if (model.AccType == AcEnum.برگشتی)
                        acc.BackWard = model.ReciveAmount;


                    acc.fid = model.FactorId;
                    acc.fSerialNumber = model.SerialFactor;
                    acc.pfSerialNumber = pf.SerialNumber;
                    acc.ModifyDate = DateTime.Now;
                    acc.ModifyDateTxt = dtService.GetDateTimePersian();
                    acc.UserModify = user.FullName;
                    acc.UserModifyID = user.Id;
                    acc.InvoiceType = (InvoiceTypeEnum)model.InvoiceType; //پیش فاکتور -  فاکتور
                    acc.InvoiceTypeTxt = Service.getTypetxtInvoiceType(model.InvoiceType);
                    acc.PayType = (PayTypeEnum)model.PayType; //نقدی - چکی -اعتباری
                    acc.PayTypeTxt = Service.getTypetxtPayType(model.PayType);
                    acc.pfid = model.PFactorId;
                    acc.BankAccountId = model.BankAccountId == null ? null : Guid.Parse(model.BankAccountId);
                    acc.BankAccountTxt = model.BankAccountTxt;
                    acc.FromBankAccountTxt = model.FromBankAccountTxt;
                    acc.VosolDate = model.VosolDate;
                    acc.VosolDateM = dtService.GetDateMiladi(model.VosolDate);
                    acc.ReciveDate = model.ReciveDate;
                    acc.ReciveDateM = dtService.GetDateMiladi(model.ReciveDate);
                    acc.DealId = pf.DealId;
                    acc.DealSerialNumber = pf.DealSerialNumber;
                    acc.Beneficiary = model.Beneficiary;
                    acc.Status = model.Status;
                    acc.ToName = model.ToName;


                    await _logSystemService.LogChange(oldAcc, acc, LogActionEnum.ویرایش, user);

                    if (setting?.FinancialConfirm != true)
                    {
                        var tasks = await ts.TaskReciveAmountPrefctor(acc, model.InvoiceType);
                        await db.Carizos.AddRangeAsync(tasks);
                    }


                    return acc.Id;
                } else
                {
                    var acP = new Accountancy() {
                        AccType = model.AccType,
                        AccTypeTxt = Enum.GetName(typeof(AcEnum), model.AccType),

                        CustomerId = model.CustomerId != null ? Guid.Parse(model.CustomerId.ToString()) : null,
                        CustomerCode = cus?.CustomerCode,
                        CustomerName = cus != null ? $"{cus.AgentName} {cus.CompanyName}".Trim() : pf.AgentName.Trim(),
                        //date = DateTime.Parse(dtService.GetDateMiladi(model.ReciveDate).ToString()),
                        //dateSh = model.ReciveDate,
                        fid = model.FactorId,
                        fSerialNumber = model.SerialFactor,
                        pfSerialNumber = pf.SerialNumber,
                        CreateDate = DateTime.Now,
                        CreateDateTxt = dtService.GetDateTimePersian(),
                        InvoiceType = (InvoiceTypeEnum)model.InvoiceType,
                        InvoiceTypeTxt = Service.getTypetxtInvoiceType(model.InvoiceType), //پیش فاکتور -  فاکتور
                        PayType = (PayTypeEnum)model.PayType,
                        PayTypeTxt = Service.getTypetxtPayType(model.PayType), //نقدی - چکی -اعتباری
                        pfid = model.PFactorId,
                        BankAccountId = model.BankAccountId == null ? null : Guid.Parse(model.BankAccountId),
                        BankAccountTxt = model.BankAccountTxt,
                        FromBankAccountTxt = model.FromBankAccountTxt,
                        VosolDate = model.VosolDate,
                        VosolDateM = dtService.GetDateMiladi(model.VosolDate),
                        ReciveDate = model.ReciveDate,
                        ReciveDateM = dtService.GetDateMiladi(model.ReciveDate),
                        UserCreator = user.FullName,
                        UserCreatorID = user.Id,
                        DealId = pf.DealId,
                        DealSerialNumber = pf.DealSerialNumber,
                        Status = model.Status,
                        Beneficiary = model.Beneficiary,
                        ToName = model.ToName
                    };

                    switch (model.AccType)
                    {
                        case AcEnum.واریزی:
                            acP.credit = model.ReciveAmount;
                            break;
                        case AcEnum.بدهکاری:
                            acP.debit = model.ReciveAmount;
                            break;
                        case AcEnum.برگشتی:
                            acP.BackWard = model.ReciveAmount;
                            break;
                    }


                    if (model.ParentId != null)
                    {
                        acP.ParentId = model.ParentId;

                        var accParent = await db.Accountancies.Where(f => f.Id == model.ParentId).FirstOrDefaultAsync();
                        accParent.Vosoled = true;

                        var carizos = await db.Carizos.Where(f => f.EntityId == model.ParentId).ToListAsync();
                        if (carizos.Count != 0)
                        {
                            db.Carizos.RemoveRange(carizos);
                        }
                    }


                    await db.Accountancies.AddAsync(acP);

                    await db.SaveChangesAsync();
                    await db.Entry(acP).ReloadAsync();
                    await _logSystemService.LogChange(acP, null, LogActionEnum.ثبت, user);

                    if (setting.FinancialConfirm != true)
                    {
                        var tasks = await ts.TaskReciveAmountPrefctor(acP, model.InvoiceType);
                        await db.Carizos.AddRangeAsync(tasks);
                    }

                    return acP.Id;
                }
            }
            catch
            {
                return null;
            }
        }*/


        [Authorize]
        private async Task<Guid?> regAccountancy(Accountancy model, AppUser user)
        {
            try
            {
                var setting = await db.ASettings.FirstOrDefaultAsync();
                var roles = await userManager.GetRolesAsync(user);
                // ================= UPDATE =================
                if (model.Id != Guid.Empty)
                {
                    var oldAcc = await db.Accountancies.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
                    var acc = await db.Accountancies.FirstOrDefaultAsync(f => f.Id == model.Id);

                    acc = InitBaseFields(acc);

                    acc.ModifyDate = DateTime.Now;
                    acc.ModifyDateTxt = dtService.GetDateTimePersian();
                    acc.UserModify = user.FullName;
                    acc.UserModifyID = user.Id;

                    await _logSystemService.LogChange(oldAcc, acc, LogActionEnum.ویرایش, user);

                    if (setting?.FinancialConfirm != true)
                    {
                        var tasks = await ts.TaskReciveAmountPrefctor(acc, model.InvoiceType);
                        await db.Carizos.AddRangeAsync(tasks);
                    }

                    return acc.Id;
                }

                // ================= CREATE =================

                model = InitBaseFields(model);

                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;

                if (model.ParentId != null)
                {
                    var parent = await db.Accountancies.FirstOrDefaultAsync(f => f.Id == model.ParentId);
                    parent?.Vosoled = true;

                    var carizos = await db.Carizos.Where(f => f.EntityId == model.ParentId).ToListAsync();

                    if (carizos.Any())
                        db.Carizos.RemoveRange(carizos);
                }

                await db.Accountancies.AddAsync(model);

                await db.SaveChangesAsync();
                await db.Entry(model).ReloadAsync();
                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

                if (setting?.FinancialConfirm != true)
                {
                    var tasks = await ts.TaskReciveAmountPrefctor(model, model.InvoiceType);
                    await db.Carizos.AddRangeAsync(tasks);
                }

                return model.Id;

                Accountancy InitBaseFields(Accountancy ac)
                {
                    ac.AccType = model.AccType;
                    ac.AccTypeTxt = Enum.GetName(model.AccType);

                    ac.CustomerId = model.CustomerId;
                    ac.CustomerCode = model.CustomerCode;
                    ac.CustomerName = model.CustomerName;

                    ac.pfid = model.pfid;
                    ac.fid = model.fid;
                    ac.pfSerialNumber = model.pfSerialNumber ?? ac.pfSerialNumber;
                    ac.fSerialNumber = model.fSerialNumber;

                    ac.InvoiceType = model.InvoiceType; //پیش فاکتور -  فاکتور
                    ac.InvoiceTypeTxt = Service.getTypetxtInvoiceType((int)model.InvoiceType);

                    ac.PayType = model.PayType; //نقدی - چکی -اعتباری
                    ac.PayTypeTxt = Service.getTypetxtPayType((int)model.PayType);

                    ac.BankAccountId = model.BankAccountId;
                    ac.BankAccountTxt = model.BankAccountTxt;
                    ac.FromBankAccountTxt = model.FromBankAccountTxt;

                    ac.VosolDate = model.VosolDate;
                    ac.ReciveDate = model.ReciveDate;

                    ac.VosolDateM = dtService.GetDateMiladi(model.VosolDate);
                    ac.ReciveDateM = dtService.GetDateMiladi(model.ReciveDate);

                    ac.DealId = model.DealId ?? ac.DealId;
                    ac.DealSerialNumber = model.DealSerialNumber ?? ac.DealSerialNumber;

                    ac.Beneficiary = model.Beneficiary;

                    ac.Status = roles.Contains("کارشناس مالی") || model.AccType == AcEnum.برگشتی
                        ? AccStatusEnum.در_دست_تایید
                        : AccStatusEnum.ثبت_اولیه;
                    ac.ToName = model.ToName;

                    switch (model.AccType)
                    {
                        case AcEnum.واریزی:
                            ac.credit = model.ReciveAmount;
                            break;
                        case AcEnum.بدهکاری:
                            ac.debit = model.ReciveAmount;
                            break;
                        case AcEnum.برگشتی:
                            ac.BackWard = model.ReciveAmount;
                            break;
                    }

                    return ac;
                }
            }
            catch
            {
                return null;
            }
        }


        private async Task UpdatefAmount(Guid fid)
        {
            try
            {
                Factor fac = await db.Factors.Where(f => f.Id == fid).FirstOrDefaultAsync();
                decimal? FacAmount = 0;
                decimal? FacPayment = 0;
                decimal? Remained = 0;
                var payfac = await db.Accountancies.Where(f => f.fid == fac.Id).ToListAsync();
                FacAmount += payfac.Where(f => f.AccType == AcEnum.بدهکاری).Sum(f => f.debit);
                foreach (var p in payfac.Where(f => f.AccType == AcEnum.واریزی).ToList())
                {
                    FacPayment += p.credit;
                }

                Remained = FacAmount - FacPayment;
                fac.ReciveAmount = decimal.Parse(FacPayment.ToString());
                fac.PaymentAmount = decimal.Parse(Remained.ToString());
                await db.SaveChangesAsync();
            }
            catch { }
        }


        [Authorize]
        public async Task<IActionResult> GetDetailAmount(Guid? cusId)
        {
            try
            {
                var accountancies = await db.Accountancies.NotVosoled().Where(f => f.CustomerId == cusId).ToListAsync();

                var pFacPayment = accountancies.Where(f =>
                    f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                    && f.AccType == AcEnum.واریزی
                    && (f.PayType == PayTypeEnum.نقدی || f.PayType == PayTypeEnum.تهاتری)
                    && f.Status == AccStatusEnum.تایید_شده
                    && !f.Wallet
                ).Sum(f => f.credit) ?? 0;

                var returnPayment = accountancies.Where(f =>
                    f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                    && f.AccType == AcEnum.برگشتی
                    && f.Status == AccStatusEnum.تایید_شده
                    && !f.Wallet
                ).Sum(f => f.BackWard) ?? 0;

                var allPrePayment = pFacPayment - returnPayment;

                //FacAmount = db.Accountancies.Where(f => f.AccType == (int)AcEnum.بدهکاری && f.CustomerId == cusId && f.FirstAcc == false).Sum(f => f.debit);

                var facIds = await db.Factors.Where(f =>
                    f.CustomerId == cusId
                    && f.status == FactorStatus.تایید_شده
                    && !f.Returned
                    && f.ParentReturnedId == null
                ).Select(f => f.Id).ToListAsync();

                var facAmount = accountancies.Where(f =>
                    f.InvoiceType == InvoiceTypeEnum.فاکتور
                    && f.fid != null
                    && facIds.Contains(f.fid.Value)
                ).Sum(f => f.debit) ?? 0;


                var remained = facAmount - allPrePayment > 0 ? facAmount - allPrePayment : 0;

//                var balance = Math.Max(0, allPrePayment - facAmount);
                var wallet = accountancies.Where(f => f.Wallet && f.Status == AccStatusEnum.تایید_شده).Sum(f => f.credit) ?? 0;

                return Ok(new { remained, facAmount, allPrePayment, wallet });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize]
        private async Task regAccountancy(PreFactor model)
        {
            try
            {
                var acou = await db.Accountancies.Where(f => f.pfid == model.Id && f.FirstAcc).FirstOrDefaultAsync();
                var cus = await db.Customers.AsNoTracking().Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();


                if (acou != null)
                {
                    acou.debit = model.SumTotalAmount + model.TotalTax - model.Discount;
                    acou.taxPercent = model.OfficialType == 1 ? 10 : 0;
                    acou.tax = model.TotalTax;
                    acou.discount = model.Discount;
                    acou.ModifyDate = DateTime.Now;
                    acou.ModifyDateTxt = dtService.GetDateTimePersian();

                    //                db.Accountancies.RemoveRange(acou);
                } else
                {
                    Accountancy acD = new Accountancy() {
                        FirstAcc = true,
                        InvoiceType = InvoiceTypeEnum.پیش_فاکتور,
                        InvoiceTypeTxt = InvoiceTypeEnum.پیش_فاکتور.ToString(),
                        AccType = AcEnum.بدهکاری,
                        AccTypeTxt = AcEnum.بدهکاری.ToString(),
                        CustomerId = model.CustomerId != null ? Guid.Parse(model.CustomerId.ToString()) : null,
                        CustomerCode = cus?.CustomerCode,
                        CustomerName = cus != null ? cus.AgentName + ' ' + cus.CompanyName : model.AgentName,
                        debit = model.SumTotalAmount + model.TotalTax - model.Discount,
                        taxPercent = model.OfficialType == 1 ? 10 : 0,
                        tax = model.TotalTax,
                        discount = model.Discount,
                        date = DateTime.Now.ToLocalTime(),
                        dateSh = dtService.GetDateTimePersian(),
                        pfid = model.Id,
                        pfSerialNumber = model.SerialNumber,
                        UserCreatorID = model.UserCreatorID,
                        UserCreator = model.UserCreator,
                        DealId = model.DealId,
                        DealSerialNumber = model.DealSerialNumber,
                    };
                    await db.Accountancies.AddAsync(acD);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Authorize]
        private async Task regAccountancy(Factor model)
        {
            var acou = await db.Accountancies.Where(f => f.fid == model.Id && f.AccType == AcEnum.بدهکاری).FirstOrDefaultAsync();
            var cus = await db.Customers.AsNoTracking().Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
            var pf = await db.PreFactors.AsNoTracking().Where(f => f.Id == model.PfactorID).FirstOrDefaultAsync();

            if (acou != null)
            {
                //db.Accountancies.RemoveRange(acou);

                acou.debit = model.SumTotalAmount + model.TotalTax - model.Discount;
                acou.taxPercent = 10;
                acou.tax = model.TotalTax;
                acou.discount = model.Discount;
                acou.ModifyDate = DateTime.Now.ToLocalTime();
                acou.ModifyDateTxt = dtService.GetDateTimePersian();
            } else
            {
                var acD = new Accountancy() {
                    AccType = AcEnum.بدهکاری,
                    AccTypeTxt = AcEnum.بدهکاری.ToString(),
                    InvoiceType = InvoiceTypeEnum.فاکتور,
                    InvoiceTypeTxt = InvoiceTypeEnum.فاکتور.ToString(),

                    CustomerId = model.CustomerId != null ? Guid.Parse(model.CustomerId.ToString()) : null,
                    CustomerCode = cus?.CustomerCode,
                    CustomerName = cus != null ? cus.AgentName + ' ' + cus?.CompanyName : model.AgentName,

                    debit = model.SumTotalAmount + model.TotalTax - model.Discount,
                    taxPercent = 10,
                    tax = model.TotalTax,
                    discount = model.Discount,

                    date = DateTime.Now.ToLocalTime(),
                    dateSh = dtService.GetDateTimePersian(),
                    fid = model.Id,
                    fSerialNumber = model.SerialNumber,

                    pfid = model.PfactorID,
                    pfSerialNumber = model.PfactorSerialNumber,
                    UserCreator = pf.UserCreator,
                    UserCreatorID = pf.UserCreatorID,
                    DealId = model.DealId,
                    DealSerialNumber = model.DealSerialNumber,
                };
                await db.Accountancies.AddAsync(acD);
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangeStatePf(Guid id, int tCode)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;

                var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                var pf = await db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync();
                switch (tCode)
                {
                    case (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد: // 7
                    {
                        var orderHeads = await db.OrderHeads.Where(f => f.Prefactor == id && f.Status != OrderHeadStatusEnum.تحویل_شده).ToListAsync();
                        foreach (var oh in orderHeads)
                        {
                            var oldOh = await db.OrderHeads.AsNoTracking().Where(f => f.Id == oh.Id).FirstOrDefaultAsync();
                            oh.Status = OrderHeadStatusEnum.تحویل_شده;
                            oh.ModifyDate = DateTime.Now;
                            oh.ModifyDateTxt = dtService.GetDateTimePersian();
                            oh.UserModify = user.FullName;
                            oh.UserModifyID = user.Id;

                            await _logSystemService.LogChange(oldOh, oh, LogActionEnum.ویرایش, user);
                        }

                        break;
                    }
                }

                pf.PrePftype = pf.Pftype;
                pf.Pftype = tCode;
                pf.PftypeTxt = Service.GetTypeTxtPfactor(tCode);

                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;
                pf.ModifyDate = DateTime.Now;
                pf.ModifyDateTxt = dtService.GetDateTimePersian();


                await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user);

                await db.SaveChangesAsync();

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpHeadF(Guid Fid)
        {
            var model = await db.Factors.Where(f => f.Id == Fid).FirstOrDefaultAsync();
            var cus = await db.Customers.AsNoTracking().Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
            model.AgentName = cus.AgentName;
            model.AgentMeliCode = cus.AgentMeliCode;
            model.Customer = cus.CompanyName;
            model.Cus_EghtesadiCode = cus.EghtesadiCode;
            model.Cus_NumberRegister = cus.NumberRegister;
            model.Cus_ShenaseMeli = cus.ShenaseMeli;
            model.Cus_PostalCode = cus.PostalCode;
            model.Cus_Mobile = cus.Mobile;
            model.Cus_Tel = cus.Tel;
            model.Cus_Address = cus.Address;

            await db.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> UpHeadPf(Guid Pfid)
        {
            var model = await db.PreFactors.Where(f => f.Id == Pfid).FirstOrDefaultAsync();
            var cus = await db.Customers.AsNoTracking().Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
            if (cus != null)
            {
                model.AgentName = cus.AgentName?.Trim();
                model.AgentMeliCode = cus.AgentMeliCode;
                model.Customer = cus.CompanyName?.Trim();
                model.CustomerCode = cus.CustomerCode;
                model.Cus_EghtesadiCode = cus.EghtesadiCode;
                model.Cus_NumberRegister = cus.NumberRegister;
                model.Cus_ShenaseMeli = cus.ShenaseMeli;
                model.Cus_PostalCode = cus.PostalCode;
                model.Cus_Mobile = cus.Mobile;
                model.Cus_Tel = cus.Tel;
                model.Cus_Address = cus.Address;
                model.Cus_Juridical = cus.Juridical;
            } else
            {
                var deal = await db.Deals.AsNoTracking().Where(f => f.Id == model.DealId).FirstOrDefaultAsync();
                if (deal == null) return Ok();

                model.AgentName = deal.CustomerName?.Trim();
                model.Cus_Mobile = deal.MobileNumber;
                model.Cus_Juridical = deal.CusJuridical;
                model.Customer = deal.CusCompanyName;
                model.AgentMeliCode = deal.CusMelliCode;
                model.Cus_Tel = deal.CusTel;
                model.Cus_EghtesadiCode = deal.CusEghtesadiCode;
                model.Cus_NumberRegister = deal.CusNumberRegister;
                model.Cus_ShenaseMeli = deal.CusShenaseMeli;
                model.Cus_Address = deal.CusAddress;
                model.Cus_PostalCode = deal.CusPostalCode;
            }

            await db.SaveChangesAsync();
            return Ok();
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddPrefactor(PreFactor model)
        {
            await using var transAction = await db.Database.BeginTransactionAsync();
            try
            {
                var cus = await db.Customers.Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
                var deal = await db.Deals.Where(f => f.Id == model.DealId).FirstOrDefaultAsync();
                var user = await userManager.GetUserAsync(HttpContext.User);

                bool isNewPrefactor = false;
                PreFactor oldModel = null;


                foreach (var item in model.PreFactorItems)
                {
                    var product = await db.Products.AsNoTracking().Where(f => f.Id == item.ProductCode).FirstOrDefaultAsync();
                    item.IsInProduction = product.IsInProduction;
                }


                var deletedItems = model.PreFactorItems.Where(f => f.IsDeleted).Select(f => f.Id).ToList();
                if (deletedItems.Any())
                {
                    var supplies = await db.Supplies.Where(f => deletedItems.Contains(f.PfItemId.Value)).ToListAsync();
                    foreach (var supply in supplies)
                        supply?.IsDeleted = true;
                }


                if (model.Id == Guid.Empty)
                {
                    isNewPrefactor = true;

                    model.UserCreatorID = user.Id;
                    model.UserCreator = user.FullName;

                    if (model.PreFactorItems.Any(f => f.Printable == 1))
                    {
                        model.Descriptions?.Add(new PreFactorDescription() {
                            Text = "اختلاف رنگ 10 الی 15 درصدی در چاپ طبیعی می باشد.",
                            PrefactorId = model.Id
                        });
                    }

                    if (cus != null)
                    {
                        model.AgentName = cus.AgentName?.Trim();
                        model.AgentMeliCode = cus.AgentMeliCode;
                        model.Customer = cus.CompanyName?.Trim();
                        model.CustomerCode = cus.CustomerCode;
                        model.Cus_EghtesadiCode = cus.EghtesadiCode;
                        model.Cus_NumberRegister = cus.NumberRegister;
                        model.Cus_ShenaseMeli = cus.ShenaseMeli;
                        model.Cus_PostalCode = cus.PostalCode;
                        model.Cus_Mobile = cus.Mobile;
                        model.Cus_Tel = cus.Tel;
                        model.Cus_Address = cus.Address;
                        model.Cus_Juridical = cus.Juridical;
                    } else
                    {
                        model.AgentName = deal.CustomerName?.Trim();
                        model.AgentMeliCode = deal.CusMelliCode;
                        model.Customer = deal.CusCompanyName;
                        model.Cus_Mobile = deal.MobileNumber;
                        model.Cus_Juridical = deal.CusJuridical;
                        model.Cus_Tel = deal.CusTel;
                        model.Cus_EghtesadiCode = deal.CusEghtesadiCode;
                        model.Cus_NumberRegister = deal.CusNumberRegister;
                        model.Cus_ShenaseMeli = deal.CusShenaseMeli;
                        model.Cus_Address = deal.CusAddress;
                        model.Cus_PostalCode = deal.CusPostalCode;
                    }
                } else
                {
                    oldModel = await db.PreFactors.AsNoTracking().Where(f => f.Id == model.Id)
                        .Include(f => f.PreFactorItems)
                        .Include(f => f.Descriptions)
                        .FirstOrDefaultAsync();

                    model.UserModifyID = user.Id;
                    model.UserModify = user.FullName;
                    model.ModifyDate = DateTime.Now;
                    model.ModifyDateTxt = dtService.GetDateTimePersian();
                    if (model.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش || model.Pftype == (int)PreFactorEnum.در_انتظار_تولید ||
                        model.Pftype == (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد)
                    {
                        await regAccountancy(model);
                    }
                }


                model.DateMiladi = dtService.GetDateMiladi(model.Date);
                model.ValidityDateMiladi = dtService.GetDateMiladi(model.ValidityDade);


                model = await checkPrefactor(model);
                model = genService.UpdatePaymentAmount(model);

                db.PreFactors.Update(model);
                db.Entry(model).Property(f => f.SerialNumber).IsModified = false;

                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();

                if (cus != null)
                {
                    cus.ModifyDate = DateTime.Now;
                    cus.ModifyDateTxt = dtService.GetDateTimePersian();
                }

                if (deal != null)
                {
                    deal.ModifyDate = DateTime.Now;
                    deal.ModifyDateTxt = dtService.GetDateTimePersian();
                }

                if (isNewPrefactor)
                {
                    await db.SaveChangesAsync();
                    await db.Entry(model).ReloadAsync();
                    await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
                } else
                {
                    await _logSystemService.LogChange(oldModel, model, LogActionEnum.ویرایش, user, ["Descriptions", "PreFactorItems"]);
                }


                await db.SaveChangesAsync();
                await transAction.CommitAsync();
                return Ok();
            }
            catch (Exception e)
            {
                await transAction.RollbackAsync();
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddFactor(Factor model, bool unlinkSepidar = false)
        {
            if (model == null) return NotFound();

            await using var transAction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();
                var cus = await db.Customers.AsTracking().Where(f => f.Id == model.CustomerId).FirstOrDefaultAsync();
                var pf = await db.PreFactors.AsNoTracking().Where(f => f.Id == model.PfactorID).FirstOrDefaultAsync();
                var deal = await db.Deals.Where(f => f.Id == model.DealId).FirstOrDefaultAsync();

                bool isNewFactor;
                Factor oldModel = null;
                var isReturnFactor = model.ParentReturnedId != null;

                if (setting.FactorTowStep == false)
                {
                    model.status = FactorStatus.تایید_شده;
                    model.statusTxt = nameof(FactorStatus.تایید_شده).Replace("_", " ");
                }

                if (model.Id == Guid.Empty)
                {
                    isNewFactor = true;

                    if (isReturnFactor)
                    {
                        var parent = await db.Factors.Where(f => f.Id == model.ParentReturnedId).FirstOrDefaultAsync();
                        parent?.Returned = true;
                    }

                    model.UserCreatorID = user.Id;
                    model.UserCreator = user.FullName;
                    model.CreateDate = DateTime.Now;
                    model.CreateDateTxt = dtService.GetDateTimePersian();
                    model.Date = dtService.GetDatePersian();
                    model.CountNum = 0;

                    if (cus != null)
                    {
                        model.AgentName = cus.AgentName;
                        model.AgentMeliCode = cus.AgentMeliCode;
                        model.Customer = cus.CompanyName;
                        model.CustomerCode = cus.CustomerCode;
                        model.Cus_EghtesadiCode = cus.EghtesadiCode;
                        model.Cus_NumberRegister = cus.NumberRegister;
                        model.Cus_ShenaseMeli = cus.ShenaseMeli;
                        model.Cus_PostalCode = cus.PostalCode;
                        model.Cus_Mobile = cus.Mobile;
                        model.Cus_Tel = cus.Tel;
                        model.Cus_Address = cus.Address;
                    } else
                    {
                        model.AgentName = deal.CustomerName;
                        model.Cus_Mobile = deal.MobileNumber;
                    }
                } else
                {
                    isNewFactor = false;
                    oldModel = await db.Factors.AsNoTracking().Where(f => f.Id == model.Id)
                        .Include(f => f.FactorItems)
                        .Include(f => f.Descriptions)
                        .FirstOrDefaultAsync();

                    model.UserModifyID = user.Id;
                    model.UserModify = user.FullName;


                    if (unlinkSepidar)
                    {
                        model.SepidarId = null;
                        model.SnFinancial = null;
                    }

                    if (setting.Sepidar == true && model.SepidarId != null)
                    {
                        var sF = await S1db.vwInvoice.AsNoTracking().Where(f => f.InvoiceId == model.SepidarId).FirstOrDefaultAsync();
                        if (sF == null) return Ok(new { status = 201 });

                        model.DateMiladi = sF.Date;
                        model.Date = sF.DateTXT;
                    }
                }

                if (pf != null)
                {
                    model.SelerFullName = pf.UserCreator;
                    model.SelerId = pf.UserCreatorID;
                    model.DealId = pf.DealId;
                    model.DealSerialNumber = pf.DealSerialNumber;
                }

                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();
                model.DateMiladi = dtService.GetDateMiladi(model.Date);

                model = genService.UpdatePaymentAmount(model);


                db.Factors.Update(model);
                db.Entry(model).Property(f => f.SerialNumber).IsModified = false;

                await db.SaveChangesAsync();
                await db.Entry(model).ReloadAsync();
                if (!isReturnFactor)
                {
                    await regAccountancy(model);
                }

                if (cus != null)
                {
                    cus.ModifyDate = DateTime.Now;
                    cus.ModifyDateTxt = dtService.GetDateTimePersian();
                    cus.Success = await genService.IsCustomerActive(model.CustomerId);
                    cus.Export = await genService.IsCustomerExport(model.CustomerId);
                }

                if (isNewFactor)
                {
                    await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);

                    var title = isReturnFactor ? "فاکتور برگشتی" : "فاکتور جدید";
                    var cusTxt = $"{model.Customer} ({model.AgentName})".Trim();
                    var message =
                        $"یک {title} به شماره سریال {model.SerialNumber} برای پیش‌فاکتور {model.PfactorSerialNumber} ،مشتری {cusTxt} ثبت شد.";
                    await genService.AddEmail(title, message, user, model.SelerId, model.SelerFullName, EntityTypeEnum.Factor, model.Id);

                    await hub.Clients.User(model.SelerId).SendAsync("notifInfo", message, "/Email/Index");

                    pushQueue.Enqueue(new PushQueueItem {
                        UserId = pf?.UserCreatorID ?? model.SelerId,
                        Title = title,
                        Body = $"{title} شماره {model.SerialNumber} برای مشتری {cusTxt} ثبت شد.",
                        Url = "/Email/Index",
                        Tag = "f" + model.SerialNumber
                    });
                } else
                {
                    await _logSystemService.LogChange(oldModel, model, LogActionEnum.ویرایش, user, ["Descriptions", "FactorItems"]);
                }

                if (deal != null)
                {
                    deal.ModifyDate = DateTime.Now;
                    deal.ModifyDateTxt = dtService.GetDateTimePersian();
                }

                await db.SaveChangesAsync();
                await transAction.CommitAsync();
                return Ok(new { status = 200 });
            }
            catch (Exception e)
            {
                await transAction.RollbackAsync();
                return BadRequest(e);
            }
        }

        [Authorize]
        public async Task<IActionResult> UtaminReg(Guid pfId, Guid usId)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == pfId).FirstOrDefaultAsync();
            var pf = await db.PreFactors.Where(f => f.Id == pfId).Include(f => f.CustomerEntity).FirstOrDefaultAsync();
            var userTamin = await udb.Users.AsNoTracking().Where(f => f.Id == usId.ToString()).FirstOrDefaultAsync();

            pf.PrePftype = pf.Pftype;
            pf.Pftype = (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین;
            pf.PftypeTxt = Service.GetTypeTxtPfactor(pf.Pftype);

            pf.UserTaminId = usId;
            pf.UserTaminTxt = userTamin.FullName;
            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();
            pf.UserModify = user.FullName;
            pf.UserModifyID = user.Id;

            pf.CommentText = null;

            var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();

            if (setting.SetAmountByRoleType != SetAmountRoleEnum.فروش)
            {
                pf.TaminStep = TaminStepEnum.تعیین_مبلغ_فروش;
                pf.TaminStepTxt = nameof(TaminStepEnum.تعیین_مبلغ_فروش).Replace("_", " ");
            }

            await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user);

            await db.SaveChangesAsync();
            await smsServ.SendAcceptPfactorToTamin(userTamin.PhoneNumber, pf.SerialNumber);

            await hub.Clients.Groups("کارشناس تامین", "مدیر ارشد").SendAsync("countPreFactorToAccept");
            var cusTxt = pf.CustomerEntity != null ? $"{pf.CustomerEntity.CompanyName} ({pf.CustomerEntity.AgentName})".Trim() : pf.AgentName;

            await hub.Clients.User(pf.UserTaminId.ToString()).SendAsync("notifActionRequired",
                $"پیش فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusTxt} توسط <b>{pf.UserCreator}</b> جهت تایید در کارتابل شما قرار گرفت",
                "/Cartable/Index?tab=TTaminExpertId"
            );

            pushQueue.Enqueue(new PushQueueItem {
                UserId = pf.UserTaminId.ToString(),
                Title = "تایید پیش‌فاکتور",
                Body = $"پیش‌فاکتور {pf.SerialNumber} برای تایید در کارتابل شما قرار گرفت.",
                Url = "/Cartable/Index?tab=TTaminExpertId",
                Tag = "pf" + pf.SerialNumber
            });
            return Ok();
        }

        [Authorize]
        public async Task<IActionResult> ResendRegUtamin(Guid pfId, Guid usId)
        {
            var pf = await db.PreFactors.AsNoTracking().Where(f => f.Id == pfId).Include(f => f.CustomerEntity).FirstOrDefaultAsync();
            var userTamin = await udb.Users.AsNoTracking().Where(f => f.Id == usId.ToString()).FirstOrDefaultAsync();

            await smsServ.SendAcceptPfactorToTamin(userTamin.PhoneNumber, pf.SerialNumber);

            var cusTxt = pf.CustomerEntity != null ? $"{pf.CustomerEntity.CompanyName} ({pf.CustomerEntity.AgentName})".Trim() : pf.AgentName;
            await hub.Clients.User(pf.UserTaminId.ToString()).SendAsync("notifActionRequired",
                $"پیش فاکتور <b>{pf.SerialNumber}</b> برای مشتری {cusTxt} توسط <b>{pf.UserCreator}</b> جهت تایید در کارتابل شما قرار گرفت",
                "/Cartable/Index?tab=TTaminExpertId"
            );
            pushQueue.Enqueue(new PushQueueItem {
                UserId = pf.UserTaminId.ToString(),
                Title = "تایید پیش‌فاکتور",
                Body = $"پیش‌فاکتور {pf.SerialNumber} برای تایید در کارتابل شما قرار گرفت.",
                Url = "/Cartable/Index?tab=TTaminExpertId",
                Tag = "pf" + pf.SerialNumber
            });
            return Ok();
        }

        [Authorize]
        public async Task<PreFactor> checkPrefactor(PreFactor model)
        {
            if (model.Id == Guid.Empty)
            {
                var set = await db.ASettings.FirstOrDefaultAsync();
                if (set.SmallOffice)
                {
                    model.PrePftype = (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری;
                    model.Pftype = (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری;
                } else
                {
                    model.PrePftype = (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین;
                    model.Pftype = (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین;
                }

                model.PftypeTxt = Service.GetTypeTxtPfactor(model.Pftype);
                model.Accepted = false;
            }


            if (!model.Accepted && model.ValidityDateMiladi < DateTime.Parse(DateTime.Now.ToShortDateString()))
            {
                model.PrePftype = model.Pftype;
                model.Pftype = (int)PreFactorEnum.فاقد_اعتبار;
                model.PftypeTxt = Service.GetTypeTxtPfactor(model.Pftype);
                model.Accepted = false;
            }


            return model;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> getItemsDescription()
        {
            try
            {
                var items = await db.BaseDescriptions.OrderBy(f => f.Order).Select(f => new { f.Text, f.Official, f.Type }).ToListAsync();
                return Ok(items);
            }
            catch
            {
                return Ok();
            }
        }

        [Authorize(Roles = "مدیر ارشد,مدیر مالی,کارشناس مالی")]
        [HttpGet]
        public async Task<IActionResult> DelFactor(Guid id)
        {
            AppUser user = await userManager.GetUserAsync(HttpContext.User);
            var item = await db.Factors.Where(f => f.Id == id).FirstOrDefaultAsync();

            await _logSystemService.LogChange(item, null, LogActionEnum.حذف, user);

            item.IsDeleted = true;
            item.UserModify = user.FullName;
            item.UserModifyID = user.Id;
            item.ModifyDate = DateTime.Now;
            item.ModifyDateTxt = dtService.GetDateTimePersian();

            if (item.ParentReturnedId != null)
            {
                var returnedF = await db.Factors.FirstOrDefaultAsync(f => f.Id == item.ParentReturnedId);
                returnedF?.Returned = false;
            }

            var ac = await db.Accountancies.Where(f => f.fid == id).FirstOrDefaultAsync();
            if (ac != null)
                ac.IsDeleted = true;
            await db.SaveChangesAsync();
            return Ok();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> DelPreFactor(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var pf = await db.PreFactors.Where(f => f.Id == id).FirstOrDefaultAsync();

            await genService.DeletePreFactor(pf, user);
            await db.SaveChangesAsync();
            return Ok();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPFactor(Guid id)
        {
            var data = await db.PreFactors
                .Include(f => f.PreFactorItems)
                .Include(f => f.Descriptions)
                .Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(data);
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPFactorItems(Guid id)
        {
            var preFactor = await db.PreFactors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);

            var data = await db.PreFactorItems.AsNoTracking()
                .Where(f => f.PrefactorId == id && f.IsInProduction)
                .Select(i => new {
                    preFactorItemId = i.Id,
                    i.Product,
                    i.ProductSmallName,
                    i.UnitTxt,
                    i.Unit,
                    i.ProductCode,
                    i.ProductGroup,
                    i.ProductGroupTxt,
                    weight = i.WeightCount
                })
                .ToListAsync();

            var model = new {
                Orders = data,
                CustomerTxt = $"{preFactor.Customer} {preFactor.AgentName}".Trim(),
                prefactorSer = preFactor.SerialNumber,
                preFactor.UserCreator,
                preFactor.UserTaminTxt,
                preFactor.UserTaminId,
                preFactor.CustomerCode,
                OrderDate = dtService.GetDatePersian(),
            };

            return Ok(new[] { model });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPFactorItemsGroupBySuppliers(Guid id)
        {
            try
            {
                var preFactor = await db.PreFactors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);


                var customerTxt = $"{preFactor.Customer} {preFactor.AgentName}".Trim();
                var prefactorSer = preFactor.SerialNumber;
                var userCreator = preFactor.UserCreator;
                var userTaminTxt = preFactor.UserTaminTxt;
                var userTaminId = preFactor.UserTaminId;
                var customerCode = preFactor.CustomerCode;
                var orderDate = dtService.GetDatePersian();

                var data = await (
                    from i in db.PreFactorItems.AsNoTracking()
                    join s in db.Supplies.AsNoTracking()
                        on i.Id equals s.PfItemId
                    where s.PfId == id && i.IsInProduction
                    group new { i, s } by new { s.SupplierId, s.SupplierTxt }
                    into g
                    select new {
                        g.Key.SupplierId,
                        g.Key.SupplierTxt,
                        CustomerTxt = customerTxt,
                        PrefactorSer = prefactorSer,
                        UserCreator = userCreator,
                        UserTaminTxt = userTaminTxt,
                        UserTaminId = userTaminId,
                        CustomerCode = customerCode,
                        OrderDate = orderDate,
                        Orders = g.Select(x => new {
                            preFactorItemId = x.i.Id,
                            x.i.Product,
                            x.i.ProductSmallName,
                            x.i.UnitTxt,
                            x.i.Unit,
                            x.i.ProductCode,
                            x.i.ProductGroup,
                            x.i.ProductGroupTxt,
                            x.s.Weight
                        }).ToList(),
                    }
                ).ToListAsync();

                return Ok(data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetSepidarFactors(int pfSerialNumber)
        {
            try
            {
                var setting = await db.ASettings.AsNoTracking().FirstOrDefaultAsync();
                if (setting.Sepidar == true)
                {
                    ViewBag.PassType = 2;
                    var sepidarFactors = await S1db.vwInvoice.AsNoTracking().Where(f => f.QuotationNumber == pfSerialNumber).ToListAsync();

                    return PartialView("~/Views/Customer/GetFactorsSepidar.cshtml", sepidarFactors);
                }

                return NoContent();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdateFactorFinancial([FromBody] FactorFinancialVModel model)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var sepidar = await S1db.vwInvoice.AsNoTracking()
                    .Where(f => f.InvoiceId == model.SepidarId)
                    .Select(f => new {
                        f.InvoiceId,
                        f.Number,
                        f.Date,
                        f.DateTXT,
                        f.CustomerPartyRef,
                        f.CustomerPartyName,
                    })
                    .FirstOrDefaultAsync();

                var oldFactor = await db.Factors.AsNoTracking().Where(f => f.Id == model.CrmFactorId).FirstOrDefaultAsync();
                var factor = await db.Factors.Where(f => f.Id == model.CrmFactorId).FirstOrDefaultAsync();
                factor.DateMiladi = sepidar.Date;
                factor.Date = sepidar.DateTXT;
                factor.SepidarId = sepidar.InvoiceId;
                factor.SnFinancial = sepidar.Number.ToString();
                factor.UserModify = user.FullName;
                factor.UserModifyID = user.Id;
                factor.ModifyDate = DateTime.Now;
                factor.ModifyDateTxt = dtService.GetDateTimePersian();


                var customer = await db.Customers.Where(f => f.Id == factor.CustomerId).FirstOrDefaultAsync();
                customer.FinancialCustomerId = sepidar.CustomerPartyRef;
                customer.FinancialCustomerName = sepidar.CustomerPartyName;

                await _logSystemService.LogChange(oldFactor, factor, LogActionEnum.ویرایش, user);
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UpdatePrefactorItems([FromBody] List<PreFactorItem> model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var setting = await db.ASettings.FirstOrDefaultAsync();

            var preFactorId = model.First()?.PrefactorId;
            var oldPreFactor = await db.PreFactors.AsNoTracking().Where(f => f.Id == preFactorId)
                .Include(f => f.PreFactorItems)
                .FirstOrDefaultAsync();
            var preFactor = await db.PreFactors.Where(f => f.Id == preFactorId).FirstOrDefaultAsync();

            preFactor.SumTotalAmount = model.Sum(f => f.TotalAmount) - preFactor.Discount;
            preFactor = Service.UpdateTaxAmountPreFactor(preFactor);


            if (setting.SetAmountByRoleType != SetAmountRoleEnum.فروش)
            {
                if (preFactor.TaminStep != TaminStepEnum.تایید_شده)
                {
                    preFactor.TaminStep = TaminStepEnum.تعیین_تامین_کننده;
                    preFactor.TaminStepTxt = nameof(TaminStepEnum.تعیین_تامین_کننده).Replace("_", " ");
                }
            }


            foreach (var item in model)
            {
                item.UserModify = user.FullName;
                item.UserModifyID = user.Id;
                item.ModifyDate = DateTime.Now;
                item.ModifyDateTxt = dtService.GetDateTimePersian();
            }

            db.PreFactorItems.UpdateRange(model);

            await _logSystemService.LogChange(oldPreFactor, preFactor, LogActionEnum.ویرایش, user, ["PreFactorItems"]);
            await db.SaveChangesAsync();

            //var updatedPreFactor = await db.PreFactors.Include(f => f.PreFactorItems).FirstOrDefaultAsync(f => f.Id == preFactorId);
            return Ok();
        }


        [HttpPost] // وقتی در تنظیمات "تایید مالی" فعال است
        public async Task<IActionResult> UploadAttachmentInvoice(List<Attachment> list, Guid pFactorId, int type) // 5:توسط مشتری / 6:توسط کارشناس
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var pf = await db.PreFactors.Where(f => f.Id == pFactorId).FirstOrDefaultAsync();
                if (pf == null) return NotFound("پیش فاکتور یافت نشد");

                var fileNameTag = type == 5 ? "_AcPfactorByCustomer_" : "_AcPfactorBySalesExpert_";

                foreach (var item in list)
                {
                    if (item.Id == Guid.Empty)
                    {
                        item.Id = Guid.NewGuid();
                        item.Filename = pf.SerialNumber + fileNameTag +
                                        dtService.GetDateTimeMilisecondsPersian().Replace(":", "").Replace("/", "").Replace(" ", "") +
                                        "_" + Guid.NewGuid().ToString().Substring(0, 4);

                        item.EntityId = pFactorId;
                        item.Url = fileSaver.SaveAcceptedPreFactorImg(item.File, item.Filename, item.fileExten);
                        item._AttachType = (_AttachType)type;
                        item._AttachTypeTxt = ((_AttachType)type).ToString();
                        item.AttachDate = DateTime.Now;
                        item.AttachDateTxt = dtService.GetDateTimePersian();
                        item.UserCreatorID = user?.Id;
                        item.UserCreator = user?.FullName;
                        db.Attachments.Add(item);
                        await _logSystemService.LogChange(item, null, LogActionEnum.ثبت, user, null,
                            "پیوست ضمیمه پیش پرداخت از طرف " + (type == 5 ? "مشتری" : "کارشناس"));

                        await hub.Clients.Group("کارشناس مالی").SendAsync("notifActionRequired",
                            $"<b>واریزی</b> برای پیش‌فاکتور <b>{pf.SerialNumber}</b> توسط <b>{(type == 5 ? "مشتری" : pf.UserCreator)}</b> پیوست شد و جهت ثبت در کارتابل شما قرار گرفت",
                            "/Cartable/Index?tab=TFinancialExpertId"
                        );
                    } else if (item.IsDeleted)
                    {
                        await _logSystemService.LogChange(item, null, LogActionEnum.حذف, user, null,
                            "پیوست ضمیمه پیش پرداخت از طرف " + (type == 5 ? "مشتری" : "کارشناس"));
                        db.Attachments.Update(item);
                    }
                }


                if (pf.Pftype > (int)PreFactorEnum.در_انتظار_تایید_مالی)
                {
                    pf.PrePftype = pf.Pftype;
                    pf.Pftype = (int)PreFactorEnum.در_انتظار_تایید_مالی;
                    pf.AcceptedFinancial = null;
                    pf.PftypeTxt = Service.GetTypeTxtPfactor(pf.Pftype);
                }

                await db.SaveChangesAsync();

                if (pf.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی)
                {
                    await hub.Clients.Group("کارشناس مالی").SendAsync("countPreFactorToAccept");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        private readonly Dictionary<int, (string Title, string Message)> errorsDict = new() {
            { 11, ("هشدارهای سیستم", "تمام هشدارهای سیستم را در بخش هشدارها بررسی و رفع نمایید.") },
            { 21, ("بهای تمام شده", "بهای تامین این پیش‌فاکتور ثبت نشده است.") },
            { 22, ("بهای تمام شده", "بهای تامین این پیش‌فاکتور به صورت ناقص ثبت شده است.") },
            { 31, ("واریزی", "پرداختی برای این پیش‌فاکتور ثبت نشده است.") },
            { 32, ("واریزی", "تمام واریزی های این پیش‌فاکتور تایید نشده اند.") },
        };

        private ErrorVModel MapError(int code)
        {
            if (errorsDict == null || !errorsDict.TryGetValue(code, out var err))
                return null;

            return new ErrorVModel {
                Code = code,
                Title = err.Title,
                Message = err.Message
            };
        }

        public async Task<IActionResult> IsAllowedToAddFactor(Guid pfId)
        {
            var errorList = new List<ErrorVModel>();

            var hasAnyWarnings = await db.SystemWarnings.AnyAsync();
            if (hasAnyWarnings)
            {
                errorList.Add(MapError(11));
            }

            var supplies = await db.Supplies
                .Where(f => f.PfId == pfId && f.SupplyType == SupplyTypeEnum.هزینه_تولید)
                .GroupBy(g => g.PfItemId)
                .ToListAsync();
            if (supplies.Count == 0)
            {
                errorList.Add(MapError(21));
            } /*else
            {
                var pfItemsCount = await db.PreFactors.Where(f => f.Id == pfId).SumAsync(f => f.PreFactorItems.Count);
                if (pfItemsCount != supplies.Count)
                {
                    errorList.Add(MapError(22));
                }
            }*/

            var accS = db.Accountancies.Where(a => a.pfid == pfId && a.AccType == AcEnum.واریزی && a.InvoiceType == InvoiceTypeEnum.پیش_فاکتور);
            var hasAnyAcc = await accS.AnyAsync();
            if (!hasAnyAcc)
            {
                errorList.Add(MapError(31));
            } else
            {
                var hasAnyNotAcceptedAcc = await accS.AnyAsync(f => f.Status != AccStatusEnum.تایید_شده);
                if (hasAnyNotAcceptedAcc)
                {
                    errorList.Add(MapError(32));
                }
            }


            return errorList.Count > 0 ? Ok(new ReturnVModel { Status = false, Errors = errorList }) : Ok();
        }


        public async Task<IActionResult> GetAllPreFactorList(string search = "")
        {
            try
            {
                var query = db.PreFactors
                    .OrderByDescending(f => f.CreateDate)
                    .AsNoTracking()
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        f.Customer.Contains(search) ||
                        f.AgentName.Contains(search) ||
                        f.SerialNumber == TextService.convertToInt(search) ||
                        f.CustomerCode == TextService.convertToInt(search)
                    );
                } else
                {
                    query = query.Take(50);
                }

                var preFactors = await query.ToListAsync();
                return PartialView(preFactors);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}