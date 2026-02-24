using ClosedXML.Excel;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Cartable;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PreFactorVm;
using CRM.EndPoint.FrontEnd.Models.Factor;
using CRM.EndPoint.FrontEnd.Services;
using CRM.Infrastructures.Sql;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Enums;
using CRM.EndPoint.FrontEnd.Models.PreFactorVm;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;

// ReSharper disable EntityFramework.UnsupportedServerSideFunctionCall

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize]
    public class CartableController : Controller {
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly Sepidar1ContextDb s1Db;
        private readonly IPushQueue pushQueue;
        private readonly IPushService pushService;
        private readonly IVeiwRenderService vrs;
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly ASetting _setting;


        public CartableController(IVeiwRenderService vrs, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, Service GenService,
            FileSaver fileSaver, DateTimeServices dtService, CRMContextDb db, UserContextDb Udb, Sepidar1ContextDb s1db, IPushQueue pushQueue, IPushService pushService)
        {
            this.vrs = vrs;
            this.userManager = userManager;
            this.roleManager = roleManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            udb = Udb;
            s1Db = s1db;
            this.pushQueue = pushQueue;
            this.pushService = pushService;
            this._setting = db.ASettings.FirstOrDefault();
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public IActionResult Index()
        {
            ViewBag.SubTitle = "کارتابل من";
            ViewBag.Head = "Cartable";
            return View();
        }


        [Authorize(Policy = "NotCustomerOnly")]
        [HttpGet]
        public async Task<IActionResult> GetCountEnvelope()
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var countObj = await genService.GetCartableCountObj(user);
                var emailsCount = await genService.GetEmailsNotSeenCount(Guid.Parse(user.Id));

                var chatsCount = await db.MainChats
                    .Where(f => f.ToUserId == user.Id || f.FromUserId == user.Id)
                    .SumAsync(f => f.Chats.Count(f => !f.Read && f.UserCreatorID != user.Id));

                var data = new { countObj, emailsCount, chatsCount };
                return Ok(data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [HttpGet]
        public IActionResult GetCartableMali(string serVal)
        {
            return PartialView();
        }


        [HttpGet]
        public IActionResult GetItemsSeRegistered()
        {
            return PartialView();
        }

        [HttpPost]
        [Authorize(Roles = "کارشناس فروش ,مدیر ارشد ,مدیر مالی, کارشناس مالی,مدیر فروش,کارشناس تامین")]
        public async Task<IActionResult> PreFactorAcceptedPartial([FromBody] filterVModel model)
        {
            try
            {
                /*pushQueue.Enqueue(new PushQueueItem {
                    SendToAll = true,
                    Title = "سلام وزین پلاستی",
                    Body = "این یک پوش نوتیف تست جدید است، اینو فرستادم که در جریان اتفاقات ما باشی!",
                    Url = "/"
                });*/
                //await pushService.SendToAllAsync("سلام وزین پلاستی", "این یک پوش نوتیف تست است", "/", null);


                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);

                var pList = await GetPrefactors(model, user, roles);
                if (pList == null)
                    throw new Exception();


                ViewBag.UserId = user.Id;
                var vModel = new PrefactorViewModel {
                    data = pList,
                    UserTamin = null,
                    _Setting = _setting,
                    rols = roles,
                };

                var html = await vrs.RenderToStringAsync("PreFactorAcceptedPartial", vModel,
                    new Dictionary<string, object> {
                        { "UserId", ViewBag.UserId }
                    });

                return Ok(new { count = vModel.data?.Count ?? 0, html });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PrefactorExportToExcel(filterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            // خواندن بدنه درخواست
            string requestBody;
            using (var reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            var model1 = JsonSerializer.Deserialize<filterVModel>(requestBody);

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    //workbook.ColumnWidth = 30;
                    var worksheet = workbook.Worksheets.Add("PreFactors");
                    worksheet.Columns().AdjustToContents();
                    worksheet.RightToLeft = true;
                    var currentRow = 1;

                    worksheet.Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.LightGreen);
                    worksheet.Row(currentRow).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Value = "ردیف";
                    worksheet.Cell(currentRow, 2).Value = "شماره سریال";
                    worksheet.Cell(currentRow, 3).Value = "نوع";
                    worksheet.Cell(currentRow, 4).Value = "تاریخ صدور";
                    worksheet.Cell(currentRow, 5).Value = "تاریخ اعتبار";
                    worksheet.Cell(currentRow, 6).Value = "جمع کل";
                    worksheet.Cell(currentRow, 7).Value = "جمع کل با ارزش افزوده";
                    worksheet.Cell(currentRow, 8).Value = "مشتری";
                    worksheet.Cell(currentRow, 9).Value = "کارشناس فروش";
                    worksheet.Cell(currentRow, 10).Value = "وضعیت";
                    worksheet.Cell(currentRow, 11).Value = "تایید";
                    worksheet.Cell(currentRow, 12).Value = "صادراتی";
                    worksheet.Column(6).Style.NumberFormat.Format = "#,##0_-ريال";
                    worksheet.Column(7).Style.NumberFormat.Format = "#,##0_-ريال";
                    var preFactors = await GetPrefactors(model1, user, roles);
                    foreach (var item in preFactors)
                    {
                        currentRow++;
                        if (currentRow % 2 == 0)
                            worksheet.Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.LightCyan);
                        worksheet.Cell(currentRow, 1).Value = currentRow;
                        worksheet.Cell(currentRow, 2).Value = item.SerialNumber;
                        worksheet.Cell(currentRow, 3).Value = item.OfficialType == 1 ? "رسمی" : "غیر رسمی";
                        worksheet.Cell(currentRow, 4).Value = TextService.ToEnglishNumber(item.Date);
                        worksheet.Cell(currentRow, 5).Value = TextService.ToEnglishNumber(item.ValidityDade);
                        worksheet.Cell(currentRow, 6).Value = item.SumTotalAmount;
                        worksheet.Cell(currentRow, 7).Value = item.PaymentAmount;
                        worksheet.Cell(currentRow, 8).Value = item.Customer + " " + item.AgentName;
                        worksheet.Cell(currentRow, 9).Value = item.UserCreator;
                        worksheet.Cell(currentRow, 10).Value = item.PftypeTxt;
                        worksheet.Cell(currentRow, 11).Value = item.Accepted == true ? "بله" : "خیر";
                        worksheet.Cell(currentRow, 12).Value = item.Export == true ? "بله" : "خیر";
                    }

                    worksheet.Columns().AdjustToContents();
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(
                            content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "ManagementList.xlsx");
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> FactorExportToExcel(filterVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);
            // خواندن بدنه درخواست
            string requestBody;
            using (var reader = new StreamReader(Request.Body))
            {
                requestBody = await reader.ReadToEndAsync();
            }

            var model1 = JsonSerializer.Deserialize<filterVModel>(requestBody);

            try
            {
                using (var workbook = new XLWorkbook())
                {
                    //workbook.ColumnWidth = 30;
                    var worksheet = workbook.Worksheets.Add("PreFactors");
                    worksheet.Columns().AdjustToContents();
                    worksheet.RightToLeft = true;
                    var currentRow = 1;

                    worksheet.Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.LightGreen);
                    worksheet.Row(currentRow).Style.Font.Bold = true;
                    worksheet.Cell(currentRow, 1).Value = "ردیف";
                    worksheet.Cell(currentRow, 2).Value = "شماره سریال";
                    worksheet.Cell(currentRow, 3).Value = "نوع";
                    worksheet.Cell(currentRow, 4).Value = "تاریخ";
                    worksheet.Cell(currentRow, 5).Value = "شماره پیش فاکتور";
                    worksheet.Cell(currentRow, 6).Value = "جمع کل";
                    worksheet.Cell(currentRow, 7).Value = "جمع کل با ارزش افزوده";
                    worksheet.Cell(currentRow, 8).Value = "مشتری";
                    worksheet.Cell(currentRow, 9).Value = "کارشناس فروش";
                    worksheet.Cell(currentRow, 10).Value = "شماره فاکتور مالی";
                    worksheet.Cell(currentRow, 11).Value = "وضعیت";
                    worksheet.Cell(currentRow, 12).Value = "صادراتی";
                    worksheet.Cell(currentRow, 13).Value = "ثبت شده توسط";
                    worksheet.Column(6).Style.NumberFormat.Format = "#,##0_-ريال";
                    worksheet.Column(7).Style.NumberFormat.Format = "#,##0_-ريال";
                    var factors = await GetFactors(model1, user, roles);
                    foreach (var item in factors)
                    {
                        currentRow++;
                        if (currentRow % 2 == 0)
                            worksheet.Row(currentRow).Style.Fill.SetBackgroundColor(XLColor.LightCyan);
                        worksheet.Cell(currentRow, 1).Value = currentRow;
                        worksheet.Cell(currentRow, 2).Value = item.SerialNumber;
                        worksheet.Cell(currentRow, 3).Value = item.OfficialType == 1 ? "رسمی" : "غیر رسمی";
                        worksheet.Cell(currentRow, 4).Value = TextService.ToEnglishNumber(item.Date);
                        worksheet.Cell(currentRow, 5).Value = item.PfactorSerialNumber;
                        worksheet.Cell(currentRow, 6).Value = item.SumTotalAmount;
                        worksheet.Cell(currentRow, 7).Value = item.PaymentAmount;
                        worksheet.Cell(currentRow, 8).Value = item.Customer + " " + item.AgentName;
                        worksheet.Cell(currentRow, 9).Value = item.SelerFullName;
                        worksheet.Cell(currentRow, 10).Value = item.SnFinancial;
                        worksheet.Cell(currentRow, 11).Value = item.statusTxt;
                        worksheet.Cell(currentRow, 12).Value = item.Export == true ? "بله" : "خیر";
                        worksheet.Cell(currentRow, 13).Value = item.UserCreator;
                    }

                    worksheet.Columns().AdjustToContents();
                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();
                        return File(
                            content,
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            "ManagementList.xlsx");
                    }
                }
            }
            catch
            {
                return null;
            }
        }


        private async Task<IList<PreFactor>> GetPrefactors(filterVModel model, AppUser user, IList<string> roles)
        {
            try
            {
                var pListQ = db.PreFactors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(model.serVal))
                {
                    var ser = model.serVal;
                    int? serNum = TextService.convertToInt(ser);

                    pListQ = pListQ.Where(s =>
                        EF.Functions.Like(s.AgentName, "%" + ser + "%") ||
                        EF.Functions.Like(s.Customer, "%" + ser + "%") ||
                        s.SerialNumber == serNum ||
                        s.CustomerEntity.CustomerCode == serNum
                    );
                }

                if (!roles.Contains("مدیر ارشد") && !roles.Contains("مدیر فروش") && !roles.Contains("کارشناس مالی") && !roles.Contains("مدیر مالی"))
                {
                    pListQ = pListQ.Where(f => f.UserCreatorID == user.Id);
                }

                if (model.filt != null)
                {
                    pListQ = pListQ.ApplyFilter(model, f => f.CustomerId, f => f.DateMiladi, f => f.UserCreatorID);
                }

                pListQ = model._type switch {
                    5 => pListQ.Include(f => f.Factors).Where(f => !f.Factors.Any()), // بدون فاکتور
                    6 => pListQ.Include(f => f.Factors).Where(f => f.Factors.Any()), // دارای فاکتور
                    _ => pListQ
                };

                pListQ = pListQ
                    .OrderByDescending(f => f.DateMiladi)
                    .ThenByDescending(f => f.CreateDate)
                    .Include(f => f.CustomerEntity);

                if (model.filt == null)
                {
                    pListQ = pListQ.Take(50);
                }

                var pList = await pListQ.AsNoTracking().ToListAsync();


                var pfIds = pList.Select(f => f.Id).ToList();
                var accS = await db.Accountancies
                    .AsNoTracking()
                    .Where(f =>
                        f.pfid.HasValue
                        && pfIds.Contains(f.pfid.Value)
                        && (f.AccType == AcEnum.واریزی || f.AccType == AcEnum.برگشتی)
                        && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                        && (
                            (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی) ||
                            ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && f.Vosoled == false)
                        )
                    )
                    .ToListAsync();

                var accDict = accS
                    .GroupBy(a => a.pfid)
                    .ToDictionary(g => g.Key, g => g.Sum(f => f.AccType == AcEnum.واریزی ? f.credit : -f.BackWard) ?? 0);

                foreach (var item in pList)
                {
                    var rec = accDict.TryGetValue(item.Id, out var value) ? value : 0;

                    item.ReciveAmount = rec;
                    item.PaymentAmount = item.SumTotalAmount + item.TotalTax - item.Discount - rec;
                    item.SumTotalAmountTax = item.SumTotalAmount + item.TotalTax - item.Discount;
                }

                //اگر سپیدار در تنظیمات فعال بود
                if (_setting.Sepidar == true)
                {
                    var serNums = pList.Select(f => (int?)f.SerialNumber).ToList();

                    var invoices = s1Db.vwInvoice.Where(q => serNums.Contains(q.QuotationNumber)).ToList();

                    var pfFactors = await db.Factors
                        .Where(f => pfIds.Contains(f.PfactorID))
                        .GroupBy(f => f.PfactorID)
                        .ToDictionaryAsync(g => g.Key, g => g.Count());

                    foreach (var item in pList)
                    {
                        var inv = invoices.Where(f => f.QuotationNumber == item.SerialNumber && f.CreationDate >= item.CreateDate);

                        item.CountFactorSepidar = inv.Count();

                        item.CountFactor = pfFactors.TryGetValue(item.Id, out var countF) ? countF : 0;
                    }
                }
                // پایان سپیدار

                return pList;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<IList<Factor>> GetFactors(filterVModel model, AppUser user, IList<string> roles)
        {
            try
            {
                var fListQ = db.Factors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(model.serVal))
                {
                    var ser = model.serVal;
                    int? serNum = TextService.convertToInt(ser);

                    fListQ = fListQ.Where(s =>
                        EF.Functions.Like(s.AgentName, "%" + ser + "%")
                        || EF.Functions.Like(s.Customer, "%" + ser + "%")
                        || s.SerialNumber == serNum
                        || s.CustomerCode == serNum
                        || s.SnFinancial == ser
                    );
                }

                if (!roles.Any(new[] { "مدیر ارشد", "مدیر فروش", "کارشناس مالی", "مدیر مالی", "مشتری" }.Contains))
                {
                    fListQ = fListQ.Where(f => f.SelerId == user.Id);
                }

                if (model.filt != null)
                {
                    fListQ = fListQ.ApplyFilter(model, f => f.CustomerId, f => f.DateMiladi, f => f.SelerId);
                }

                fListQ = model._type switch {
                    1 => fListQ.Where(f => f.OfficialType == 1),
                    2 => fListQ.Where(f => f.OfficialType == 0),
                    _ => fListQ
                };

                if (!string.IsNullOrEmpty(model.EntityId))
                {
                    fListQ = fListQ.Where(f => f.PfactorID == Guid.Parse(model.EntityId)).AsQueryable();
                }

                fListQ = fListQ
                    .OrderByDescending(f => f.DateMiladi)
                    .ThenByDescending(f => f.CreateDate);

                if (model.filt == null)
                {
                    fListQ = fListQ.Take(50);
                }

                var fList = await fListQ.AsNoTracking().ToListAsync();
                return fList;
            }
            catch
            {
                return null;
            }
        }


        [HttpGet]
        [Authorize(Roles = "مدیر ارشد , کارشناس تامین , مدیر تامین")]
        public async Task<IActionResult> GetPreFactorTaminExpert(string serVal, int type)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);

                var model = new TaminViewModel {
                    UserId = user.Id,
                    Setting = _setting,
                    IsAdmin = rols.Contains("مدیر ارشد") || rols.Contains("مدیر تامین")
                };


                var query = db.PreFactors.AsQueryable();
                switch (type)
                {
                    case 1:
                        query = query.Where(s => s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین);
                        break;
                    default:
                        query = query.Where(s => s.Pftype > (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین);
                        break;
                }

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    var serialVal = TextService.convertToInt(serVal);
                    query = query.Where(s =>
                        EF.Functions.Like(s.AgentName, $"%{serVal}%") ||
                        EF.Functions.Like(s.Customer, $"%{serVal}%") ||
                        s.SerialNumber == serialVal
                    );

                } else if (!rols.Contains("مدیر ارشد") && !rols.Contains("مدیر تامین"))
                {
                    query = query.Where(f => f.UserTaminId == Guid.Parse(user.Id));
                }


                var preFactors = await query.OrderByDescending(f => f.ModifyDate).Take(100).Include(f => f.PreFactorItems).ToListAsync();

                var pfIds = preFactors.Select(f => f.Id).ToList();
                var supplies = await db.Supplies
                    .Where(f => pfIds.Contains(f.PfId))
                    .GroupBy(f => f.PfId)
                    .Select(f => new {
                        pfId = f.Key,
                        totalAmount = f.Sum(g => g.TotalAmount),
                        //totalWeight = f.Where(g => g.SupplyType == SupplyTypeEnum.هزینه_تولید).Sum(g => Convert.ToDecimal(g.Weight)),
                        totalItems = f.Where(g => g.SupplyType == SupplyTypeEnum.هزینه_تولید).GroupBy(g => g.PfItemId).Count()
                    })
                    .ToListAsync();


                foreach (var pf in preFactors)
                {
                    var s = supplies.FirstOrDefault(f => f.pfId == pf.Id);
                    if (s == null)
                    {
                        pf.NoSupply = true;
                        continue;
                    }

                    //var pfItemsWeight = pf.PreFactorItems.Sum(x => Convert.ToDecimal(x?.WeightCount));
                    var pfItemsCount = pf.PreFactorItems.Count;

                    pf.TotalSupplyAmount = s.totalAmount;

                    pf.SupplyMismatch = s.totalItems != pfItemsCount; //|| s.totalWeight != pfItemsWeight;
                }

                model.PreFactors = preFactors;


                var html = await vrs.RenderToStringAsync("GetPreFactorTaminExpert", model);
                return Ok(new { count = model.PreFactors.Count, html });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [HttpGet]
        [Authorize(Roles = "مدیر ارشد , مدیر مالی, کارشناس مالی")]
        public async Task<IActionResult> GetPreFactorFinancialExpert(string serVal, int type = 1)
        {
            try
            {
                var query = db.PreFactors.AsQueryable();

                switch (type)
                {
                    case 1:
                        query = query.Where(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی);
                        break;
                    case 2:
                        query = query.Where(f =>
                            f.Pftype == (int)PreFactorEnum.در_انتظار_تولید ||
                            f.Pftype == (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد ||
                            f.AcceptedFinancial == true
                        );
                        break;
                    case 3:
                        query = query.Where(f => f.AcceptedFinancial == false);
                        break;
                }

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    query = query.Where(f =>
                        f.AgentName.Contains(serVal) ||
                        f.Customer.Contains(serVal) ||
                        f.SerialNumber == TextService.convertToInt(serVal)
                    );
                }

                var result = await query
                    .OrderByDescending(g => g.ModifyDate)
                    .Take(100)
                    .Select(pf => new PreFactorFinancialExpertVModel {
                        PreFactor = pf,
                        ReceiveAmount =
                            db.Accountancies.AsNoTracking().ForPreFactor()
                                .NotVosoled()
                                .Where(a => a.pfid == pf.Id)
                                .Sum(f => f.AccType == AcEnum.واریزی ? f.credit : -f.BackWard) ?? 0,

                        NotAcceptedFiAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor().OnlyFinancial()
                                .Count(a => a.pfid == pf.Id && a.Status != AccStatusEnum.تایید_شده),

                        NotAcceptedMdAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor().OnlyManagingDirector()
                                .Count(a => a.pfid == pf.Id && a.Status != AccStatusEnum.تایید_شده),

                        TotalAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor()
                                .Count(a => a.pfid == pf.Id),
                    })
                    .ToListAsync();


                var html = await vrs.RenderToStringAsync("GetPreFactorFinancialExpert", result);
                return Ok(new { count = result.Count, html });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Authorize(Roles = "کارشناس تامین , مدیر تامین , مدیر ارشد , مدیر عامل")]
        public async Task<IActionResult> GetPreFactorManagingDirector(string serVal, int type = 1)
        {
            try
            {
                var query = db.PreFactors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    query = query.Where(f =>
                        f.AgentName.Contains(serVal) ||
                        f.Customer.Contains(serVal) ||
                        f.SerialNumber == TextService.convertToInt(serVal)
                    );
                }


                switch (type)
                {
                    case 1:
                        query = query.Where(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی && f.AcceptedManagingDirector == null)
                            .Where(pf =>
                                db.Accountancies.ForPreFactor().OnlyManagingDirector()
                                    .Any(a => a.pfid == pf.Id && a.Status != AccStatusEnum.ثبت_اولیه)
                            );

                        break;
                    case 2:
                        query = query.Where(f => f.AcceptedManagingDirector == true);
                        break;
                    case 3:
                        query = query.Where(f => f.AcceptedManagingDirector == false);
                        break;
                }


                var result = await query
                    .OrderByDescending(g => g.CreateDate)
                    .Take(100)
                    .Select(pf => new PreFactorFinancialExpertVModel {
                        PreFactor = pf,
                        ReceiveAmount =
                            db.Accountancies.AsNoTracking().ForPreFactor()
                                .NotVosoled()
                                .Where(a => a.pfid == pf.Id)
                                .Sum(f => f.AccType == AcEnum.واریزی ? f.credit : -f.BackWard) ?? 0,

                        NotAcceptedMdAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor().OnlyManagingDirector()
                                .Count(a => a.pfid == pf.Id && a.Status != AccStatusEnum.تایید_شده),

                        TotalAccCount =
                            db.Accountancies.AsNoTracking().ForPreFactor().OnlyManagingDirector()
                                .Count(a => a.pfid == pf.Id),
                    })
                    .ToListAsync();

                var html = await vrs.RenderToStringAsync("GetPreFactorManagingDirector", result);
                return Ok(new { count = result.Count, html });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSelesExpertPartial()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var data = db.PreFactors.Where(f => f.UserCreatorID == user.Id).ToList();
            return PartialView(data);
        }

        [HttpGet]
        [Authorize(Roles = "مدیر ارشد , مدیر فروش")]
        public async Task<IActionResult> GetPreFactorSalesAdmin(string serVal, int type)
        {
            try
            {
                var query = db.PreFactors.AsQueryable();

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    var serialVal = TextService.convertToInt(serVal);
                    query = query.Where(s =>
                        EF.Functions.Like(s.AgentName, $"%{serVal}%") ||
                        EF.Functions.Like(s.Customer, $"%{serVal}%") ||
                        s.SerialNumber == serialVal);
                }

                switch (type)
                {
                    case -1:
                        query = query.Where(s =>
                            s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش
                            || s.Pftype == (int)PreFactorEnum.در_انتظار_تولید
                            || s.Pftype == (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد
                            || s.Pftype == (int)PreFactorEnum.بسته_شده
                        );
                        break;
                    case 1:
                        query = query.Where(s => s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش);
                        break;
                    case 2:
                    {
                        query = query.Where(s =>
                            s.Pftype == (int)PreFactorEnum.در_انتظار_تولید
                            || s.Pftype == (int)PreFactorEnum.جنس_به_مشتری_تحویل_داده_شد
                            || s.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی
                        );
                        break;
                    }
                }


                var pList = await (
                        from pf in query
                        let receiveAmount = db.Accountancies.ForPreFactor().NotVosoled()
                            .Where(a => a.pfid == pf.Id)
                            .Sum(f => f.AccType == AcEnum.واریزی ? (f.credit ?? 0) : -(f.BackWard ?? 0))
                        select new PreFactorSalesAdminViewModel {
                            PreFactor = pf,
                            ReceiveAmount = receiveAmount,
                            SumTotalAmountTax = pf.SumTotalAmount + pf.TotalTax - pf.Discount,
                            PaymentAmount = pf.SumTotalAmount + pf.TotalTax - pf.Discount - receiveAmount
                        })
                    .OrderByDescending(f => f.PreFactor.AcceptedFinancial == false)
                    .ThenByDescending(f => f.ReceiveAmount != 0)
                    .ThenByDescending(f => f.PreFactor.ModifyDate)
                    .Take(100)
                    .ToListAsync();


                var html = await vrs.RenderToStringAsync("GetPreFactorSalesAdmin", pList);
                return Ok(new { count = pList.Count, html });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> FactorPartial([FromBody] FactorVmodel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var rols = await userManager.GetRolesAsync(user);

            var fList = await GetFactors(model.fmodel, user, rols);

            var vModel = new FactorsViewModel {
                factors = fList,
                Rols = rols,
                _Setting = _setting
            };

            if (rols.Contains("مدیر فروش") || rols.Contains("کارشناس مالی"))
                vModel.Financial = true;

            var html = await vrs.RenderToStringAsync("FactorPartial", vModel,
                new Dictionary<string, object> {
                    { "PassType", model.PassType } //passType: (0:PreFactor,Factor | 1:CustomerProfile | 5:Supply | 4:Deal | 6:Email)
                });

            return Ok(new { count = vModel.factors?.Count ?? 0, html });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFactorsPartialModal(Guid id, int type)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);
                ViewBag.PassType = type; //passType: (0:PreFactor,Factor | 1:CustomerProfile | 5:Supply | 4:Deal | 6:Email)

                var fList = type switch {
                    4 => await db.Factors.Where(f => f.DealId == id).ToListAsync(),
                    5 => await db.Factors.Where(f => f.PfactorID == id).ToListAsync(),
                    6 => await db.Factors.Where(f => f.Id == id).ToListAsync(),
                    _ => null
                };

                var model = new FactorsViewModel {
                    factors = fList,
                    Rols = rols,
                    _Setting = _setting
                };

                var html = await vrs.RenderToStringAsync("FactorPartial", model,
                    new Dictionary<string, object> {
                        { "PassType", ViewBag.PassType }
                    }
                );

                return Ok(new { count = fList?.Count ?? 0, html });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}