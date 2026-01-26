using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.AccountanciesVm;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Accountency;
using CRM.EndPoint.FrontEnd.Services;
using CRM.EndPoint.FrontEnd.Services.Factor;
using CRM.EndPoint.FrontEnd.Services.SaveChangeCustom;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CRM.Core.Domain;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.LogSystem;
using CRM.EndPoint.FrontEnd.Models.Payment;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.EndPoint.FrontEnd.Services.TaskServices;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CRM.EndPoint.FrontEnd.Controllers {
    public class AccountancyController : Controller {
        private readonly Service genService;
        private readonly FileSaver fileSaver;
        private readonly DateTimeServices dtService;
        private readonly CRMContextDb db;
        private readonly UserContextDb udb;
        private readonly TaskServices _ts;
        private readonly IHubContext<TaskHub> hub;
        private readonly IVeiwRenderService _vrs;
        private readonly LogSystemService _logSystemService;
        private readonly CrmSaveCahngeAsync crmSaveCahngeAsync;
        private readonly SmsServices smsServ;
        private readonly UserManager<AppUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public AccountancyController(CrmSaveCahngeAsync crmSaveCahngeAsync, SmsServices SmsServ, UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager, Service GenService, FileSaver fileSaver, DateTimeServices dtService, CRMContextDb db,
            UserContextDb Udb, TaskServices ts, IHubContext<TaskHub> hub, IVeiwRenderService vrs, LogSystemService logSystemService)
        {
            this.crmSaveCahngeAsync = crmSaveCahngeAsync;
            smsServ = SmsServ;
            this.userManager = userManager;
            this.roleManager = roleManager;
            genService = GenService;
            this.fileSaver = fileSaver;
            this.dtService = dtService;
            this.db = db;
            udb = Udb;
            _ts = ts;
            this.hub = hub;
            _vrs = vrs;
            _logSystemService = logSystemService;
        }

        [Authorize(Policy = "NotCustomerOnly")]
        public IActionResult Index()
        {
            //try {

            //var pf = db.PreFactors.Where(f => f.Accepted).ToListAsync().Result;

            //foreach (var model in pf)
            //{


            //    Accountancy acD = new Accountancy()
            //    {
            //        FirstAcc = true,
            //        InvoiceType = (int)InvoiceTypeEnum.پیش_فاکتور,
            //        InvoiceTypeTxt = InvoiceTypeEnum.پیش_فاکتور.ToString(),
            //        AccType = (int)AcEnum.بدهکاری,
            //        AccTypeTxt = AcEnum.بدهکاری.ToString(),
            //        CustomerId = Guid.Parse(model.CustomerId.ToString()),
            //        debit = model.SumTotalAmount + model.TotalTax - model.Discount,
            //        tax = model.TotalTax,
            //        discount = model.Discount,
            //        date = model.AcceptedDate ?? DateTime.Now,
            //        dateSh = dtService.GetDateTimePersian(model.AcceptedDate ?? DateTime.Now),
            //        pfid = model.Id,
            //        pfSerialNumber = model.SerialNumber
            //    };

            //    var dt = DateTime.Parse("2024-03-20");
            //    if (model.CreateDate < dt && model.OfficialType == 1)
            //    {
            //        acD.taxPercent = 9;
            //    }
            //    else if (model.CreateDate > dt && model.OfficialType == 1)
            //    {
            //        acD.taxPercent = 10;
            //    }
            //    else
            //    {
            //        acD.taxPercent = 0;
            //    }
            //    db.Accountancies.Add(acD);
            //    db.SaveChanges();
            //}


            //------------

            //    int ser = 1;

            //    var ac = db.Accountancies.OrderBy(f => f.date).ToList();
            //    foreach (var item in ac.OrderBy(f => f.date))
            //    {

            //        item.SerialNum = ser++;


            //    }

            //    db.SaveChanges();

            //}
            //catch
            //{

            //}


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
            //var user = await userManager.GetUserAsync(HttpContext.User);
            if (type == 1 || type == 11 || type == 12)
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

            if (type == 2)
            {
                var item = await db.Factors.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();


                var ac = await db.Accountancies.Where(f => f.fid == item.Id && f.AccType == AcEnum.واریزی && f.InvoiceType == InvoiceTypeEnum.فاکتور)
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

            if (type == 3)
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

            if (type == 4)
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


            return null;
        }

        public async Task<IActionResult> GetAllPaymentCartablePartial(string serVal, CartableEnum cartable, int type = 1)
        {
            try
            {
                var user = await userManager.GetUserAsync(HttpContext.User);
                var roles = await userManager.GetRolesAsync(user);

                var query = db.Accountancies.ForPreFactor().AsQueryable();
                if (cartable == CartableEnum.مدیر_عامل)
                {
                    query = query.OnlyManagingDirector();
                }

                var pendingCount = await query.Where(f => f.Status == AccStatusEnum.در_دست_تایید || f.Status == AccStatusEnum.ثبت_اولیه).CountAsync();

                query = type switch {
                    1 => query.Where(f => f.Status == AccStatusEnum.در_دست_تایید || f.Status == AccStatusEnum.ثبت_اولیه),
                    2 => query.Where(f => f.Status == AccStatusEnum.تایید_شده),
                    3 => query.Where(f => f.Status == AccStatusEnum.رد_شده),
                    _ => query
                };

                if (!string.IsNullOrWhiteSpace(serVal))
                {
                    var serNum = TextService.convertToInt(serVal);
                    query = query.Where(f =>
                        f.pfSerialNumber == serNum
                        || f.SerialNum == serNum
                        || f.UserCreator.Contains(serVal)
                        || f.CustomerName.Contains(serVal)
                    );
                }

                var accS = await query.OrderByDescending(f => f.CreateDate).Take(100).ToListAsync();
                var count = accS.Count;


                var model = new PaymentPartialVModel {
                    Items = accS,
                    PassType = cartable == CartableEnum.مدیر_عامل ? 56 : 55,
                    UserId = user.Id,
                    IsAdmin = roles.Contains("مدیر ارشد"),
                    IsSalesAdmin = roles.Contains("مدیر فروش"),
                    IsManagingDirector = roles.Contains("مدیر عامل"),
                    IsTamin = roles.Contains("مدیر تامین") || roles.Contains("کارشناس تامین"),
                    IsFinancial = roles.Contains("کارشناس مالی") || roles.Contains("مدیر مالی"),
                    IsCustomer = roles.Contains("مشتری"),
                };


                var html = await _vrs.RenderToStringAsync("GetAllPaymentPartial", model);

                return Ok(new { html, count, pendingCount });
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
                var showNotAcceptedPfBtn = false;
                var showChangeStatusAllBtn = false;

                var model = new PaymentPartialVModel {
                    PassType = passType,
                    UserId = user.Id
                };

                var accQuery = db.Accountancies.AsNoTracking();
                switch (type)
                {
                    case 1: // تمام صورتحساب های پیش فاکتور
                    case 12: // کارتابل مالی
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

                if (accQuery.Any())
                {
                    model.IsAdmin = roles.Contains("مدیر ارشد");
                    model.IsSalesAdmin = roles.Contains("مدیر فروش");
                    model.IsManagingDirector = roles.Contains("مدیر عامل");
                    model.IsTamin = roles.Contains("مدیر تامین") || roles.Contains("کارشناس تامین");
                    model.IsFinancial = roles.Contains("کارشناس مالی") || roles.Contains("مدیر مالی");
                    model.IsCustomer = roles.Contains("مشتری");

                    if (type == 3) // deal
                    {
                        model.IsCreatorParent = deal?.UserCreatorID == user.Id;
                    } else if (type == 1 || type == 12) // pf
                    {
                        model.IsCreatorParent = pf?.UserCreatorID == user.Id;

                        if (type == 12)
                        {
                            var allAcceptedAcc = accQuery.All(f => f.Status == AccStatusEnum.تایید_شده);
                            var hasRejectedAcc = accQuery.Any(f => f.Status == AccStatusEnum.رد_شده);

                            showAcceptPfBtn =
                                pf?.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی
                                && allAcceptedAcc
                                && pf.AcceptedFinancial != true;

                            //showNotAcceptedPfBtn = hasRejectedAcc && pf?.AcceptedFinancial != false;
                        }

                        var isInvalid = pf?.Pftype is (int)PreFactorEnum.فاقد_اعتبار or (int)PreFactorEnum.بسته_شده
                            or (int)PreFactorEnum.حذف_شده or (int)PreFactorEnum.برگشت_خورده_از_طرف_مدیر_فروش;

                        showChangeStatusAllBtn =
                            passType != 5
                            && (model.IsCreatorParent || model.IsAdmin || model.IsSalesAdmin)
                            && accQuery.Any(f => f.Status == AccStatusEnum.ثبت_اولیه || f.Status == AccStatusEnum.رد_شده)
                            && !isInvalid;
                    }
                }

                var acc = await accQuery.ToListAsync();
                model.Items = acc;

                var html = await _vrs.RenderToStringAsync("GetAllPaymentPartial", model);
                var data = new { showAcceptPfBtn, showNotAcceptedPfBtn, showChangeStatusAllBtn, customerId };
                return Ok(new { data, html });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWalletPartialIfAny(Guid cusId)
        {
            try
            {
                var accList = await db.Accountancies.AsNoTracking().ForCustomer(cusId).Where(f => f.Wallet).ToListAsync();

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

                var dataF = db.Accountancies
                    .Where(f =>
                            f.FirstAcc == false
                        /*&& (
                            ((f.AccType == AcEnum.واریزی || f.AccType == AcEnum.برگشتی) && f.Accepted == true)
                            ||
                            (f.AccType == AcEnum.بدهکاری)
                        )*/
                    ).AsQueryable();

                if (!rols.Any(f => new[] { "مدیر ارشد", "مدیر فروش", "کارشناس مالی", "مدیر مالی", "مدیر تامین" }.Contains(f)))
                {
                    dataF = dataF.Where(f => f.UserCreatorID == user.Id);
                }

                if (modelF.filt != null)
                {
                    var fd = dtService.GetDateMiladi(modelF.filt.fromDate) ?? dtService.GetDateMiladi("1000/01/01");
                    var td = dtService.GetDateMiladi(modelF.filt.toDate) ?? DateTime.Now;

                    if (modelF.filt.CusFilterChd)
                        dataF = dataF.Where(f => f.CustomerId == modelF.filt.customerFilter).AsQueryable();
                    if (modelF.filt.DateFilterChk)
                        dataF = dataF.Where(f => f.CreateDate >= fd && f.CreateDate <= td).OrderByDescending(f => f.CreateDate).AsQueryable();
                    if (modelF.filt.SelFilterChd)
                        dataF = dataF.Where(f => f.UserCreatorID == modelF.filt.selFilter.ToString()).AsQueryable();
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


        public async Task<IActionResult> GetAllBankAccountJson()
        {
            try
            {
                var data = await db.BankAccounts
                    .Select(f => new SelectOptionGuidDto {
                        id = f.Id,
                        text = f.AccountName + " (" + f.BankName + " " + f.AccountNumber + ")",
                    })
                    .OrderByDescending(f => f.text)
                    .ToListAsync();

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
                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int recordsTotal = 0;
                var dataF = db.BankAccounts.AsQueryable();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnDirection)))
                {
                    if (sortColumnDirection == "asc")
                    {
                        switch (sortColumn)
                        {
                            case "AccountName":
                                dataF = dataF.OrderBy(f => f.AccountName);
                                break;
                            default:
                                dataF = dataF.OrderBy(f => f.AccountName);
                                break;
                        }
                    } else
                    {
                        switch (sortColumn)
                        {
                            case "AccountName":
                                dataF = dataF.OrderByDescending(f => f.AccountName);
                                break;
                            default:
                                dataF = dataF.OrderByDescending(f => f.AccountName);
                                break;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    dataF = dataF.Where(m =>
                        m.AccountName.Contains(searchValue)
                    );
                }

                recordsTotal = dataF.Count();
                var data = await dataF.Skip(skip).Take(pageSize).ToListAsync();
                var jsonData = new { draw, recordsFiltered = recordsTotal, recordsTotal, data };
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
                var ba = await db.BankAccounts.FirstOrDefaultAsync(f => f.Id == model.Id);

                bool isChangeActive = false;
                if (model.AccountNumber == null && model.BankName == null)
                {
                    isChangeActive = true;
                    ba.Active = model.Active;
                } else
                {
                    db.Entry(ba).CurrentValues.SetValues(model);
                    db.Entry(ba).Property(f => f.CreateDate).IsModified = false;
                    db.Entry(ba).Property(f => f.CreateDateTxt).IsModified = false;
                }

                ba.UserModify = user.FullName;
                ba.UserModifyID = user.Id;
                ba.ModifyDate = DateTime.Now;
                ba.ModifyDateTxt = dtService.GetDateTimePersian();

                var action = isChangeActive ? LogActionEnum.تغییر_وضعیت : LogActionEnum.ویرایش;
                await _logSystemService.LogChange(oldBa, ba, action, user);
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
            try
            {
                var user = userManager.GetUserAsync(HttpContext.User).Result;
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                db.BankAccounts.Add(model);

                await _logSystemService.LogChange(model, null, LogActionEnum.ثبت, user);
                await db.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
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

        /*public async Task<IActionResult> ToggleAcceptAcc(Guid id, bool accepted)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldAcc = await db.Accountancies.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var acc = await db.Accountancies.Where(f => f.Id == id).FirstOrDefaultAsync();
            acc.Accepted = accepted;

            acc.UserModify = user.FullName;
            acc.UserModifyID = user.Id;
            acc.ModifyDate = DateTime.Now;
            acc.ModifyDateTxt = dtService.GetDateTimePersian();

            if (accepted)
            {
                var tasks = await _ts.TaskReciveAmountPrefctor(acc, (int)acc.InvoiceType);
                await db.Carizos.AddRangeAsync(tasks);
            } else
            {
                var oldCarizos = await db.Carizos.Where(f => f.EntityId == acc.Id).ToListAsync();
                if (oldCarizos.Count != 0)
                {
                    db.Carizos.RemoveRange(oldCarizos);
                }
            }

            var action = acc.Accepted switch {
                true => LogActionEnum.تایید_و_تغییر_وضعیت,
                false => LogActionEnum.رد_و_تغییر_وضعیت,
            };
            await _logSystemService.LogChange(oldAcc, acc, action, user);

            await db.SaveChangesAsync();
            return Ok();
        }*/

        [HttpPost]
        public async Task<IActionResult> AcceptAccountancy([FromQuery] AccStatusEnum status, [FromBody] IdDescriptionVModel model)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldAcc = await db.Accountancies.AsNoTracking().Where(f => f.Id == model.Id).FirstOrDefaultAsync();
            var acc = await db.Accountancies.Where(f => f.Id == model.Id).FirstOrDefaultAsync();

            acc.CommentText = model.Description;
            if (acc.PayType == PayTypeEnum.نقدی || acc.PayType == PayTypeEnum.تهاتری)
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
            }


            // ثبت کاریزو
            if (acc.PayType == PayTypeEnum.اعتباری || acc.PayType == PayTypeEnum.چکی || acc.PayType == PayTypeEnum.تهاتری)
            {
                if (status == AccStatusEnum.تایید_شده)
                {
                    var tasks = await _ts.TaskReciveAmountPrefctor(acc, acc.InvoiceType);
                    await db.Carizos.AddRangeAsync(tasks);
                } else
                {
                    var oldCarizos = await db.Carizos.Where(f => f.EntityId == acc.Id).ToListAsync();
                    if (oldCarizos.Count != 0)
                    {
                        db.Carizos.RemoveRange(oldCarizos);
                    }
                }
            }


            await db.SaveChangesAsync();
            await hub.Clients.Groups("کارشناس مالی", "مدیر عامل", "مدیر ارشد").SendAsync("countPreFactorToAccept");
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

            /*var oldPf = await db.PreFactors.AsNoTracking().Where(f => f.Id == pfId).FirstOrDefaultAsync();
            var pf = await db.PreFactors.Where(f => f.Id == pfId).Include(f => f.CustomerEntity).FirstOrDefaultAsync();
            var user = userManager.GetUserAsync(HttpContext.User).Result;

            var sendToFinancial = false;
            var sendToManagingDirector = false;
            var isRepaired = false;

            var allRoles = new List<string>();
            string notifTxt = pf.CustomerEntity != null
                ? $"مشتری {pf.CustomerEntity.CustomerCode} {pf.CustomerEntity.CompanyName} ({pf.CustomerEntity.AgentName})"
                : $"مشتری {pf.AgentName}";

            if (pf.AcceptedFinancial == false || pf.AcceptedManagingDirector == false)
            {
                isRepaired = true;

                if (pf.AcceptedFinancial == false && pf.AcceptedManagingDirector == false)
                {
                    pf.AcceptedFinancial = null;
                    pf.AcceptedManagingDirector = null;
                    if (accS.Any(a => a.PayType == PayTypeEnum.نقدی || a.PayType == PayTypeEnum.چکی))
                        sendToFinancial = true;
                    if (accS.Any(a => a.PayType == PayTypeEnum.اعتباری || a.PayType == PayTypeEnum.تهاتری))
                        sendToManagingDirector = true;
                } else if (pf.AcceptedFinancial == false)
                {
                    pf.AcceptedFinancial = null;
                    if (accS.Any(a => a.PayType == PayTypeEnum.نقدی || a.PayType == PayTypeEnum.چکی))
                        sendToFinancial = true;
                    else
                        sendToManagingDirector = true;
                } else
                {
                    pf.AcceptedManagingDirector = null;
                    if (accS.Any(a => a.PayType == PayTypeEnum.اعتباری || a.PayType == PayTypeEnum.تهاتری))
                        sendToManagingDirector = true;
                    else
                        sendToFinancial = true;
                }

                pf.CommentText = pf.CommentText + " " + "(اصـلاح شـده)";
            }

            foreach (var acc in accS)
            {
                acc.Status = true;

                if (acc.Accepted == false)
                    acc.Accepted = null;

                if ((acc.PayType == PayTypeEnum.اعتباری || acc.PayType == PayTypeEnum.تهاتری) && acc.Accepted == null)
                {
                    sendToManagingDirector = true;
                } else if ((acc.PayType == PayTypeEnum.نقدی || acc.PayType == PayTypeEnum.چکی) && acc.Accepted == null)
                {
                    sendToFinancial = true;
                }
            }


            if (sendToManagingDirector && sendToFinancial)
            {
                pf.AcceptedManagingDirector = null;
                pf.AcceptedFinancial = null;
                await NotifCartable("کارشناس مالی", "TFinancialExpertId")!;
                await NotifCartable("مدیر عامل", "TManagingDirectorId")!;
            } else if (sendToManagingDirector)
            {
                pf.AcceptedManagingDirector = null;
                await NotifCartable("مدیر عامل", "TManagingDirectorId")!;
            } else if (sendToFinancial)
            {
                pf.AcceptedFinancial = null;
                await NotifCartable("کارشناس مالی", "TFinancialExpertId")!;
            }

            if (pf.Pftype > (int)PreFactorEnum.در_انتظار_تایید_مالی)
            {
                pf.PrePftype = pf.Pftype;
            }

            pf.Pftype = (int)PreFactorEnum.در_انتظار_تایید_مالی;
            pf.FinancialStep = await genService.ChangeFinancialStep(pf, accS);
            pf.PftypeTxt = pf.FinancialStep?.ToString().Replace("_", " ") + (isRepaired ? " (اصلاح شده)" : "");
            pf.UserModify = user.FullName;
            pf.UserModifyID = user.Id;
            pf.ModifyDate = DateTime.Now;
            pf.ModifyDateTxt = dtService.GetDateTimePersian();

            await _logSystemService.LogChange(oldPf, pf, LogActionEnum.تغییر_وضعیت, user, null, "ارسال به مالی");

            await db.SaveChangesAsync();

            if (allRoles.Count != 0)
            {
                var countObj = await genService.GetCountEnvelopeObj();
                allRoles.Add(pf.UserCreatorID);
                await hub.Clients.Users(allRoles).SendAsync("countPreFactorToAccept", countObj);
            }

            return Ok();

            async Task NotifCartable(string role, string tab)
            {
                var users = (await userManager.GetUsersInRoleAsync(role)).Select(u => u.Id).ToArray();
                allRoles.AddRange(users);
                await hub.Clients.Users(users).SendAsync("notifInfo",
                    $"{(isRepaired ? "پیش‌فاکتور اصلاح‌شده" : "پیش‌فاکتور")} <b>{pf.SerialNumber}</b> مربوط به {notifTxt} " +
                    $"توسط <b>{user.FullName}</b> جهت تایید در کارتابل شما قرار گرفت.",
                    $"/Cartable/Index?tab={tab}");
            }*/
        }


        [HttpGet]
        public async Task<IActionResult> ChangeStatusAcc(Guid id)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var oldAcc = await db.Accountancies.AsNoTracking().Where(f => f.Id == id).FirstOrDefaultAsync();
            var acc = await db.Accountancies.Where(f => f.Id == id).FirstOrDefaultAsync();

            acc.Status = AccStatusEnum.در_دست_تایید;

            await _logSystemService.LogChange(oldAcc, acc, LogActionEnum.تغییر_وضعیت, user, null, "ارسال به مالی");
            await db.SaveChangesAsync();

            return Ok();
        }


        /*public PreFactor RepairPf(PreFactor pf)
        {
            try
            {
                if (pf.AcceptedFinancial == false && pf.AcceptedManagingDirector == false)
                {
                    pf.AcceptedFinancial = null;
                    pf.AcceptedManagingDirector = null;
                } else if (pf.AcceptedFinancial == false)
                {
                    pf.AcceptedFinancial = null;
                } else
                {
                    pf.AcceptedManagingDirector = null;
                }

                pf.CommentText = pf.CommentText + " " + "(اصـلاح شـده)";

                return pf;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }*/
    }
}