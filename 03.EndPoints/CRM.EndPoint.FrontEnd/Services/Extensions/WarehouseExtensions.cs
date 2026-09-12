using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.Extensions;

public static class WarehouseExtensions {
    public static readonly Expression<Func<WarehouseTransaction, decimal>>
        StockValueExpression = x => x.TransactionType == WarehouseTransactionTypeEnum.ورود ? x.Weight : -x.Weight;

    extension(IQueryable<WarehouseTransaction> query) {
        public async Task<decimal> GetStockAsync(Guid warehouseProductId)
        {
            return await query
                .Where(x => x.WarehouseProductId == warehouseProductId)
                .SumAsync(StockValueExpression);
        }

        public async Task<decimal> GetStockWithoutTransactionAsync(Guid warehouseProductId, Guid transactionId)
        {
            return await query
                .Where(x => x.WarehouseProductId == warehouseProductId && x.Id != transactionId)
                .SumAsync(StockValueExpression);
        }
    }

    extension(IQueryable<WarehouseProductVoucher> query) {
        public async Task<decimal> GetInCirculationAsync(Guid warehouseProductId, Guid? excludedVoucherId = null)
        {
            var circulationQuery = query
                .Where(x =>
                    x.WarehouseProductId == warehouseProductId &&
                    x.WarehouseVoucher.Status != WarehouseVoucherStatusEnum.تحویل_شده);

            if (excludedVoucherId.HasValue)
            {
                circulationQuery = circulationQuery.Where(x => x.WarehouseVoucherId != excludedVoucherId.Value);
            }

            return await circulationQuery.SumAsync(x => x.Weight);
        }
    }

    extension(CRMContextDb db) {
        public async Task<decimal> GetWarehouseProductStockAsync(Guid warehouseProductId, Guid? excludedTransactionId = null)
        {
            return excludedTransactionId.HasValue
                ? await db.WarehouseTransactions.GetStockWithoutTransactionAsync(warehouseProductId, excludedTransactionId.Value)
                : await db.WarehouseTransactions.GetStockAsync(warehouseProductId);
        }

        public async Task<decimal> GetWarehouseProductAvailableStockAsync(Guid warehouseProductId, Guid? excludedTransactionId = null,
            Guid? excludedVoucherId = null)
        {
            var stock = await db.GetWarehouseProductStockAsync(warehouseProductId, excludedTransactionId);

            var inCirculation = await db.WarehouseProductVouchers.GetInCirculationAsync(warehouseProductId, excludedVoucherId);

            return stock - inCirculation;
        }
    }
}