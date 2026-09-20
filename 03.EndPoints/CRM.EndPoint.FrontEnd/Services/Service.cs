using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.Emails.Entities;
using CRM.Core.Domain.Factors.Enum;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Reports.Enum;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Core.Domain.Transfers.Entities;
using FactorEntity = CRM.Core.Domain.Factors.Entities.Factor;
using CRM.EndPoint.FrontEnd.Models.Cartable;
using CRM.Infrastructures.Sql;
using CRM.Infrastructures.Sql.Accountancies.QueryExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace CRM.EndPoint.FrontEnd.Services {
    public class Service : Controller {
        private readonly CRMContextDb db;
        private readonly UserManager<AppUser> userManager;

        public Service(CRMContextDb db, UserManager<AppUser> userManager)
        {
            this.db = db;
            this.userManager = userManager;
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


        public async Task<int> GetEmailsNotSeenCount(Guid userId)
        {
            var emailsCount = await db.Emails.AsNoTracking().Where(e => e.ToId == userId && !e.Seen).CountAsync();
            return emailsCount;
        }


        public async Task UpdateChildEntityDeal(Guid? dealId, Customer customer, string dealCustomerName = "", string dealMobileNumber = "")
        {
            var pFactors = await db.PreFactors.Where(f => f.DealId == dealId).ToListAsync();
            foreach (var p in pFactors)
            {
                p.CustomerId = customer?.Id;
                p.CustomerCode = customer?.CustomerCode;
                p.Customer = customer?.CompanyName;
                p.AgentName = customer != null ? customer.AgentName : dealCustomerName;
                p.Cus_Mobile = customer != null ? customer.Mobile : dealMobileNumber;
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
            if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر تامین") || roles.Contains("کارشناس تامین"))
            {
                int pfCount;
                if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر تامین"))
                {
                    pfCount = await pf.CountAsync(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین);
                } else
                {
                    var userId = Guid.Parse(user.Id);
                    pfCount = await pf.CountAsync(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین && f.UserTaminId == userId);
                }

                var invoiceCount = await accPf.OnlyTamin()
                    .CountAsync(a => a.Status == AccStatusEnum.در_دست_تایید || a.Status == AccStatusEnum.ثبت_اولیه);

                countObj.Tamin = new SubCartableCountVModel {
                    InvoiceCount = invoiceCount,
                    PreFactorCount = pfCount,
                };
            }

            // Financial
            if (roles.Contains("مدیر ارشد") || roles.Contains("کارشناس مالی"))
            {
                var financialPfCount = await pf.CountAsync(f => f.Pftype == (int)PreFactorEnum.در_انتظار_تایید_مالی && f.AcceptedFinancial == null);

                var pendingAccCount = await accPf.CountAsync(a => a.Status == AccStatusEnum.در_دست_تایید || a.Status == AccStatusEnum.ثبت_اولیه);

                var reqCount = await db.RequestAmounts.CountAsync(f => f.RequestStatus == RequestStatusEnum.در_انتظار_پرداخت);

                countObj.Financial = new SubCartableCountVModel {
                    InvoiceCount = pendingAccCount,
                    PreFactorCount = financialPfCount,
                    RequestCount = reqCount
                };
            }

            // Managing Director
            if (roles.Contains("مدیر ارشد") || roles.Contains("مدیر عامل"))
            {
                var reqCount = await db.RequestAmounts.CountAsync(f =>
                    f.RequestStatus == RequestStatusEnum.در_انتظار_تایید_مدیرعامل || f.RequestStatus == RequestStatusEnum.در_انتظار_ارسال_به_مالی);
                var transferCount = await db.Transfers.CountAsync(f => f.Status == TransferStatusEnum.در_دست_تایید);
                countObj.ManagingDirector = new SubCartableCountVModel {
                    RequestCount = reqCount,
                    TransferCount = transferCount
                };
            }

            return countObj;
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


        public static T UpdatePaymentAmount<T>(T model, int taxPercent) where T : class
        {
            if (model is FactorEntity factor)
            {
                var items = factor.FactorItems.Where(x => !x.IsDeleted);

                decimal sumTotalAmount = 0;

                foreach (var item in items)
                {
                    item.TotalAmount = decimal.Parse(item.WeightCount) * item.UnitAmount;
                    sumTotalAmount += item.TotalAmount;
                }

                factor.SumTotalAmount = Math.Round(sumTotalAmount);
                factor.Discount = Math.Round(factor.Discount);

                decimal totalTax = 0;

                if (factor.OfficialType == 1)
                {
                    factor.PercentTax = taxPercent;

                    totalTax = Math.Round((factor.SumTotalAmount - factor.Discount) * taxPercent / 100m);
                } else
                {
                    factor.PercentTax = 0;
                }

                factor.TotalTax = totalTax;
                factor.PaymentAmount = Math.Round(factor.SumTotalAmount - factor.Discount + totalTax);
            } else if (model is PreFactor preFactor)
            {
                var items = preFactor.PreFactorItems.Where(x => !x.IsDeleted);

                decimal sumTotalAmount = 0;

                foreach (var item in items)
                {
                    item.TotalAmount = decimal.Parse(item.WeightCount) * item.UnitAmount;
                    sumTotalAmount += item.TotalAmount;
                }

                preFactor.SumTotalAmount = Math.Round(sumTotalAmount);
                preFactor.Discount = Math.Round(preFactor.Discount);

                decimal totalTax = 0;

                if (preFactor.OfficialType == 1)
                {
                    preFactor.PercentTax = taxPercent;

                    totalTax = Math.Round((preFactor.SumTotalAmount - preFactor.Discount) * taxPercent / 100m);
                } else
                {
                    preFactor.PercentTax = 0;
                }

                preFactor.TotalTax = totalTax;
                preFactor.PaymentAmount = Math.Round(preFactor.SumTotalAmount - preFactor.Discount + totalTax);
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