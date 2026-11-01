#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Orders.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Company.Entities;
using CRM.Core.Domain.Querying;

namespace CRM.Core.Domain.Orders.Repositories;
public sealed record OrderReader(string UserId, bool ReadAll, bool ReadAssigned);
public sealed record OrderOption(Guid Id, string Text);
public sealed record HeadSelection(List<Guid>? FilterIds, List<Guid> SelectedIds, string? Search);
public sealed record OrderTableResult(string? Draw, int RecordsFiltered, int RecordsTotal, List<Order> Data);
public sealed record OrderHeadTableResult(string? Draw, int RecordsFiltered, int RecordsTotal, List<OrderHead> Data);
public sealed record OrderPrintData(CRM.Core.Domain.Company.Entities.Company? Company, OrderHead? OrderHead, PreFactor? PreFactor);
public interface IOrderRepository
{
    Task<List<OrderHead>> GetHeadsByPreFactorAsync(Guid id, bool undelivered, CancellationToken cancellationToken);
    Task<List<OrderHead>> GetHeadsByDealAsync(Guid id, CancellationToken cancellationToken);
    Task<List<OrderOption>> GetPreFactorOptionsAsync(Guid customerId, CancellationToken cancellationToken);
    Task<List<OrderOption>> GetFactorOptionsAsync(Guid customerId, CancellationToken cancellationToken);
    Task<List<OrderOption>> GetCustomerOptionsAsync(OrderReader reader, CancellationToken cancellationToken);
    Task<List<OrderHead>> GetBoardAsync(QueryFilter filter, int invoiceStatus, OrderReader reader, CancellationToken cancellationToken);
    Task<OrderHead?> GetHeadAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<Order?> GetItemAsync(Guid id, bool tracking, CancellationToken cancellationToken);
    Task<List<PreFactorItem>> GetMissingItemsAsync(Guid pfId, CancellationToken cancellationToken);
    Task<List<Guid>> GetVisibleHeadIdsAsync(OrderReader reader, CancellationToken cancellationToken);
    Task<List<OrderHead>> SelectHeadsAsync(HeadSelection selection, CancellationToken cancellationToken);
    Task<bool> AllowMultipleSelectionAsync(CancellationToken cancellationToken);
    Task<OrderTableResult> GetItemTableAsync(Guid id, TableRequest request, CancellationToken cancellationToken);
    Task<OrderHeadTableResult> GetHeadTableAsync(TableRequest request, CancellationToken cancellationToken);
    Task<OrderPrintData> GetPrintDataAsync(Guid id, CancellationToken cancellationToken);
    Task<string?> GetCustomerMobileAsync(OrderHead head, CancellationToken cancellationToken);
    Task SaveHeadAsync(OrderHead head, bool create, CancellationToken cancellationToken);
    Task SaveItemAsync(Order item, bool create, CancellationToken cancellationToken);
    Task TouchDealAsync(Guid id, DateTime time, string text, CancellationToken cancellationToken);
    Task BeginAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
}
