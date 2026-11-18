using System.Linq;
using CRM.EndPoint.FrontEnd.Models.Supply;
using CRM.Core.Domain.RequestAmounts.Enums;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Extensions;

public static class SupplierFinancialExtensions {
    public static IQueryable<SupplierFinancialSummaryVModel> GetSupplierFinancialSummaries(this CRMContextDb db)
    {
        var supplyAmounts = db.SupplyFactors
            .AsNoTracking()
            .GroupBy(x => x.SupplierId)
            .Select(g => new {
                SupplierId = g.Key,
                SupplyAmount = g.Sum(x => x.PaymentAmount),
                PaidAmount = 0m
            });

        var paidAmounts = db.RequestAmountPayments
            .AsNoTracking()
            .Where(x => x.RequestAmount.TargetPersonId.HasValue
                && x.RequestAmount.RequestAmountType != RequestAmountTypeEnum.تنخواه
                && x.RequestAmount.RequestAmountType != RequestAmountTypeEnum.عودت_به_مشتری)
            .GroupBy(x => x.RequestAmount.TargetPersonId!.Value)
            .Select(g => new {
                SupplierId = g.Key,
                SupplyAmount = 0m,
                PaidAmount = g.Sum(x => x.Amount)
            });

        return supplyAmounts
            .Concat(paidAmounts)
            .GroupBy(x => x.SupplierId)
            .Select(g => new SupplierFinancialSummaryVModel {
                SupplierId = g.Key,
                TotalSupplyAmount = g.Sum(x => x.SupplyAmount),
                TotalPaidAmount = g.Sum(x => x.PaidAmount),
                SupplierDebt = g.Sum(x => x.SupplyAmount) - g.Sum(x => x.PaidAmount)
            });

    }
}
