using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;

namespace CRM.Core.Domain.Accountancies.Repositories;

public interface IDepositFinancialRepository
{
    Task<Accountancy?> GetDepositAsync(Guid id, CancellationToken ct);
    Task<bool> HasAvailableCollectionBankAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<ExternalSoftware>> GetDestinationsAsync(CancellationToken ct);
    Task<List<ExternalMapping>> GetMappingsAsync(Guid id, Guid? customerId, Guid? bankId, CancellationToken ct);
    Task SaveMappingAsync(ExternalMapping mapping, CancellationToken ct);
    Task DeleteMappingAsync(ExternalMapping mapping, CancellationToken ct);
    Task RemoveDestinationMappingAsync(ExternalMapping mapping, CancellationToken ct);
    Task<bool> HasOtherDestinationMappingAsync(ExternalMapping mapping, int destinationId, CancellationToken ct);
    Task<DepositOriginalCheque?> ReadOriginalChequeAsync(IReadOnlyList<ExternalMapping> mappings, CancellationToken ct);
    Task<bool> HasDestinationDependenciesAsync(Guid softwareId, DepositDependencyTarget target, int id,
        CancellationToken ct, DepositDependencyTarget? owner = null, int? ownerId = null);
    Task<IAsyncDisposable> LockAsync(Guid id, CancellationToken ct);
}

public sealed record DepositOriginalCheque(decimal Amount, DateTime Due);

public enum DepositDependencyTarget { Receipt, Draft, Cheque, History, Banking, BankingItem, Payment, PaymentItem, Note, NoteItem, Voucher }
