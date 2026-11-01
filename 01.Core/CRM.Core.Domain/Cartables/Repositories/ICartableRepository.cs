#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Factors.Entities;
using CRM.Core.Domain.Querying;

namespace CRM.Core.Domain.Cartables.Repositories;
public sealed record CartableReadScope(string UserId, bool ReadAll, bool SupplyAdmin);
public sealed record CartablePreFactors(IList<PreFactor> Data, ASetting? Setting);
public sealed record CartableSupplyData(List<PreFactor> PreFactors, Dictionary<int, int> FilterCounts);
public sealed record CartableFinancialData(List<CartableFinancialRow> Data, Dictionary<int, int> FilterCounts);
public sealed record CartableSalesData(List<CartableSalesRow> Data, Dictionary<int, int> FilterCounts);
public class CartableFinancialRow
{
    public PreFactor PreFactor { get; set; } = null !;
    public int NotAcceptedFiAccCount { get; set; }
    public int NotAcceptedMdAccCount { get; set; }
    public int TotalAccCount { get; set; }
    public decimal ReceiveAmount { get; set; }
}

public class CartableSalesRow
{
    public PreFactor PreFactor { get; set; } = null !;
    public decimal ReceiveAmount { get; set; }
    public decimal SumTotalAmountTax { get; set; }
    public decimal PaymentAmount { get; set; }
}

public interface ICartableRepository
{
    Task<ASetting?> GetSettingsAsync(CancellationToken ct);
    Task<IList<PreFactor>> GetPreFactorsAsync(QueryFilter filter, CartableReadScope scope, int searchNumber, CancellationToken ct);
    Task<IList<Factor>> GetFactorsAsync(QueryFilter filter, Guid? customerId, CartableReadScope scope, int searchNumber, CancellationToken ct);
    Task<CartableSupplyData> GetSupplyAsync(string? search, int type, CartableReadScope scope, int searchNumber, CancellationToken ct);
    Task<CartableFinancialData> GetFinancialAsync(string? search, int type, int searchNumber, CancellationToken ct);
    Task<CartableSalesData> GetSalesAsync(string? search, int type, int searchNumber, CancellationToken ct);
    Task<List<PreFactor>> GetOwnPreFactorsAsync(string userId, CancellationToken ct);
    Task<List<Factor>?> GetModalFactorsAsync(Guid id, int type, CancellationToken ct);
    Task<int> GetUnreadChatsAsync(string userId, CancellationToken ct);
}
