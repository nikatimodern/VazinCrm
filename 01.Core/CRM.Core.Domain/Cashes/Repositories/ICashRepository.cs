#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.Querying;

namespace CRM.Core.Domain.Cashes.Repositories;
public class CashGroupRow
{
    public int ItemsCount { get; set; }
    public Guid? CashGroupId { get; set; }
    public int GroupNumber { get; set; }
    public string UserCreator { get; set; }
    public string CreateDateTxt { get; set; }
    public decimal TotalAmount { get; set; }
    public CashStatusEnum Status { get; set; }
    public string StatusTxt { get; set; }
    public DateTime CreateDate { get; set; }
}

public class CashierBalanceDto
{
    public Guid CashierId { get; set; }
    public decimal? TotalCredit { get; set; }
    public decimal? TotalDebit { get; set; }
    public bool isAccepted { get; set; }
}

public class CashierRow
{
    public Cashier Cashier { get; set; }
    public List<CashierMappingRow> CashierMappings { get; set; } = [];
    public bool IsAccepted { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal Balance { get; set; }
}

public class CashierMappingRow
{
    public string SoftwareTitle { get; set; }
    public string TafsiliCode { get; set; }
}

public sealed record CashOption<T>(T Value, string Text);
public sealed record CashDetail(Cash Cash, List<int> OrderHeadSerials, List<Attachment> Attachments);
public sealed record CashTable<T>(string? Draw, int RecordsTotal, int RecordsFiltered, List<T> Data);
public sealed record CashListTable(string? Draw, int RecordsTotal, int RecordsFiltered, List<Cash> Data, bool IsExported, object? CashierBalance);
public sealed record CashierOption(Guid Value, string Text, bool Target);
public interface ICashRepository
{
    Task<Cash?> GetAsync(Guid id, bool tracking, CancellationToken ct);
    Task<CashDetail?> GetDetailAsync(Guid id, CancellationToken ct);
    Task<Cashier?> GetCashierAsync(Guid id, bool tracking, CancellationToken ct);
    Task<BaseCash?> GetBaseAsync(Guid id, bool tracking, CancellationToken ct);
    Task<CashGroup?> GetGroupAsync(Guid id, CancellationToken ct);
    Task<List<Cash>> GetGroupItemsAsync(Guid id, bool export, CancellationToken ct);
    Task<List<Cash>> GetDraftsAsync(string userId, CancellationToken ct);
    Task<List<Cash>> GetByIdsAsync(List<Guid> ids, CancellationToken ct);
    Task<Accountancy?> GetDebitAsync(Guid cashId, CancellationToken ct);
    Task<List<CashOption<Guid>>> GetCentersAsync(CancellationToken ct);
    Task<List<CashOption<Guid>>> GetTitlesAsync(bool supply, CancellationToken ct);
    Task<List<CashOption<Guid>>> GetOwnCashiersAsync(string userId, CancellationToken ct);
    Task<List<CashierOption>> GetCashierOptionsAsync(string userId, CancellationToken ct);
    Task<CashierBalanceDto> GetBalanceAsync(Guid userId, CancellationToken ct);
    Task<CashListTable> GetTableAsync(TableRequest table, QueryFilter filter, string userId, bool readAll, int searchNumber, CancellationToken ct);
    Task<CashTable<CashGroupRow>> GetGroupTableAsync(TableRequest table, string userId, bool readAll, int searchNumber, CancellationToken ct);
    Task<CashTable<CashierRow>> GetCashierTableAsync(TableRequest table, CancellationToken ct);
    Task<CashTable<BaseCash>> GetBaseTableAsync(TableRequest table, CancellationToken ct);
    Task<List<int>> GetUsedVoucherNumbersAsync(int start, int count, CancellationToken ct);
    Task<bool> IsDuplicateBaseAsync(BaseCash item, CancellationToken ct);
    void Add(Cash item);
    void Apply(Cash current, Cash input);
    void AddCashier(Cashier item);
    void RemoveCashierMappings(Cashier item);
    void SaveBase(BaseCash item, bool create);
    void AddGroup(CashGroup group);
    void RemoveGroup(CashGroup group);
    void AddDebit(Accountancy item);
    void RemoveDebit(Accountancy item);
    void SaveAttachments(IList<Attachment> attachments);
    Task BeginAsync(CancellationToken ct);
    Task SaveAsync(CancellationToken ct);
    Task CommitAsync(CancellationToken ct);
    Task RollbackAsync(CancellationToken ct);
}
