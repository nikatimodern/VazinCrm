using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.AccountanciesVm;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Accountency;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.SaveChangeCustom;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models.Payment;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using CRM.EndPoint.FrontEnd.Services.QueryExtensions;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.EndPoint.FrontEnd.Services.TaskServices;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CRM.EndPoint.FrontEnd.Controllers {
    public class AccountancyController : Controller {
        private readonly Service genService;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly TaskServices _ts;
        private readonly IHubContext<TaskHub> hub;
        private readonly IVeiwRenderService _vrs;
        private readonly LogSystemService _logSystemService;
        private readonly IPushQueue pushQueue;
        private readonly CrmSaveCahngeAsync crmSaveCahngeAsync;
        private readonly UserManager<AppUser> userManager;


        public AccountancyController(CrmSaveCahngeAsync crmSaveCahngeAsync, UserManager<AppUser> userManager, Service GenService,
            DateTimeServices dtService, CRMContextDb db, TaskServices ts, IHubContext<TaskHub> hub, IVeiwRenderService vrs,
            LogSystemService logSystemService,
            IPushQueue pushQueue)
        {
            this.crmSaveCahngeAsync = crmSaveCahngeAsync;
            this.userManager = userManager;
            genService = GenService;
            this.dtService = dtService;
            this.db = db;
            _ts = ts;
            this.hub = hub;
            _vrs = vrs;
            _logSystemService = logSystemService;
            this.pushQueue = pushQueue;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> getAccountancy(Guid id, bool parent = false)
        {
            var acc = parent
                ? await db.Accountancies.Where(f => f.ParentId == id).FirstOrDefaultAsync()
                : await db.Accountancies.Where(f => f.Id == id).FirstOrDefaultAsync();

            if (acc == null) return NotFound("رکوردی یافت نشد!");

            var _atach = await db.Attachments.Where(f => f.EntityId == acc.Id).ToListAsync();

            var model = new AccViewModel {
                ListAttach = _atach,
                AccData = acc
            };

            return Ok(model);
        }


        [HttpPost]
        public async Task<IActionResult> delAccountancy(Guid id)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            var acc = await db.Accountancies.Where(f => f.Id == id).FirstOrDefaultAsync();

            await _logSystemService.LogChange(acc, null, LogActionEnum.حذف, user);

            acc.IsDeleted = true;
            acc.UserModify = user.FullName;
            acc.UserModifyID = user.Id;
            acc.ModifyDate = DateTime.Now;
            acc.ModifyDateTxt = dtService.GetDateTimePersian();

            await crmSaveCahngeAsync.SaveChange();


            var carizo = await db.Carizos.Where(f => f.EntityId == id).ToListAsync();
            if (carizo.Any())
                db.Carizos.RemoveRange(carizo);

            await db.SaveChangesAsync();
            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> GetAllAmountPartial(int type, Guid? id)
        {
            switch (type)
            {
                case 1:
                case 11:
                case 12:
                {
                    var item = await db.PreFactors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                    if (item == null) return NotFound("پیش فاکتور یافت نشد");


                    var ac = db.Accountancies.AsNoTracking()
                        .Where(f =>
                            f.pfid == item.Id
                            && f.AccType == AcEnum.واریزی
                            && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                            && (
                                (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی) ||
                                ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && f.Vosoled == false)
                            )
                        ).Sum(f => f.credit) ?? 0;
                    item.ReciveAmount = ac;

                    var acReturn = db.Accountancies.AsNoTracking()
                        .Where(f =>
                            f.pfid == item.Id
                            && f.AccType == AcEnum.برگشتی
                            && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                            && (
                                (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی) ||
                                ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && f.Vosoled == false)
                            )
                        ).Sum(f => f.BackWard) ?? 0;

                    item.PaymentAmount = item.SumTotalAmount + item.TotalTax - item.Discount - (item.ReciveAmount - acReturn);
                    item.SumTotalAmountTax = item.SumTotalAmount + item.TotalTax - item.Discount;

                    var model = new PaymentListViewModel {
                        Discount = item.Discount,
                        PaymentAmount = item.PaymentAmount < 0 ? 0 : item.PaymentAmount,
                        ReciveAmount = item.ReciveAmount,
                        BackwardAmount = acReturn,
                        SumTotalAmount = item.SumTotalAmount,
                        SumTotalAmountTax = item.SumTotalAmountTax,
                        TotalTax = item.TotalTax
                    };

                    return PartialView(model);
                }
                case 2:
                {
                    var item = await db.Factors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();


                    var ac = await db.Accountancies
                        .Where(f => f.fid == item.Id && f.AccType == AcEnum.واریزی && f.InvoiceType == InvoiceTypeEnum.فاکتور)
                        .ToListAsync();
                    item.ReciveAmount = ac.Sum(f => f.credit).Value;
                    item.PaymentAmount = (item.SumTotalAmount + item.TotalTax - item.Discount - item.ReciveAmount);
                    item.SumTotalAmountTax = (item.SumTotalAmount + item.TotalTax - item.Discount);
                    var model = new PaymentListViewModel() {
                        Discount = item.Discount,
                        PaymentAmount = item.PaymentAmount,
                        ReciveAmount = item.ReciveAmount,
                        SumTotalAmount = item.SumTotalAmount,
                        SumTotalAmountTax = item.SumTotalAmountTax,
                        TotalTax = item.TotalTax
                    };

                    return PartialView(model);
                }
                case 3:
                {
                    var preFactors = await db.PreFactors.AsNoTracking().Where(f => f.DealId == id).ToListAsync();

                    var sumDiscount = preFactors.Sum(f => f.Discount);
                    var sumTotalAmount = preFactors.Sum(f => f.SumTotalAmount);
                    var sumTotalTax = preFactors.Sum(f => f.TotalTax);

                    var sumReceiveAmount = db.Accountancies.AsNoTracking()
                        .Where(f =>
                            f.DealId == id
                            && f.AccType == AcEnum.واریزی
                            && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                            && (
                                (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی) ||
                                ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && f.Vosoled == false)
                            )
                        )
                        .Sum(f => f.credit) ?? 0;

                    var sumAcReturn = db.Accountancies.AsNoTracking()
                        .Where(f =>
                            f.DealId == id
                            && f.AccType == AcEnum.برگشتی
                            && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                            && (
                                (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی) ||
                                ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && f.Vosoled == false)
                            )
                        )
                        .Sum(f => f.BackWard) ?? 0;

                    var sumPaymentAmount = sumTotalAmount + sumTotalTax - sumDiscount - (sumReceiveAmount - sumAcReturn);
                    var sumSumTotalAmountTax = sumTotalAmount + sumTotalTax - sumDiscount;

                    var model = new PaymentListViewModel() {
                        Discount = sumDiscount,
                        PaymentAmount = sumPaymentAmount < 0 ? 0 : sumPaymentAmount,
                        ReciveAmount = sumReceiveAmount,
                        BackwardAmount = sumAcReturn,
                        SumTotalAmount = sumTotalAmount,
                        SumTotalAmountTax = sumSumTotalAmountTax,
                        TotalTax = sumTotalTax
                    };

                    return PartialView(model);
                }
                case 4:
                {
                    var preFactors = await db.PreFactors.AsNoTracking().Where(f => f.CustomerId == id).ToListAsync();

                    var sumDiscount = preFactors.Sum(f => f.Discount);
                    var sumTotalAmount = preFactors.Sum(f => f.SumTotalAmount);
                    var sumTotalTax = preFactors.Sum(f => f.TotalTax);

                    var sumReceiveAmount = db.Accountancies.AsNoTracking()
                        .Where(f =>
                            f.CustomerId == id
                            && f.AccType == AcEnum.واریزی
                            && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                            && (
                                (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی) ||
                                ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && f.Vosoled == false)
                            )
                        ).Sum(f => f.credit) ?? 0;

                    var sumAcReturn = db.Accountancies.AsNoTracking()
                        .Where(f =>
                            f.CustomerId == id
                            && f.AccType == AcEnum.برگشتی
                            && f.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
                            && (
                                (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی) ||
                                ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && f.Vosoled == false)
                            )
                        ).Sum(f => f.BackWard) ?? 0;

                    var sumPaymentAmount = sumTotalAmount + sumTotalTax - sumDiscount - (sumReceiveAmount - sumAcReturn);
                    var sumSumTotalAmountTax = sumTotalAmount + sumTotalTax - sumDiscount;

                    var model = new PaymentListViewModel() {
                        Discount = sumDiscount,
                        PaymentAmount = sumPaymentAmount < 0 ? 0 : sumPaymentAmount,
                        ReciveAmount = sumReceiveAmount,
                        BackwardAmount = sumAcReturn,
                        SumTotalAmount = sumTotalAmount,
                        SumTotalAmountTax = sumSumTotalAmountTax,
                        TotalTax = sumTotalTax
                    };

                    return PartialView(model);
                }
                default:
                    return null;
            }
        }

        public async Task<IActionResult> GetAllPaymentCartablePartial(string serVal, CartableEnum cartable, int type = 1, PayTypeEnum? payType = null)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);

                var baseQuery = db.Accountancies
                    .AsNoTracking()
                    .Include(x => x.Deal)
                    .ForPreFactor()
                    .OrderByDescending(x => x.CreateDate)
                    .AsQueryable();

                if (cartable == CartableEnum.کارشناس_تامین)
                {
                    baseQuery = baseQuery.OnlyTamin();
                }

                if (payType.HasValue)
                {
/*                    baseQuery = payType is PayTypeEnum.اعتباری or PayTypeEnum.چکی
                        ? baseQuery.Where(f => f.PayType == payType.Value || f.ParentId.HasValue)
                        : baseQuery.Where(f => f.PayType == payType.Value);*/
                    baseQuery = baseQuery.Where(f => f.PayType == payType.Value);
                }


                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    var serNum = TextService.convertToInt(serVal);
                    baseQuery = baseQuery.Where(f =>
                        f.pfSerialNumber == serNum
                        || f.SerialNum == serNum
                        || f.UserCreator.Contains(serVal)
                        || f.CustomerName.Contains(serVal)
                    );
                }

                var filterCounts = new Dictionary<int, int> {
                    [-1] = await baseQuery.CountAsync(),
                    [1] = await baseQuery.Where(f => f.Status == AccStatusEnum.در_دست_تایید || f.Status == AccStatusEnum.ثبت_اولیه).CountAsync(),
                    [2] = await baseQuery.Where(f => f.Status == AccStatusEnum.تایید_شده).CountAsync(),
                    [3] = await baseQuery.Where(f => f.Status == AccStatusEnum.رد_شده).CountAsync(),
                };

                var query = type switch {
                    1 => baseQuery.Where(f => f.Status == AccStatusEnum.در_دست_تایید || f.Status == AccStatusEnum.ثبت_اولیه),
                    2 => baseQuery.Where(f => f.Status == AccStatusEnum.تایید_شده),
                    3 => baseQuery.Where(f => f.Status == AccStatusEnum.رد_شده),
                    _ => baseQuery
                };

                if (string.IsNullOrWhiteSpace(serVal))
                {
                    query = query.Take(100);
                }

                var accS = await query.ToListAsync();

                var model = new PaymentPartialVModel
                {
                    Items = accS,
                    PassType = cartable == CartableEnum.کارشناس_تامین ? 56 : 55,
                    UserId = user.Id
                };

                SetPaymentPermissions(model, roles, user);

                var html = await _vrs.RenderToStringAsync("GetAllPaymentPartial", model);

                return Ok(new { html, count = accS.Count, filterCounts });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAllPaymentPartial(int type, Guid id, int passType)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);

                Guid? customerId = null;
                PreFactor pf = null;
                Deal deal = null;

                var showAcceptPfBtn = false;
                var showChangeStatusAllBtn = false;

                var model = new PaymentPartialVModel {
                    PassType = passType,
                    UserId = user.Id
                };

                var accQuery = db.Accountancies
                    .AsNoTracking()
                    .Include(x => x.Deal)
                    .AsQueryable();
                
                switch (type)
                {
                    case 1: // تمام صورتحساب های پیش فاکتور
                        accQuery = accQuery.ForPreFactor(id);
                        pf = await db.PreFactors.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
                        customerId = pf?.CustomerId;
                        break;
                    case 2: // تمام صورتحساب های فاکتور
                        accQuery = accQuery.ForFactor(id);
                        break;
                    case 3: // تمام صورتحساب های سفارش
                        accQuery = accQuery.ForDeal(id);
                        deal = await db.Deals.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
                        customerId = deal?.CustomerId;
                        break;
                    case 4: // تمام صورتحساب های مشتری
                        accQuery = accQuery.ForCustomer(id).Where(f => f.Wallet == true);
                        break;
                }

                var acc = await accQuery.ToListAsync();
                model.Items = acc;

                if (acc.Count > 0)
                {
                    SetPaymentPermissions(model, roles, user);

                    if (type == 3) // deal
                    {
                        model.IsCreatorParent = deal?.UserCreatorID == user.Id;
                    } else if (type == 1) // pf
                    {
                        model.IsCreatorParent = pf?.UserCreatorID == user.Id;

                        showAcceptPfBtn =
                            (roles.Contains("مدیر ارشد") || roles.Contains("کارشناس مالی") || roles.Contains("مدیر مالی"))
                            && pf?.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی
                            && pf.AcceptedFinancial != true
                            && acc.All(f => f.Status == AccStatusEnum.تایید_شده);


                        var isInvalid = pf?.Pftype is (int)PreFactorEnum.فاقد_اعتبار or (int)PreFactorEnum.بسته_شده
                            or (int)PreFactorEnum.حذف_شده or (int)PreFactorEnum.برگشت_خورده_از_طرف_مدیر_فروش;

                        var hasPendingOrRejected = acc.Any(f => f.Status is AccStatusEnum.ثبت_اولیه or AccStatusEnum.رد_شده);

                        showChangeStatusAllBtn =
                            passType != 5
                            && (model.IsCreatorParent || model.IsAdmin || model.IsSalesAdmin)
                            && hasPendingOrRejected
                            && !isInvalid;
                    }
                }


                var html = await _vrs.RenderToStringAsync("GetAllPaymentPartial", model);
                var data = new { showAcceptPfBtn, showChangeStatusAllBtn, customerId };
                return Ok(new { data, html });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private static void SetPaymentPermissions(PaymentPartialVModel model, IList<string> roles, AppUser user)
        {
            model.IsAdmin = roles.Contains("مدیر ارشد");
            model.IsSalesAdmin = roles.Contains("مدیر فروش");
            model.IsManagingDirector = roles.Contains("مدیر عامل") || user.NormalizedUserName == "A.NIROMAND";
            model.IsTamin = roles.Contains("مدیر تامین") || roles.Contains("کارشناس تامین");
            model.IsFinancial = roles.Contains("کارشناس مالی") || roles.Contains("مدیر مالی");
            model.IsCustomer = roles.Contains("مشتری");
        }

        [HttpGet]
        public async Task<IActionResult> GetWalletPartialIfAny(Guid cusId)
        {
            try
            {
                var accList = await db.Accountancies.AsNoTracking()
                    .ForCustomer(cusId)
                    .NotVosoled()
                    .Where(f => f.Wallet)
                    .ToListAsync();

                if (accList.Count == 0)
                    return Ok(false);

                return PartialView("GetAllWalletPartial", accList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet]
        public async Task<IActionResult> AddInvoiceWalletToPf(Guid id, Guid pfId)
        {
            try
            {
                var acc = await db.Accountancies.Where(f => f.Id == id).FirstOrDefaultAsync();
                var pf = await db.PreFactors.AsNoTracking().Where(f => f.Id == pfId)
                    .Select(f => new { f.DealId, f.SerialNumber, f.DealSerialNumber }).FirstOrDefaultAsync();

                acc.Wallet = false;
                acc.pfid = pfId;
                acc.pfSerialNumber = pf.SerialNumber;
                acc.DealId = pf.DealId;
                acc.DealSerialNumber = pf.DealSerialNumber;

                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetAllAccount()
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var rols = await userManager.GetRolesAsync(user);
                AccfilterVModel modelF = JsonConvert.DeserializeObject<AccfilterVModel>(Request.Form["modelF"].FirstOrDefault());
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                var dataF = db.Accountancies.Where(f =>
                    f.FirstAcc == false &&
                    f.InvoiceType != InvoiceTypeEnum.تنخواه
                ).AsQueryable();

                if (!rols.Any(f => new[] { "مدیر ارشد", "مدیر فروش", "کارشناس مالی", "مدیر مالی", "مدیر تامین" }.Contains(f)))
                {
                    dataF = dataF.Where(f => f.UserCreatorID == user.Id);
                }

                if (modelF.filt != null)
                {
                    dataF = dataF.ApplyFilter(modelF, f => f.CustomerId, f => f.VosolDateM, f => f.UserCreatorID);
                }

                dataF = modelF.AccType switch {
                    1 => dataF.Where(f => f.AccType == AcEnum.واریزی).AsQueryable(),
                    2 => dataF.Where(f => f.AccType == AcEnum.بدهکاری).AsQueryable(),
                    3 => dataF.Where(f => f.AccType == AcEnum.برگشتی).AsQueryable(),
                    _ => dataF
                };

                dataF = modelF.PayType switch {
                    1 => dataF.Where(f => f.PayType == PayTypeEnum.نقدی).AsQueryable(),
                    2 => dataF.Where(f => f.PayType == PayTypeEnum.چکی).AsQueryable(),
                    3 => dataF.Where(f => f.PayType == PayTypeEnum.اعتباری).AsQueryable(),
                    4 => dataF.Where(f => f.PayType == PayTypeEnum.تهاتری).AsQueryable(),
                    _ => dataF
                };

                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(m =>
                        m.SerialNum.ToString() == searchValue ||
                        m.CustomerName.Contains(searchValue) ||
                        m.CustomerCode.ToString() == searchValue ||
                        m.BankAccountTxt.Contains(searchValue) ||
                        m.Beneficiary.Contains(searchValue) ||
                        m.ToName.Contains(searchValue) ||
                        m.credit.ToString() == searchValue ||
                        m.pfSerialNumber.ToString() == searchValue
                    ).AsQueryable();
                }


                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        dataF = sortColumn switch {
                            "UserCreator" => dataF.OrderBy(f => f.UserCreator),
                            "PfSerialNumber" => dataF.OrderBy(f => f.pfSerialNumber),
                            "BankAccountTxt" => dataF.OrderBy(f => string.IsNullOrEmpty(f.BankAccountTxt)).ThenBy(f => f.BankAccountTxt),
                            "Customer" => dataF.OrderBy(f => f.CustomerCode),
                            "VosolDate" => dataF.OrderBy(f => f.VosolDateM == null).ThenBy(f => f.VosolDateM),
                            "credit" => dataF.OrderBy(f => f.credit == null).ThenBy(f => f.credit),
                            _ => dataF.OrderBy(f => f.SerialNum)
                        };
                    } else
                    {
                        dataF = sortColumn switch {
                            "UserCreator" => dataF.OrderByDescending(f => f.UserCreator),
                            "PfSerialNumber" => dataF.OrderByDescending(f => f.pfSerialNumber),
                            "BankAccountTxt" => dataF.OrderBy(f => string.IsNullOrEmpty(f.BankAccountTxt)).ThenByDescending(f => f.BankAccountTxt),
                            "Customer" => dataF.OrderByDescending(f => f.CustomerCode),
                            "VosolDate" => dataF.OrderBy(f => f.VosolDateM == null).ThenByDescending(f => f.VosolDateM),
                            "credit" => dataF.OrderBy(f => f.credit == null).ThenByDescending(f => f.credit),
                            _ => dataF.OrderByDescending(f => f.SerialNum)
                        };
                    }
                }

                recordsTotal = dataF.Count();
                List<Accountancy> data;
                if (pageSize == -1)
                    data = await dataF.Skip(skip).ToListAsync();
                else
                    data = await dataF.Skip(skip).Take(pageSize).ToListAsync();

                var SCount = skip + 1;
                foreach (var acc in data)
                {
                    acc.Row = SCount++;
                    acc.credit = acc.credit ?? acc.debit ?? acc.BackWard;
                    if (acc.PayType is PayTypeEnum.اعتباری or PayTypeEnum.چکی && acc.Vosoled)
                    {
                        acc.PayTypeTxt += " (وصول شده)";
                    }
                }

                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };

                return Ok(jsonData);
            }
            catch
            {
                return null;
            }
        }


        public async Task<IActionResult> GetAllBankAccountJson(BankAccountTypeEnum? type = null)
        {
            try
            {
                var query = db.BankAccounts.OrderBy(f => f.AccountName).AsQueryable();

                if (type.HasValue)
                    query = query.Where(f => f.BankAccountType == type);


                var items = await query
                    .Select(f => new {
                        f.Id,
                        f.AccountName,
                        f.BankName,
                        f.AccountNumber,
                        f.ShebaNumber,
                        f.CartNumber,
                        f.BankAccountType,
                        f.OfficialType
                    })
                    .ToListAsync();

                var data = items.Select(f => new {
                    value = f.Id,
                    accountName = f.AccountName,
                    bankName = f.BankName,
                    accountNumber = f.AccountNumber,
                    shebaNumber = f.ShebaNumber,
                    cartNumber = f.CartNumber,
                    officialType = f.OfficialType,
                    type = f.BankAccountType
                });


                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> GetAllBankAccount()
        {
            try
            {
                var draw = Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();
                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();
                var model = JsonConvert.DeserializeObject<filterVModel>(Request.Form["model"].FirstOrDefault());
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;

                var dataF = db.BankAccounts.AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        dataF = sortColumn switch {
                            "AccountName" => dataF.OrderBy(f => f.AccountName),
                            _ => dataF.OrderBy(f => f.CreateDate)
                        };
                    } else
                    {
                        dataF = sortColumn switch {
                            "AccountName" => dataF.OrderByDescending(f => f.AccountName),
                            _ => dataF.OrderByDescending(f => f.CreateDate)
                        };
                    }
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(m => m.AccountName.Contains(searchValue));
                }

                if (model?._type != null && model._type != -1)
                {
                    dataF = dataF.Where(f => f.BankAccountType == (BankAccountTypeEnum)model._type);
                }

                if (pageSize != -1)
                    dataF = dataF.Skip(skip).Take(pageSize);

                var data = await dataF.ToListAsync();
                recordsTotal = dataF.Count();

                var jsonData = new {
                    draw,
                    recordsFiltered = recordsTotal,
                    recordsTotal,
                    data
                };
                return Ok(jsonData);
            }
            catch
            {
                return null;
            }
        }

        public async Task<IActionResult> GetBankAccount(Guid id)
        {
            var data = await db.BankAccounts.Where(f => f.Id == id).FirstOrDefaultAsync();
            return Ok(data);
        }

        [HttpPost]
        public async Task<IActionResult> UpBankAccount([FromBody] BankAccount model)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldBa = await db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);

                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dtService.GetDateTimePersian();

                db.BankAccounts.Update(model);

                await _logSystemService.LogChange(oldBa, model, LogActionEnum.ویرایش, user);
                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddBankAccount([FromBody] BankAccount model)
        {
            if (model == null) return BadRequest();

            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                model.BankAccountTypeTxt = model.BankAccountType.ToString().Replace("_", " ");
                await db.BankAccounts.AddAsync(model);

                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
                await db.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ChangeActiveBankAccount([FromBody] BankAccount model)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);

                var oldBa = await db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
                var ba = await db.BankAccounts.FirstOrDefaultAsync(f => f.Id == model.Id);

                ba.Active = model.Active;

                ba.UserModify = user.FullName;
                ba.UserModifyID = user.Id;
                ba.ModifyDate = DateTime.Now;
                ba.ModifyDateTxt = dtService.GetDateTimePersian();

                await _logSystemService.LogChange(oldBa, ba, LogActionEnum.تغییر_وضعیت, user);
                await db.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public async Task<IActionResult> DelBankAccountItem(Guid id)
        {
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;
                var bankAcc = await db.BankAccounts.Where(f => f.Id == id).FirstOrDefaultAsync();

                await _logSystemService.LogChange(bankAcc, null, LogActionEnum.حذف, user);

                bankAcc.IsDeleted = true;
                bankAcc.UserModify = user.FullName;
                bankAcc.UserModifyID = user.Id;
                bankAcc.ModifyDate = DateTime.Now;
                bankAcc.ModifyDateTxt = dtService.GetDateTimePersian();

                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }


        [HttpPost]
        public async Task<IActionResult> AcceptAccountancy([FromQuery] AccStatusEnum status, [FromBody] IdDescriptionVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldAcc = await db.Accountancies.AsNoTracking().FirstOrDefaultAsync(f => f.Id == model.Id);
            var acc = await db.Accountancies.FirstOrDefaultAsync(f => f.Id == model.Id);

            if (acc == null) return NotFound();

            acc.CommentText = model.Description;
            if (acc.PayType is PayTypeEnum.نقدی or PayTypeEnum.تهاتری)
            {
                acc.Vosoled = status == AccStatusEnum.تایید_شده;
            }

            acc.UserModify = user.FullName;
            acc.UserModifyID = user.Id;
            acc.ModifyDate = DateTime.Now;
            acc.ModifyDateTxt = dtService.GetDateTimePersian();
            acc.Status = status;

            if (status == AccStatusEnum.تایید_شده)
            {
                acc.AcceptedUser = user.FullName;
                acc.AcceptedUserId = user.Id;
            } else
            {
                acc.AcceptedUser = null;
                acc.AcceptedUserId = null;
            }

            var action = acc.Status switch {
                AccStatusEnum.تایید_شده => LogActionEnum.تایید_و_تغییر_وضعیت,
                AccStatusEnum.رد_شده => LogActionEnum.رد_و_تغییر_وضعیت,
                _ => LogActionEnum.تغییر_وضعیت
            };

            await _logSystemService.LogChange(oldAcc, acc, action, user);
            if (acc.Status == AccStatusEnum.رد_شده)
            {
                var message =
                    $"<p>{acc.AccTypeTxt} شماره {acc.SerialNum} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) با توضیحات زیر نیاز به اصلاح دارد:</p>";
                message += $"توضیحات: <u>'{acc.CommentText?.Trim().Replace("\n", " ")}'</u>";
                await genService.AddEmail(
                    $"{acc.AccTypeTxt} نیاز به اصلاح", message, user, acc.UserCreatorID, acc.UserCreator, EntityTypeEnum.Accountancy, acc.Id
                );
                pushQueue.Enqueue(new PushQueueItem {
                    UserId = acc.UserCreatorID,
                    Title = "واریزی نیاز به اصلاح",
                    Body = $"{acc.AccTypeTxt} مربوط به پیش‌فاکتور {acc.pfSerialNumber} نیاز به اصلاح دارد.",
                    Url = "/Email/Index",
                    Tag = "acc" + acc.SerialNum
                });
            }


            // ثبت کاریزو
            if (acc.PayType is PayTypeEnum.اعتباری or PayTypeEnum.چکی or PayTypeEnum.تهاتری)
            {
                if (status == AccStatusEnum.تایید_شده)
                {
                    await _ts.CreatePreFactorAccountancyTasks(acc);
                } else
                {
                    await _ts.RemoveAccountancyTasks(acc.Id);
                }
            }


            await db.SaveChangesAsync();
            await hub.Clients.Groups("کارشناس مالی", "مدیر تامین", "کارشناس تامین", "مدیر ارشد").SendAsync("countOfPendingCartable");
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> ChangeStatusAllAcc(Guid pfId)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var accS = await db.Accountancies.ForPreFactor(pfId).Where(f =>
                f.Status == AccStatusEnum.ثبت_اولیه || f.Status == AccStatusEnum.رد_شده
            ).ToListAsync();

            foreach (var acc in accS)
            {
                var oldAcc = await db.Accountancies.AsNoTracking().Where(f => f.Id == acc.Id).FirstOrDefaultAsync();
                acc.Status = AccStatusEnum.در_دست_تایید;

                await _logSystemService.LogChange(oldAcc, acc, LogActionEnum.تغییر_وضعیت, user, null, "ارسال به مالی");
            }

            await db.SaveChangesAsync();
            return Ok();
        }


        [HttpGet]
        public async Task<IActionResult> ChangeStatusAcc(Guid id)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var oldAcc = await db.Accountancies.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
                var acc = await db.Accountancies.Where(f => f.Id == id).FirstOrDefaultAsync();

                acc.Status = AccStatusEnum.در_دست_تایید;

                await _logSystemService.LogChange(oldAcc, acc, LogActionEnum.تغییر_وضعیت, user, null, "ارسال به مالی");
                await db.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}