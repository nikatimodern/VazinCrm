#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Cartables.Enums;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Deals.Entities;
using CRM.Core.Domain.Querying;

namespace CRM.Core.Domain.Accountancies.Repositories;
public sealed record AccountancyDetails(Accountancy? AccData, List<Attachment> ListAttach);
public sealed record AccountancyCartable(List<Accountancy> Items, Dictionary<int, int> FilterCounts);
public sealed record AccountancyPayments(List<Accountancy> Items, PreFactor? PreFactor, Deal? Deal);
public sealed record AccountancyTable(string? Draw, int RecordsFiltered, int RecordsTotal, List<Accountancy> Data);
public sealed record BankAccountTable(string? Draw, int RecordsFiltered, int RecordsTotal, List<BankAccount> Data);
public sealed record InvoiceTotals(decimal Discount, decimal TotalAmount, decimal Tax, decimal Received, decimal Returned);
public interface IAccountancyRepository
{
    Task<Accountancy?> GetAsync(Guid id, bool parent, bool tracking, CancellationToken ct);
    Task<List<Attachment>> GetAttachmentsAsync(Guid id, CancellationToken ct);
    Task<PreFactor?> GetPreFactorAsync(Guid? id, CancellationToken ct);
    Task<InvoiceTotals?> GetTotalsAsync(int type, Guid? id, CancellationToken ct);
    Task<AccountancyCartable> GetCartableAsync(string? search, int searchNumber, CartableEnum cartable, int type, PayTypeEnum? payType, CancellationToken ct);
    Task<AccountancyPayments> GetPaymentsAsync(int type, Guid id, bool depositsOnly, bool realizedDepositsOnly, bool returnsOnly, CancellationToken ct);
    Task<List<Accountancy>> GetWalletAsync(Guid customerId, CancellationToken ct);
    Task<AccountancyTable> GetTableAsync(TableRequest table, QueryFilter filter, int accType, int payType, string userId, bool readAll, CancellationToken ct);
    Task<List<BankAccount>> GetBankAccountsAsync(BankAccountTypeEnum? type, CancellationToken ct);
    Task<BankAccountTable> GetBankTableAsync(TableRequest table, int? type, CancellationToken ct);
    Task<BankAccount?> GetBankAsync(Guid id, bool tracking, CancellationToken ct);
    Task<List<Accountancy>> GetPendingAsync(Guid pfId, CancellationToken ct);
    Task<AccountancyTransfer?> GetTransferAsync(Guid accountancyId, CancellationToken ct);
    Task SaveTransferAsync(AccountancyTransfer transfer, CancellationToken ct);
    Task<bool> RemoveTransferAsync(Guid accountancyId, CancellationToken ct);
    Task<bool> RemoveVosolAsync(Guid accountancyId, CancellationToken ct);
    void SaveBank(BankAccount item, bool create);
    Task RemoveTasksAsync(Guid id, CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
}
