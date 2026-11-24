using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql.Sepidar.Stocks.Repositories;

public static class SepidarStockSummary
{
    public static DataTable CreateTable(IEnumerable<(int StockRef, int ItemRef, int? TracingRef)> keys, int fiscalYearId)
    {
        var table = new DataTable();
        table.Columns.Add("StockID", typeof(int));
        table.Columns.Add("ItemID", typeof(int));
        table.Columns.Add("TracingID", typeof(int));
        table.Columns.Add("FiscalYearID", typeof(int));
        table.Columns.Add("FeedFromClosingOperation", typeof(bool));
        foreach (var key in keys.Distinct().OrderBy(x => x.StockRef).ThenBy(x => x.ItemRef).ThenBy(x => x.TracingRef))
            table.Rows.Add(key.StockRef, key.ItemRef, (object?)key.TracingRef ?? DBNull.Value, fiscalYearId, false);
        return table;
    }

    public static Task LockAsync(SepidarContextDbBase db, DataTable keys, CancellationToken ct) =>
        ExecuteAsync(db, "INV.spLockItemStockSummary", keys, false, ct);

    public static Task RecalculateAsync(SepidarContextDbBase db, DataTable keys, CancellationToken ct) =>
        ExecuteAsync(db, "INV.spUpdateItemStockSummary", keys, true, ct);

    private static async Task ExecuteAsync(SepidarContextDbBase db, string procedure, DataTable keys, bool recalculate,
        CancellationToken ct)
    {
        var transaction = db.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction
            ?? throw new InvalidOperationException("Inventory changes require an active Sepidar SQL transaction.");
        var connection = db.Database.GetDbConnection() as SqlConnection
            ?? throw new InvalidOperationException("The Sepidar connection must be a SQL Server connection.");
        await using var command = new SqlCommand(procedure, connection, transaction) { CommandType = CommandType.StoredProcedure };
        command.Parameters.Add(new SqlParameter("@SummaryTable", SqlDbType.Structured) {
            TypeName = "INV.SummaryRecordTable", Value = keys
        });
        if (recalculate)
        {
            command.Parameters.Add("@UpdateSaleWithReserve", SqlDbType.Bit).Value = true;
            command.Parameters.Add("@IncludeRegisteredOrders", SqlDbType.Bit).Value = false;
        }
        await command.ExecuteNonQueryAsync(ct);
    }
}
