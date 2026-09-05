using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CRM.Core.Domain.Warehouses.Entities;
using CRM.Core.Domain.Warehouses.Enums;
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
}