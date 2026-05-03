using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
using CRM.Core.Domain.Emails.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Enum;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.LogSystem;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Customer;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.Framework.Domain;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;


namespace CRM.EndPoint.FrontEnd.Services {
    public class Service : Controller {
        private readonly CRMContextDb db;
        private readonly DateTimeServices dtService;
        private readonly LogSystemService logSystemService;
        private readonly UserManager<AppUser> userManager;
        private readonly IHubContext<TaskHub> hub;

        public Service(CRMContextDb _db, DateTimeServices dtService, LogSystemService logSystemService, UserManager<AppUser> userManager,
            IHubContext<TaskHub> hub)
        {
            db = _db;
            this.dtService = dtService;
            this.logSystemService = logSystemService;
            this.userManager = userManager;
            this.hub = hub;
        }

        public interface IFilterableEntity {
            Guid CustomerId { get; set; }
            DateTime CreateDate { get; set; }
            string UserCreatorID { get; set; }
        }


        /// <param name="pf">پیش فاکتور</param>
        /// <param name="type">1: accept 2: reject</param>
        public async Task<PreFactor> AcceptOrRejectPreFactor(PreFactor pf, int type)
        {
            var rejectDict = new Dictionary<int, int> {
                { (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین, (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین }, // عدم تایید تامین
                { (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری, (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین }, // عدم تایید تامین
                { (int)PreFactorEnum.در_انتظار_تایید_مشتری, (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین }, // عدم تایید تامین
                { (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش, (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری }, // عدم تایید مدیرفروش
                { (int)PreFactorEnum.در_انتظار_تایید_مالی, (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش }, // عدم تایید مالی
                { (int)PreFactorEnum.در_انتظار_تولید, (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش }, // عدم تایید مالی
            };

            var acceptedDict = new Dictionary<int, int> {
                { (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین, (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری }, // تایید تامین
                { (int)PreFactorEnum.در_انتظار_تایید_مشتری, (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش }, // تایید مشتری
                { (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش, (int)PreFactorEnum.در_انتظار_تایید_مالی }, // تایید مدیرفروش
                { (int)PreFactorEnum.در_انتظار_تایید_مالی, (int)PreFactorEnum.در_انتظار_تولید }, // تایید مالی
            };


            pf.PrePftype = pf.Pftype;
            switch (type)
            {
                case 1:
                    pf.Pftype = acceptedDict[pf.Pftype];
                    break;
                case 2:
                    pf.Pftype = rejectDict[pf.Pftype];
                    break;
            }

            pf.PftypeTxt = GetTypeTxtPfactor(pf.Pftype);

            await db.SaveChangesAsync();

            await hub.Clients.All.SendAsync("countPreFactorToAccept");
            return pf;
        }


        public async Task DeletePreFactor(PreFactor pf, AppUser user)
        {
            try
            {
                pf.IsDeleted = true;
                pf.PrePftype = pf.Pftype;
                pf.Pftype = (int)PreFactorEnum.حذف_شده;
                pf.PftypeTxt = nameof(PreFactorEnum.حذف_شده).Replace("_", " ");

                pf.UserModify = user.FullName;
                pf.UserModifyID = user.Id;
                pf.ModifyDate = DateTime.Now;
                pf.ModifyDateTxt = dtService.GetDateTimePersian();

                pf.CommentText = "حذف شده توسط" + " " + user.FullName;

                var orderHeads = await db.OrderHeads.Where(f => f.Prefactor == pf.Id).ToListAsync();
                foreach (var i in orderHeads)
                {
                    i.IsDeleted = true;
                    if (string.IsNullOrEmpty(user.Id))
                        i.CommentText = "حذف شده توسط سیستم";
                }

                var accS = await db.Accountancies.ForPreFactor(pf.Id).ToListAsync();

                foreach (var acc in accS.Where(f => f.PayType != PayTypeEnum.اعتباری))
                    acc?.Wallet = true;

                foreach (var acc in accS.Where(f => f.PayType == PayTypeEnum.اعتباری))
                    acc?.IsDeleted = true;


                await logSystemService.LogChange(pf, null, LogActionEnum.حذف, user);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }


        public async Task AddEmail(string title, string message, AppUser fromUser, string toUserId, string toUserName, EntityTypeEnum? entityName,
            Guid? entityId)
        {
            var newEmail = new Email {
                Title = title,
                Message = message,
                FromId = Guid.Parse(fromUser?.Id),
                FromName = fromUser?.FullName,
                ToId = Guid.Parse(toUserId),
                ToName = toUserName,
                EntityId = entityId,
                EntityName = entityName?.ToString(),
                UserCreator = fromUser?.FullName,
                UserCreatorID = fromUser?.Id
            };

            db.Emails.Add(newEmail);
        }


        public async Task UpdateChildEntityDeal(Guid? dealId, Customer customer, string dealCustomerName = "", string dealMobileNumber = "")
        {
            var pFactors = await db.PreFactors.Where(f => f.DealId == dealId).ToListAsync();
            foreach (var f in pFactors)
            {
                f.CustomerId = customer?.Id;
                f.CustomerCode = customer?.CustomerCode;
                f.Customer = customer?.CompanyName;
                f.AgentName = customer != null ? customer.AgentName : dealCustomerName;
                f.Cus_Mobile = customer != null ? customer.Mobile : dealMobileNumber;
            }

            var factors = await db.Factors.Where(f => f.DealId == dealId).ToListAsync();
            foreach (var f in factors)
            {
                f.CustomerId = customer?.Id;
                f.CustomerCode = customer?.CustomerCode;
                f.Customer = customer?.CompanyName;
                f.AgentName = customer != null ? customer.AgentName : dealCustomerName;
                f.Cus_Mobile = customer != null ? customer.Mobile : dealMobileNumber;
            }

            var reports = await db.Reports.Where(f => f.DealId == dealId).ToListAsync();
            foreach (var f in reports)
            {
                f.CustomerId = customer?.Id;
                f.CustomerCode = customer?.CustomerCode;
                if (customer != null)
                {
                    f.CustomerTxt = $"{customer.CompanyName ?? ""} {customer.AgentName ?? ""}".Trim() + "(کد مشتری " + customer.CustomerCode + ")";
                    f.RepTel = customer.Mobile;
                } else
                {
                    f.CustomerTxt = dealCustomerName;
                    f.RepTel = dealMobileNumber;
                }
            }

            var ordersHeads = await db.OrderHeads.Where(f => f.DealId == dealId).ToListAsync();
            foreach (var f in ordersHeads)
            {
                f.Customer = customer?.Id;
                f.CustomerCode = customer?.CustomerCode;
                f.CustomerTxt = customer != null ? $"{customer.CompanyName ?? ""} {customer.AgentName ?? ""}".Trim() : dealCustomerName;
            }

            var acc = await db.Accountancies.Where(f => f.DealId == dealId).ToListAsync();
            foreach (var f in acc)
            {
                f.CustomerId = customer?.Id;
                f.CustomerCode = customer?.CustomerCode;
                f.CustomerName = customer != null ? $"{customer.CompanyName ?? ""} {customer.AgentName ?? ""}".Trim() : dealCustomerName;
            }
        }

        public async Task<CartableCountObjVModel> GetCartableCountObj(AppUser user)
        {
            var roles = (await userManager.GetRolesAsync(user)).ToHashSet();

            var pf = db.PreFactors.AsNoTracking();
            var accPf = db.Accountancies.AsNoTracking().ForPreFactor();

            var countObj = new CartableCountObjVModel();

            // Sales Admin
            if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر فروش"))
            {
                countObj.SalesAdmin = await pf.CountAsync(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش);
            }

            // Tamin
            if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر تامین"))
            {
                countObj.Tamin = await pf.CountAsync(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین);
            } else if (roles.Contains("کارشناس تامین"))
            {
                var userId = Guid.Parse(user.Id);

                countObj.Tamin = await pf.CountAsync(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین && f.UserTaminId == userId);
            }

            // Financial
            if (roles.Contains("مدیر ارشد") || roles.Contains("کارشناس مالی"))
            {
                var financialPfCount = await pf.CountAsync(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی && f.AcceptedFinancial == null);

                var pendingAccCount = await accPf.CountAsync(a => a.Status == AccStatusEnum.در_دست_تایید || a.Status == AccStatusEnum.ثبت_اولیه);

                countObj.Financial = new SubCartableCountVModel {
                    InvoiceCount = pendingAccCount,
                    PreFactorCount = financialPfCount,
                };
            }

            // Managing Director
            if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر عامل"))
            {
                var invoiceCount = await accPf.OnlyManagingDirector()
                    .CountAsync(a => a.Status == AccStatusEnum.در_دست_تایید || a.Status == AccStatusEnum.ثبت_اولیه);
                var reqCount = await db.RequestAmounts.CountAsync(f =>
                    f.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل || f.RequestStatus == RequestStatusEnum.در_انتظار_ارسال_به_مالی);
                countObj.ManagingDirector = new SubCartableCountVModel {
                    InvoiceCount = invoiceCount,
                    RequestCount = reqCount
                };
            }

            return countObj;
        }

        public async Task<int> GetEmailsNotSeenCount(Guid userId)
        {
            var emailsCount = await db.Emails.AsNoTracking().Where(e => e.ToId == userId && !e.Seen).CountAsync();
            return emailsCount;
        }

        public async Task<FinancialStepEnum> GetFinancialStep(PreFactor pf)
        {
            var accList = await db.Accountancies.AsNoTracking().ForPreFactor(pf.Id).ToArrayAsync();

            var mdAcc = accList.Where(f => f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.تهاتری).ToArray();
            var financialAcc = accList.Where(f => f.PayType == PayTypeEnum.نقدی || f.PayType == PayTypeEnum.چکی).ToArray();


            bool waitingMd =
                mdAcc.Any()
                && (mdAcc.Any(f => f.Status != AccStatusEnum.تایید_شده) || pf.AcceptedManagingDirector != true);

            bool waitingFinancial =
                financialAcc.Any()
                && (financialAcc.Any(f => f.Status != AccStatusEnum.تایید_شده) || pf.AcceptedFinancial != true);

            if (waitingMd && waitingFinancial)
                return FinancialStepEnum.در_انتظار_تایید_مالی_و_مدیرعامل;

            if (waitingMd)
                return FinancialStepEnum.در_انتظار_تایید_مدیرعامل;

            if (waitingFinancial)
                return FinancialStepEnum.در_انتظار_تایید_مالی;

            if (mdAcc.Any() && financialAcc.Any())
                return FinancialStepEnum.در_انتظار_تایید_مالی_و_مدیرعامل;

            if (mdAcc.Any())
                return FinancialStepEnum.در_انتظار_تایید_مدیرعامل;

            return FinancialStepEnum.در_انتظار_تایید_مالی;
        }

        public async Task<object> getDetailAmountFactor(Guid? cusId, Guid? pfid)
        {
            IList<CRM.Core.Domain.Factors.Entities.Factor> fac = null;
            if (pfid != null)
            {
                fac = await db.Factors.Where(f => f.CustomerId == cusId && f.PfactorID == pfid).ToListAsync();
            } else
            {
                fac = await db.Factors.Where(f => f.CustomerId == cusId).ToListAsync();
            }

            decimal? FacAmount = 0;
            decimal? FacPayment = 0;
            decimal? Remained = 0;
            foreach (var item in fac)
            {
                //SumAmountFactors += (item.SumTotalAmount + item.TotalTax);
                var payfac = await db.Accountancies.Where(f => f.fid == item.Id).ToListAsync();
                FacAmount += payfac.Where(f => f.AccType == 0).Sum(f => f.debit);
                foreach (var p in payfac.Where(f => f.AccType == AcEnum.واریزی).ToList())
                {
                    FacPayment += p.credit;
                }
            }

            Remained = FacAmount - FacPayment;
            return new { Remained, FacAmount, FacPayment };
        }


        public static PreFactor UpdateTaxAmountPreFactor(PreFactor pf)
        {
            pf.PercentTax = pf.OfficialType == 1 ? 10 : 0;
            pf.TotalTax = pf.OfficialType == 1 ? pf.SumTotalAmount * 10 / 100 : 0;
            pf.PaymentAmount = pf.SumTotalAmount + pf.TotalTax;
            return pf;
        }

        public T UpdatePaymentAmount<T>(T model) where T : class
        {
            if (typeof(T) == typeof(Core.Domain.Factors.Entities.Factor))
            {
                var factor = model as Core.Domain.Factors.Entities.Factor;
                var items = factor.FactorItems.Where(f => !f.IsDeleted);
                decimal sumTotalAmount = 0;
                decimal totalTax = 0;
                decimal paymentAmount = 0;
                foreach (var item in items)
                {
                    item.TotalAmount = decimal.Parse(item.WeightCount) * item.UnitAmount;

                    sumTotalAmount += item.TotalAmount;
                }


                if (factor.OfficialType == 1)
                {
                    totalTax = Math.Round((sumTotalAmount - factor.Discount) * 10 / 100);
                }

                paymentAmount = (sumTotalAmount - factor.Discount) + totalTax;
                factor.SumTotalAmount = Math.Round(sumTotalAmount);
                factor.TotalTax = Math.Round(totalTax);
                factor.PaymentAmount = Math.Round(paymentAmount);
            } else if (typeof(T) == typeof(PreFactor))
            {
                var preFactor = model as PreFactor;
                var items = preFactor.PreFactorItems.Where(f => !f.IsDeleted);
                decimal sumTotalAmount = 0;
                decimal totalTax = 0;
                decimal paymentAmount = 0;
                foreach (var item in items)
                {
                    item.TotalAmount = decimal.Parse(item.WeightCount) * item.UnitAmount;

                    sumTotalAmount += item.TotalAmount;
                }


                if (preFactor.OfficialType == 1)
                {
                    totalTax = Math.Round((sumTotalAmount - preFactor.Discount) * 10 / 100);
                }

                paymentAmount = sumTotalAmount - preFactor.Discount + totalTax;
                preFactor.SumTotalAmount = Math.Round(sumTotalAmount);
                preFactor.TotalTax = Math.Round(totalTax);
                preFactor.PaymentAmount = Math.Round(paymentAmount);
            }

            return model;
        }

        public static string GetTypeTxtPfactor(int type)
        {
            return Enum.GetName(typeof(PreFactorEnum), type)?.Replace("_", " ") ?? "";
        }


        public static string getTypetxtPayType(int type)
        {
            var s = Enum.GetValues(typeof(PayTypeEnum))
                .Cast<PayTypeEnum>().FirstOrDefault(f => (int)f == type).ToString().Replace("_", " ");
            return s;
        }

        public static string getTypetxtInvoiceType(int type)
        {
            var s = Enum.GetValues(typeof(InvoiceTypeEnum))
                .Cast<InvoiceTypeEnum>().FirstOrDefault(f => (int)f == type).ToString().Replace("_", " ");
            return s;
        }

        public static string getTypetxtReportType(int type)
        {
            var s = Enum.GetValues(typeof(ReportTypeEnum))
                .Cast<ReportTypeEnum>().FirstOrDefault(f => (int)f == type).ToString().Replace("_", " ");
            return s;
        }

        public static string GetTypeTxtRoleType(int type)
        {
            var s = Enum.GetValues<RoleTypeEnum>()
                .FirstOrDefault(f => (int)f == type).ToString().Replace("_", " ");
            return s;
        }


        //[HttpPost]
        //public async Task<IActionResult> GotoCartable(GotoCartableViewmodel model)
        //{
        //    try
        //    {
        //        //var userInRol = await userManager.GetUsersInRoleAsync("کارشناس مالی");
        //        var rol = await udb.Roles.Where(f => f.Name == model.rol).FirstOrDefaultAsync();
        //        if (rol != null)
        //        {
        //            switch (model.entityName)
        //            {
        //                case "PreFactor":
        //                    var data = await db.PreFactors
        //                        .Where(f => f.Id == Guid.Parse(model.entityId))
        //                        .FirstOrDefaultAsync();
        //                    //data.CartableRolID = rol.Id;
        //                    //data.CartableRolTxt = rol.Name;
        //                    //if (!string.IsNullOrEmpty(model.user))
        //                    //{
        //                    //    data.CartableUserId = model.user;
        //                    //    data.CartableUserTxt = udb.Users.Where(f => f.Id == model.user).FirstOrDefaultAsync().Result.FullName;
        //                    //}
        //                    await db.SaveChangesAsync();
        //                    return Ok();
        //            }
        //        }
        //        return BadRequest();
        //    }
        //    catch
        //    {
        //        return BadRequest();

        //    }

        //}


        public async Task<bool> IsCustomerExport(Guid? customerId)
        {
            var isExport = await db.Factors.AsNoTracking().AnyAsync(f => f.CustomerId == customerId && f.Export == true);

            return isExport;
        }

        public async Task<bool> IsCustomerActive(Guid? customerId)
        {
            var isActive = await db.Factors.AsNoTracking().AnyAsync(f => f.CustomerId == customerId && f.status == FactorStatus.تایید_شده);

            return isActive;
        }
    }
}