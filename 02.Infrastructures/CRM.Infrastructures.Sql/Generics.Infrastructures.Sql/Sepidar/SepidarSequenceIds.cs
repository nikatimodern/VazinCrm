using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Infrastructures.Sql;

// Shared allocation used by SepidarService and invoice exports.
// The visible invoice Number is separate from the internal spGetNextId ID.
public static class SepidarSequenceIds
{
    public static async Task<int> GetNextIdAsync(
        this SepidarContextDbBase context, string tableName,
        int incValue = 1, CancellationToken ct = default)
    {
        var connection = context.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await context.Database.OpenConnectionAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = @"
DECLARE @Id int;
EXEC FMK.spGetNextId @TableName = @TableName, @Id = @Id OUTPUT, @IncValue = @IncValue;
SELECT @Id;";

            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "@TableName";
            tableParameter.Value = tableName;
            command.Parameters.Add(tableParameter);

            var countParameter = command.CreateParameter();
            countParameter.ParameterName = "@IncValue";
            countParameter.Value = incValue;
            command.Parameters.Add(countParameter);

            var transaction = context.Database.CurrentTransaction;
            if (transaction != null)
                command.Transaction = transaction.GetDbTransaction();

            return Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        }
        finally
        {
            if (openedHere)
                await context.Database.CloseConnectionAsync();
        }
    }

    // Call only inside the same serializable transaction that inserts the invoice.
    public static async Task<int> GetNextInvoiceNumberAsync(
        this SepidarContextDbBase context, int fiscalYearId, CancellationToken ct = default)
    {
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("شماره‌گذاری فاکتور باید داخل تراکنش انجام شود.");

        var lastNumber = await context.SalesInvoices.AsNoTracking()
            .Where(x => x.FiscalYearRef == fiscalYearId)
            .MaxAsync(x => (int?)x.Number, ct) ?? 0;
        return checked(lastNumber + 1);
    }
}
