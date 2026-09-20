using System;
using System.Linq;
using System.Linq.Expressions;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;

namespace CRM.Infrastructures.Sql.Accountancies.QueryExtensions;

public static class AccountancyQueryExtensions {
    public static IQueryable<Accountancy> ForPreFactor(this IQueryable<Accountancy> query, Guid? pfId = null)
    {
        var q = query.Where(a =>
            a.InvoiceType == InvoiceTypeEnum.پیش_فاکتور &&
            (a.AccType == AcEnum.واریزی || a.AccType == AcEnum.برگشتی)
        );

        if (pfId.HasValue)
            q = q.Where(a => a.pfid == pfId.Value);

        return q;
    }

    public static IQueryable<Accountancy> ForFactor(this IQueryable<Accountancy> query, Guid fId)
    {
        return query.Where(a =>
            a.fid == fId
            && a.InvoiceType == InvoiceTypeEnum.فاکتور
            && (a.AccType == AcEnum.واریزی || a.AccType == AcEnum.برگشتی)
        );
    }

    public static IQueryable<Accountancy> ForDeal(this IQueryable<Accountancy> query, Guid dealId)
    {
        return query.Where(a =>
            a.DealId == dealId
            && a.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
            && (a.AccType == AcEnum.واریزی || a.AccType == AcEnum.برگشتی)
        );
    }

    public static IQueryable<Accountancy> ForCustomer(this IQueryable<Accountancy> query, Guid? customerId)
    {
        return query.Where(a =>
            a.CustomerId == customerId
            && a.InvoiceType == InvoiceTypeEnum.پیش_فاکتور
            && (a.AccType == AcEnum.واریزی || a.AccType == AcEnum.برگشتی)
        );
    }



    public static Expression<Func<Accountancy, bool>> OnlyFinancialExpr()
    {
        return a => a.PayType == PayTypeEnum.نقدی || a.PayType == PayTypeEnum.چکی;
    }
    public static Expression<Func<Accountancy, bool>> OnlyTaminExpr()
    {
        return a => a.PayType == PayTypeEnum.اعتباری || a.PayType == PayTypeEnum.تهاتری;
    }

    public static IQueryable<Accountancy> OnlyFinancial(this IQueryable<Accountancy> query)
    {
        return query.Where(a => a.PayType == PayTypeEnum.نقدی || a.PayType == PayTypeEnum.چکی);
    }

    public static IQueryable<Accountancy> OnlyTamin(this IQueryable<Accountancy> query)
    {
        return query.Where(a => a.PayType == PayTypeEnum.اعتباری || a.PayType == PayTypeEnum.تهاتری);
    }


    public static IQueryable<Accountancy> NotVosoled(this IQueryable<Accountancy> query)
    {
        return query.Where(f =>
            (f.PayType != PayTypeEnum.اعتباری && f.PayType != PayTypeEnum.چکی)
            ||
            ((f.PayType == PayTypeEnum.اعتباری || f.PayType == PayTypeEnum.چکی) && !f.Vosoled)
        );
    }
}