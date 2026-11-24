using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.Infrastructures.Sql.Accountancies.Repositories;

public sealed class DepositFinancialRepository(CRMContextDb db, Sepidar1ContextDb first, Sepidar2ContextDb second)
    : IDepositFinancialRepository
{
    public Task<Accountancy?> GetDepositAsync(Guid id, CancellationToken ct) =>
        db.Accountancies.AsNoTracking().Include(x => x.AccountancyTransfer)
            .SingleOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ExternalSoftware>> GetDestinationsAsync(CancellationToken ct) =>
        await db.ExternalSoftwares.AsNoTracking().OrderBy(x => x.Title).ToListAsync(ct);

    public Task<List<ExternalMapping>> GetMappingsAsync(Guid id, Guid? customerId, Guid? bankId, CancellationToken ct) =>
        db.ExternalMappings.AsNoTracking().Where(x =>
            (x.EntityId == id && x.MapType == ExternalMappingTypeEnum.واریزی && x.EntityName == nameof(Accountancy))
            || (x.EntityId == customerId && (x.MapType == ExternalMappingTypeEnum.مشتری
                || x.MapType == ExternalMappingTypeEnum.تامین_کننده) && x.EntityName == "Customer")
            || (x.EntityId == bankId && x.MapType == ExternalMappingTypeEnum.حساب_بانکی && x.EntityName == nameof(BankAccount)))
            .ToListAsync(ct);

    public async Task SaveMappingAsync(ExternalMapping mapping, CancellationToken ct)
    {
        if (db.Entry(mapping).State == EntityState.Detached)
        {
            var stored = await db.ExternalMappings.SingleOrDefaultAsync(x => x.Id == mapping.Id, ct);
            if (stored == null) db.ExternalMappings.Add(mapping);
            else stored.ExEntityId = mapping.ExEntityId;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteMappingAsync(ExternalMapping mapping, CancellationToken ct)
    {
        db.ExternalMappings.Remove(mapping);
        await db.SaveChangesAsync(ct);
    }

    public Task<IAsyncDisposable> LockAsync(Guid id, CancellationToken ct) => ExternalSoftwares.FinancialMappingLock.AcquireAsync(db, id, ct);

    public async Task RemoveDestinationMappingAsync(ExternalMapping mapping, CancellationToken ct)
    {
        var removed = await db.ExternalMappings.Where(x => x.Id == mapping.Id && x.EntityId == mapping.EntityId &&
            x.ExternalSoftwareId == mapping.ExternalSoftwareId && x.MapType == mapping.MapType &&
            x.EntityName == mapping.EntityName && x.ExEntityName == mapping.ExEntityName && x.ExEntityId == mapping.ExEntityId)
            .ExecuteDeleteAsync(ct);
        if (removed == 0 && await db.ExternalMappings.AnyAsync(x => x.Id == mapping.Id, ct))
            throw new InvalidOperationException("سند مقصد حذف شد، اما ارتباط آن در ERP تغییر کرده است.");
    }

    public Task<bool> HasOtherDestinationMappingAsync(ExternalMapping mapping, int destinationId, CancellationToken ct)
    {
        var id = destinationId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var pending = ExternalMapping.PendingIdPrefix + id;
        return db.ExternalMappings.AnyAsync(x => x.Id != mapping.Id && x.ExternalSoftwareId == mapping.ExternalSoftwareId &&
            x.ExEntityName == mapping.ExEntityName && (x.ExEntityId == id || x.ExEntityId == pending), ct);
    }

    public async Task<bool> HasDestinationDependenciesAsync(Guid softwareId, DepositDependencyTarget target, int id,
        CancellationToken ct, DepositDependencyTarget? owner = null, int? ownerId = null)
    {
        SepidarContextDbBase destination = softwareId == Sepidar1SoftwareId ? first : softwareId == Sepidar2SoftwareId ? second
            : throw new InvalidOperationException("نرم‌افزار مالی مقصد معتبر نیست.");
        var (table, _) = ReferenceTable(target);
        var references = await destination.Database.SqlQuery<DepositReference>($"""
            SELECT OBJECT_SCHEMA_NAME(k.parent_object_id) AS SchemaName,
                OBJECT_NAME(k.parent_object_id) AS TableName, COL_NAME(k.parent_object_id,k.parent_column_id) AS ColumnName
            FROM sys.foreign_key_columns k
            WHERE k.referenced_object_id = OBJECT_ID({table})
            """).ToListAsync(ct);
        using var identifiers = new SqlCommandBuilder();
        var exclusions = target switch
        {
            DepositDependencyTarget.Receipt => new[] { "RPA.ReceiptDraft", "RPA.ReceiptCheque" },
            DepositDependencyTarget.Cheque or DepositDependencyTarget.BankingItem or DepositDependencyTarget.PaymentItem =>
                new[] { "RPA.ReceiptChequeHistory" },
            DepositDependencyTarget.Banking => new[] { "RPA.ReceiptChequeBankingItem" },
            DepositDependencyTarget.Payment => new[] { "RPA.PaymentChequeOther" },
            DepositDependencyTarget.Note => new[] { "GNR.DebitCreditNoteItem" },
            DepositDependencyTarget.Voucher => new[] { "ACC.VoucherItem" },
            _ => Array.Empty<string>()
        };
        var clauses = new List<string>();
        foreach (var reference in references)
        {
            var name = reference.SchemaName + "." + reference.TableName;
            if (exclusions.Contains(name, StringComparer.OrdinalIgnoreCase))
                continue;
            var sql = "EXISTS (SELECT 1 FROM " + identifiers.QuoteIdentifier(reference.SchemaName) + "." +
                identifiers.QuoteIdentifier(reference.TableName) + " WHERE " + identifiers.QuoteIdentifier(reference.ColumnName) + "={0}";
            if (owner.HasValue && ownerId.HasValue)
            {
                var (ownerTable, ownerKey) = ReferenceTable(owner.Value);
                if (name.Equals(ownerTable, StringComparison.OrdinalIgnoreCase) &&
                    reference.ColumnName.Equals("VoucherRef", StringComparison.OrdinalIgnoreCase))
                    sql += " AND " + identifiers.QuoteIdentifier(ownerKey) + "<>{1}";
            }
            clauses.Add(sql + ")");
        }
        if (clauses.Count == 0)
            return false;
        var query = FormattableStringFactory.Create("SELECT CAST(CASE WHEN " + string.Join(" OR ", clauses) +
            " THEN 1 ELSE 0 END AS bit) AS [Value]", id, ownerId ?? 0);
        return await destination.Database.SqlQuery<bool>(query).SingleAsync(ct);
    }

    private static (string Table, string Key) ReferenceTable(DepositDependencyTarget target) => target switch
    {
        DepositDependencyTarget.Receipt => ("RPA.ReceiptHeader", "ReceiptHeaderId"),
        DepositDependencyTarget.Draft => ("RPA.ReceiptDraft", "ReceiptDraftId"),
        DepositDependencyTarget.Cheque => ("RPA.ReceiptCheque", "ReceiptChequeId"),
        DepositDependencyTarget.History => ("RPA.ReceiptChequeHistory", "ReceiptChequeHistoryId"),
        DepositDependencyTarget.Banking => ("RPA.ReceiptChequeBanking", "ReceiptChequeBankingId"),
        DepositDependencyTarget.BankingItem => ("RPA.ReceiptChequeBankingItem", "ReceiptChequeBankingItemId"),
        DepositDependencyTarget.Payment => ("RPA.PaymentHeader", "PaymentHeaderId"),
        DepositDependencyTarget.PaymentItem => ("RPA.PaymentChequeOther", "PaymentChequeOtherId"),
        DepositDependencyTarget.Note => ("GNR.DebitCreditNote", "DebitCreditNoteId"),
        DepositDependencyTarget.NoteItem => ("GNR.DebitCreditNoteItem", "DebitCreditNoteItemId"),
        DepositDependencyTarget.Voucher => ("ACC.Voucher", "VoucherId"),
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private sealed record DepositReference(string SchemaName, string TableName, string ColumnName);
}
