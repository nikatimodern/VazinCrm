using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.Cartables.Entities;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Contracts.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.Payments.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.RequestAmounts.Entities;
using CRM.Core.Domain.Supplies.Entities;
using CRM.EndPoint.FrontEnd.Models.LogSystem;
using Generics.Infrastructures.Sql;
using Newtonsoft.Json;

namespace CRM.EndPoint.FrontEnd.Services;

public class LogSystemService(CRMContextDb db, DateTimeServices dtService) {
    private readonly List<string> _excludedProps = [
        "CreateDate", "CreateDateTxt", "ModifyDate", "ModifyDateTxt", "UserCreatorID", "UserCreator",
        "UserModifyID", "UserModify", "DateSort", "DateSortTxt", "TaminStep"
    ];

    public async Task<LogSystem> LogChange<T>(T oldEntity, T newEntity, LogActionEnum action, AppUser user,
        List<string> includedCollections = null, string commentTxt = null)
    {
        try
        {
            if (oldEntity == null) return null;

            var entityType = typeof(T);
            var entityName = entityType.Name;

            var entityIdValue = GetIdAsString(oldEntity);

            var (parentNameValue, parentIdValue) = GetParentInfo(oldEntity);

            var logBuilder = new StringBuilder();
            if (action is LogActionEnum.ویرایش or LogActionEnum.تغییر_وضعیت or LogActionEnum.تایید_و_تغییر_وضعیت or LogActionEnum.رد_و_تغییر_وضعیت &&
                newEntity != null)
            {
                var simpleProps = entityType.GetProperties()
                    .Where(p => p.CanRead && IsSimpleType(p.PropertyType) && !_excludedProps.Contains(p.Name));

                const string rtl = "\u2067";
                const string pdi = "\u2069";

                foreach (var prop in simpleProps)
                {
                    var oldVal = prop.GetValue(oldEntity)?.ToString();
                    var newVal = prop.GetValue(newEntity)?.ToString();

                    var normalizedOld = string.IsNullOrWhiteSpace(oldVal) ? null : oldVal.Trim();
                    var normalizedNew = string.IsNullOrWhiteSpace(newVal) ? null : newVal.Trim();

                    if (normalizedOld != normalizedNew)
                        logBuilder.AppendLine(
                            $"📌 {prop.Name}: {rtl}'{oldVal?.Replace("\n", " ") ?? "null"}'{pdi} → {rtl}'{newVal?.Replace("\n", " ") ?? "null"}'{pdi}"
                        );
                }

                if (includedCollections != null && includedCollections.Count != 0)
                {
                    var collectionProps = entityType.GetProperties()
                        .Where(p =>
                            typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) &&
                            p.PropertyType != typeof(string) &&
                            includedCollections.Contains(p.Name));

                    foreach (var prop in collectionProps)
                    {
                        var oldList = (prop.GetValue(oldEntity) as IEnumerable<object>)?.ToList();
                        var newList = (prop.GetValue(newEntity) as IEnumerable<object>)?.ToList();

                        if (oldList == null || newList == null)
                            continue;

                        CompareCollections(logBuilder, oldList, newList, prop.Name);
                    }
                }

                if (string.IsNullOrWhiteSpace(logBuilder.ToString()))
                    return null;
            } else
            {
                logBuilder = null;
            }


            var entitySerialNumber = LogSetEntitySerialNumber(oldEntity);
            var lSModel = new LogSystemModel<T> {
                OldEntity = oldEntity,
                NewEntity = newEntity,
                SerialNumber = entitySerialNumber,
                Action = action,
                FullName = user.FullName?.Trim(),
                CommentTxt = commentTxt,
            };
            var description = LogSetDescription(lSModel);

            var log = new LogSystem {
                Id = Guid.NewGuid(),
                EntityName = entityName,
                EntityId = entityIdValue,
                ParentEntityId = parentIdValue,
                ParentEntityName = parentNameValue,
                ChangeDetails = logBuilder?.ToString(),
                ChangeTime = DateTime.Now,
                ChangeTimeTxt = dtService.GetDateTimePersian(),
                ChangedBy = user.FullName?.Trim(),
                ChangedById = user.Id,
                ActionType = action,
                ActionTypeTxt = action.ToString().Replace("_", " "),
                CommentTxt = commentTxt,
                EntitySerialNumber = entitySerialNumber,
                Description = description,
            };

            if (entityName == nameof(PreFactor) &&
                action is LogActionEnum.تغییر_وضعیت or LogActionEnum.تایید_و_تغییر_وضعیت or LogActionEnum.رد_و_تغییر_وضعیت)
            {
                log.Cartable = LogSetCartable(oldEntity as PreFactor, newEntity as PreFactor);
            }

            db.logSystems.Add(log);

            return log;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return null;
        }
    }

    private static Cartable LogSetCartable(PreFactor oldPf, PreFactor newPf)
    {
        var cartable = new Cartable {
            FromCartable = GetCartableEnum(oldPf.Pftype),
            ToCartable = GetCartableEnum(newPf.Pftype),
        };
        cartable.FromCartableTxt = cartable.FromCartable?.ToString().Replace("_", " ");
        cartable.ToCartableTxt = cartable.ToCartable?.ToString().Replace("_", " ");

        return cartable;
    }

    private static CartableEnum? GetCartableEnum(int pfType)
    {
        return pfType switch {
            (int)PreFactorEnum.در_انتظار_تایید_مدیر_فروش => CartableEnum.مدیر_فروش,

            (int)PreFactorEnum.در_انتظار_تایید_مسئول_تامین => CartableEnum.کارشناس_تامین,

            (int)PreFactorEnum.در_انتظار_تایید_مالی => CartableEnum.کارشناس_مالی,

            (int)PreFactorEnum.در_انتظار_واگذاری_به_مسئول_تامین or
                (int)PreFactorEnum.در_انتظار_ارسال_به_مشتری or
                (int)PreFactorEnum.در_انتظار_تولید => CartableEnum.کارشناس_فروش,

            (int)PreFactorEnum.در_انتظار_تایید_مشتری => CartableEnum.مشتری,

            _ => null
        };
    }


    private static string LogSetDescription<T>(LogSystemModel<T> model)
    {
        var oldEntity = model.OldEntity;
        var newEntity = model.NewEntity;
        var serialNumber = model.SerialNumber;
        var action = model.Action;
        var fullName = model.FullName;
        var commentTxt = model.CommentTxt;

        string fullCustomer;
        switch (oldEntity)
        {
            case Deal deal:
                var newDeal = newEntity as Deal;
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک سفارش به شماره سریال {serialNumber} با عنوان <i>{deal.Title}</i> توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"سفارش شماره {serialNumber} با عنوان <i>{deal.Title}</i> توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"سفارش شماره {serialNumber} با عنوان <i>{deal.Title}</i> توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.بازیابی:
                        return $"سفارش شماره {serialNumber} با عنوان <i>{deal.Title}</i> توسط <b>{fullName}</b> بازیابی شد.";

                    case LogActionEnum.تغییر_وضعیت:
                        return
                            $"سفارش شماره {serialNumber} با عنوان <i>{deal.Title}</i> توسط <b>{fullName}</b> در وضعیت <b>{newDeal?.Status.ToString()}</b> قرار گرفت.";
                }

                break;
            case PreFactor oldPf:
                var newPf = newEntity as PreFactor;
                fullCustomer = $"{oldPf.Customer} {oldPf.AgentName}".Trim();
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return
                            $"یک پیش‌فاکتور به شماره سریال {serialNumber} برای مشتری ({fullCustomer}) توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"پیش‌فاکتور شماره {serialNumber} مربوط به مشتری ({fullCustomer}) توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"پیش‌فاکتور شماره {serialNumber} مربوط به مشتری ({fullCustomer}) توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.بازیابی:
                        return $"پیش‌فاکتور شماره {serialNumber} مربوط به مشتری ({fullCustomer}) توسط <b>{fullName}</b> بازیابی شد.";

                    case LogActionEnum.تغییر_وضعیت:
                        if (oldPf.PftypeTxt?.Trim() == newPf?.PftypeTxt?.Trim())
                        {
                            if (commentTxt == "ارسال به مالی")
                                return $"پیش‌فاکتور شماره {serialNumber} مربوط به مشتری ({fullCustomer}) توسط <b>{fullName}</b> به مالی ارسال شد.";
                        }

                        return
                            $"پیش‌فاکتور شماره {serialNumber} توسط <b>{fullName}</b> از وضعیت <b>{oldPf.PftypeTxt}</b> به وضعیت <b class=\"text-dark\">{newPf?.PftypeTxt}</b> تغییر یافت.";

                    case LogActionEnum.تایید_و_تغییر_وضعیت:
                        if (oldPf.PftypeTxt?.Trim() == newPf?.PftypeTxt?.Trim()) //  "تایید مالی" / "تایید مدیرعامل"
                            return $"پیش‌فاکتور شماره {serialNumber} توسط <b>{fullName}</b> {commentTxt} شد.";

                        return
                            $"پیش‌فاکتور شماره {serialNumber} توسط <b>{fullName}</b> تایید شد و از وضعیت <b>{oldPf.PftypeTxt}</b> به وضعیت <b class=\"text-dark\">{newPf?.PftypeTxt}</b> تغییر یافت.";

                    case LogActionEnum.رد_و_تغییر_وضعیت:
                        return
                            $"پیش‌فاکتور شماره {serialNumber} توسط <b>{fullName}</b> عدم تایید خورد و از وضعیت <b>{oldPf.PftypeTxt}</b> به وضعیت <b class=\"text-dark\">{newPf?.PftypeTxt}</b> تغییر یافت.";
                }

                break;
            case Customer cus:
                fullCustomer = $"{cus.CompanyName} {cus.AgentName}".Trim();
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"مشتری شماره {serialNumber} ({fullCustomer}) توسط <b>{fullName}</b> در سامانه ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"مشتری شماره {serialNumber} ({fullCustomer}) توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"مشتری شماره {serialNumber} ({fullCustomer}) توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.بازیابی:
                        return $"مشتری شماره {serialNumber} ({fullCustomer}) توسط <b>{fullName}</b> بازیابی شد.";

                    case LogActionEnum.تایید_و_تغییر_وضعیت:
                        return $"مشتری شماره {serialNumber} ({fullCustomer}) توسط <b>{fullName}</b> تایید شد.";
                }

                break;
            case Core.Domain.Factors.Entities.Factor f:
                var title = f.ParentReturnedId != null ? "فاکتور برگشتی" : "فاکتور";
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        fullCustomer = $"{f.Customer} {f.AgentName}".Trim();
                        return $"یک {title} به شماره سریال {serialNumber} برای مشتری ({fullCustomer}) توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"{title} شماره {serialNumber} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"{title} شماره {serialNumber} توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.بازیابی:
                        return $"{title} شماره {serialNumber} توسط <b>{fullName}</b> بازیابی شد.";

                    case LogActionEnum.تغییر_وضعیت:
                        return $"{title} شماره {serialNumber} توسط <b>{fullName}</b> به حالت در انتظار تایید بازگشت.";

                    case LogActionEnum.تایید_و_تغییر_وضعیت:
                        return $"{title} شماره {serialNumber} توسط <b>{fullName}</b> تایید شد.";

                    case LogActionEnum.رد_و_تغییر_وضعیت:
                        return $"{title} شماره {serialNumber} توسط <b>{fullName}</b> ابطال شد.";
                }

                break;
            case Report report:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return report.DealSerialNumber != null
                            ? $"یک {report.ReportTypeTxt} برای سفارش {report.DealSerialNumber} توسط <b>{fullName}</b> ثبت شد."
                            : $"یک {report.ReportTypeTxt} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return report.DealSerialNumber != null
                            ? $"یک {report.ReportTypeTxt} مربوط به سفارش {report.DealSerialNumber} توسط <b>{fullName}</b> ویرایش شد."
                            : $"یک {report.ReportTypeTxt} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return report.DealSerialNumber != null
                            ? $"یک {report.ReportTypeTxt} مربوط به سفارش {report.DealSerialNumber} توسط <b>{fullName}</b> حذف شد."
                            : $"یک {report.ReportTypeTxt} توسط <b>{fullName}</b> حذف شد.";
                }

                break;
            case Attachment:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک {commentTxt} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"یک {commentTxt} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"یک {commentTxt} توسط <b>{fullName}</b> حذف شد.";
                }

                break;
            case Accountancy acc:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return
                            $"یک <b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} برای پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return
                            $"<b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return
                            $"<b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.بازیابی:
                        return
                            $"<b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> بازیابی شد.";

                    case LogActionEnum.تغییر_وضعیت:
                        if (commentTxt == "ارسال به مالی")
                            return
                                $"<b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> به مالی ارسال شد.";
                        return
                            $"<b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> به حالت در انتظار تایید بازگشت.";

                    case LogActionEnum.تایید_و_تغییر_وضعیت:
                        return
                            $"<b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> تایید شد.";

                    case LogActionEnum.رد_و_تغییر_وضعیت:
                        return
                            $"<b>{acc.AccTypeTxt} {acc.PayTypeTxt}</b> به شماره ثبت {serialNumber} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) توسط <b>{fullName}</b> رد شد.";
                }

                break;
            case BankAccount bank:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک حساب بانکی به نام {bank.AccountName} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"حساب بانکی به نام {bank.AccountName} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"حساب بانکی به نام {bank.AccountName} توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.تغییر_وضعیت:
                        return
                            $"حساب بانکی به نام {bank.AccountName} توسط <b>{fullName}</b> به وضعیت <b>{(bank.Active ? "فعال" : "غیرفعال")}</b> تغییر یافت.";
                }

                break;

            case AppUser user:
                /*if (!string.IsNullOrEmpty(commentTxt) && (commentTxt.Contains("192.168.1.3") || commentTxt.Contains("127.0.0.1") || commentTxt.Contains("::1")) && fullName != "مدیر ارشد")
                    return null;*/

                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"کاربر <b>{user.FullName}</b> در سامانه ثبت شد.";

                    case LogActionEnum.ورود_کاربر:
                        return $"کاربر <b>{user.FullName}</b> وارد سامانه شد.";

                    case LogActionEnum.خروج_کاربر:
                        return $"کاربر <b>{user.FullName}</b> از سامانه خارج شد.";
                }

                break;
            case OrderHead oh:
                var newOh = newEntity as OrderHead;
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک حواله به شماره {serialNumber} مربوط به پیش‌فاکتور {oh.PrefactorSer} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"حواله شماره {serialNumber} مربوط به پیش‌فاکتور {oh.PrefactorSer} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"حواله شماره {serialNumber} مربوط به پیش‌فاکتور {oh.PrefactorSer} توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.بازیابی:
                        return $"حواله شماره {serialNumber} مربوط به پیش‌فاکتور {oh.PrefactorSer} توسط <b>{fullName}</b> بازیابی شد.";

                    case LogActionEnum.تغییر_وضعیت:
                        return
                            $"حواله شماره {serialNumber} مربوط به پیش‌فاکتور {oh.PrefactorSer} توسط <b>{fullName}</b> در وضعیت <b>{newOh?.Status?.ToString().Replace("_", " ")}</b> قرار گرفت.";
                }

                break;
            case Supply supply:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"تامین کننده و بهای تامین پیش‌فاکتور {supply.PfSerial} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"تامین کننده و بهای تامین پیش‌فاکتور {supply.PfSerial} توسط <b>{fullName}</b> ویرایش شد.";
                }

                break;
            case Payment:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک پرداختی به شماره پرداخت {serialNumber} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"یک پرداختی به شماره پرداخت {serialNumber} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"یک پرداختی به شماره پرداخت {serialNumber} توسط <b>{fullName}</b> حذف شد.";
                }

                break;
            case SupplyFactor supplyFactor:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک فاکتور تامین به شماره فاکتور {supplyFactor.FactorSerial} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"یک فاکتور تامین به شماره فاکتور {supplyFactor.FactorSerial} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"یک فاکتور تامین به شماره فاکتور {supplyFactor.FactorSerial} توسط <b>{fullName}</b> حذف شد.";
                }

                break;
            case RequestAmount oldReq:
                var newReq = newEntity as RequestAmount;
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک درخواست وجه به شماره درخواست {serialNumber} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"یک درخواست وجه به شماره درخواست {serialNumber} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"یک درخواست وجه به شماره درخواست {serialNumber} توسط <b>{fullName}</b> حذف شد.";

                    case LogActionEnum.تایید_و_تغییر_وضعیت:
                        return $"یک درخواست وجه به شماره درخواست {serialNumber} توسط <b>{fullName}</b> تایید شد.";

                    case LogActionEnum.تغییر_وضعیت:
                        return
                            $"درخواست وجه شماره {serialNumber} توسط <b>{fullName}</b> از وضعیت <b>{oldReq.RequestStatusTxt}</b> به وضعیت <b class=\"text-dark\">{newReq?.RequestStatusTxt}</b> تغییر یافت.";
                }

                break;
            case Cash:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک تنخواه به شماره تنخواه {serialNumber} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"یک تنخواه به شماره تنخواه {serialNumber} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"یک تنخواه به شماره تنخواه {serialNumber} توسط <b>{fullName}</b> حذف شد.";
                }

                break;
            case Checkbook cb:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک دسته چک برای بانک {cb.BankName} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.حذف:
                        return $"دسته چک بانک {cb.BankName} توسط <b>{fullName}</b> حذف شد.";
                }

                break;
            case Check check:
                switch (action)
                {
                    case LogActionEnum.ثبت:
                        return $"یک برگه چک به شماره سریال {check.SerialNumber} توسط <b>{fullName}</b> ثبت شد.";

                    case LogActionEnum.ویرایش:
                        return $"یک برگه چک به شماره سریال {check.SerialNumber} توسط <b>{fullName}</b> ویرایش شد.";

                    case LogActionEnum.حذف:
                        return $"یک برگه چک به شماره سریال {check.SerialNumber} توسط <b>{fullName}</b> حذف شد.";
                }

                break;
        }

        return null;
    }


    private static int? LogSetEntitySerialNumber<T>(T entity)
    {
        return entity switch {
            Deal deal => deal.SerialNumber,
            PreFactor pf => pf.SerialNumber,
            Core.Domain.Factors.Entities.Factor f => f.SerialNumber,
            Accountancy a => a.SerialNum,
            OrderHead o => o.OrderNumber,
            Customer c => c.CustomerCode,
            Payment p => p.PaymentNumber,
            SupplyFactor s => TextService.ExtractNumber(s.FactorSerial),
            RequestAmount r => r.RequestNumber,
            Cash c => c.Serial,
            Check c => TextService.ExtractNumber(c.SerialNumber),
            _ => null
        };
    }

    private void CompareCollections(StringBuilder logBuilder, List<object> oldList, List<object> newList, string collectionName)
    {
        try
        {
            //var oldMap = oldList.ToDictionary(e => GetIdAsString(e));
            //var newMap = newList.ToDictionary(e => GetIdAsString(e));
            var oldMap = oldList.Select((e, i) => new {
                Key = GetIdAsString(e) == "null" ? $"null_{i}" : GetIdAsString(e),
                Value = e
            }).ToDictionary(x => x.Key, x => x.Value);
            var newMap = newList.Select((e, i) => new {
                Key = GetIdAsString(e) == "null" ? $"null_{i}" : GetIdAsString(e),
                Value = e
            }).ToDictionary(x => x.Key, x => x.Value);

            var removed = oldMap.Keys.Except(newMap.Keys);
            var added = newMap.Keys.Except(oldMap.Keys);
            var common = oldMap.Keys.Intersect(newMap.Keys);

            foreach (var id in removed)
            {
                var ent = oldMap[id];
                LogCollectionItem(logBuilder, collectionName, ent, "❌ Removed");
            }

            foreach (var id in added)
            {
                var ent = newMap[id];
                LogCollectionItem(logBuilder, collectionName, ent, "➕ Added");
            }

            foreach (var id in common)
            {
                var oldItem = oldMap[id];
                var newItem = newMap[id];
                var diffs = GetPropertyDiffs(oldItem, newItem);
                if (!string.IsNullOrEmpty(diffs))
                {
                    logBuilder.AppendLine($"✏️ {collectionName} Updated (ID={id}) :\n{diffs}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void LogCollectionItem(StringBuilder logBuilder, string collectionName, object ent, string action)
    {
        var idVal = GetIdAsString(ent);

        switch (collectionName)
        {
            case "Descriptions":
            {
                var textVal = ent.GetType().GetProperty("Text")?.GetValue(ent)?.ToString() ?? "";
                logBuilder.AppendLine($"{action} {collectionName} (ID={idVal}) : Text='{textVal}'");
                break;
            }
            case "PreFactorItems":
            case "FactorItems":
            {
                var product = ent.GetType().GetProperty("Product")?.GetValue(ent)?.ToString() ?? "";
                var weightCount = ent.GetType().GetProperty("WeightCount")?.GetValue(ent)?.ToString() ?? "";
                var unitAmount = ent.GetType().GetProperty("UnitAmount")?.GetValue(ent)?.ToString() ?? "";
                var totalAmount = ent.GetType().GetProperty("TotalAmount")?.GetValue(ent)?.ToString() ?? "";
                var productSmallName = ent.GetType().GetProperty("ProductSmallName")?.GetValue(ent)?.ToString() ?? "";

                logBuilder.AppendLine(
                    $"{action} {collectionName} (ID={idVal}) :\n  • Product= '{product}'\n  • ProductSmallName= '{productSmallName}'\n  • WeightCount= '{weightCount}'\n  • UnitAmount= '{unitAmount}'\n  • TotalAmount= '{totalAmount}'\n"
                );
                break;
            }
            case "Orders":
            {
                var productSmallName = ent.GetType().GetProperty("ProductSmallName")?.GetValue(ent)?.ToString() ?? "";
                var product = ent.GetType().GetProperty("Product")?.GetValue(ent)?.ToString() ?? "";
                var weight = ent.GetType().GetProperty("Weight")?.GetValue(ent)?.ToString() ?? "";

                logBuilder.AppendLine(
                    $"{action} {collectionName} (ID={idVal}) :\n  • Product= '{product}'\n  • ProductSmallName= '{productSmallName}'\n  • Weight= '{weight}'\n"
                );
                break;
            }
            default:
                logBuilder.AppendLine($"{action} {collectionName}: {SerializeShort(ent)}");
                break;
        }
    }

    private static string GetIdAsString(object entity)
    {
        var idProp = entity.GetType().GetProperty("Id");
        if (idProp == null) return "null";

        var value = idProp.GetValue(entity);
        return value?.ToString() ?? "null";
    }


    private static string SerializeShort(object entity)
    {
        return JsonConvert.SerializeObject(entity, new JsonSerializerSettings {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MaxDepth = 2
        });
    }

    private string GetPropertyDiffs(object oldObj, object newObj)
    {
        var sb = new StringBuilder();
        var props = oldObj.GetType().GetProperties()
            .Where(p => p.CanRead && IsSimpleType(p.PropertyType) && !_excludedProps.Contains(p.Name));

        foreach (var prop in props)
        {
            var oldVal = prop.GetValue(oldObj)?.ToString();
            var newVal = prop.GetValue(newObj)?.ToString();

            var normalizedOld = string.IsNullOrWhiteSpace(oldVal) ? null : oldVal.Trim();
            var normalizedNew = string.IsNullOrWhiteSpace(newVal) ? null : newVal.Trim();

            if (normalizedOld != normalizedNew)
            {
                const string rtl = "\u2067";
                const string pdi = "\u2069";

                sb.AppendLine($"  • {prop.Name}: {rtl}'{oldVal ?? "null"}'{pdi} → {rtl}'{newVal ?? "null"}'{pdi}");
                //sb.AppendLine($"  • {prop.Name}: '{oldVal ?? "null"}' → '{newVal ?? "null"}'");
            }
        }

        return sb.ToString();
    }

    private static bool IsSimpleType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return
            underlyingType.IsPrimitive ||
            underlyingType.IsEnum ||
            underlyingType == typeof(string) ||
            underlyingType == typeof(decimal) ||
            underlyingType == typeof(DateTime) ||
            underlyingType == typeof(Guid) ||
            underlyingType == typeof(double) ||
            underlyingType == typeof(float) ||
            underlyingType == typeof(int) ||
            underlyingType == typeof(long) ||
            underlyingType == typeof(short) ||
            underlyingType == typeof(byte) ||
            underlyingType == typeof(bool);
    }

    private static (string parentNameValue, string parentIdValue) GetParentInfo<T>(T oldEntity)
    {
        string parentNameValue = null;
        string parentIdValue = null;

        switch (oldEntity)
        {
            case Accountancy acc:
                parentNameValue = nameof(PreFactor);
                parentIdValue = acc.pfid.ToString();
                break;

            case PreFactor pf:
                parentNameValue = nameof(Deal);
                parentIdValue = pf.DealId?.ToString();
                break;

            case Core.Domain.Factors.Entities.Factor fac:
                parentNameValue = nameof(PreFactor);
                parentIdValue = fac.PfactorID.ToString();
                break;

            case OrderHead oh:
                parentNameValue = nameof(PreFactor);
                parentIdValue = oh.Prefactor?.ToString();
                break;

            case Report r:
                if (r.DealSerialNumber != null)
                {
                    parentNameValue = nameof(Deal);
                    parentIdValue = r.DealId?.ToString();
                }

                break;

            case Attachment att:
                parentNameValue = att._AttachType switch {
                    _AttachType.DocPayFactor => nameof(Core.Domain.Factors.Entities.Factor),
                    _AttachType.AcceptedCusPreFactor => nameof(PreFactor),
                    _AttachType.UploadCustomerPf => nameof(PreFactor),
                    _AttachType.UploadSalesExpertPf => nameof(PreFactor),
                    _AttachType.ContractAttach => nameof(Contract),
                    _AttachType.DocPayPreFactor => nameof(Accountancy),
                    _ => null
                };

                parentIdValue = att.EntityId?.ToString();
                break;
        }

        return (parentNameValue, parentIdValue);
    }
}