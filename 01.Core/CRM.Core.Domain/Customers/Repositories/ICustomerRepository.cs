#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
namespace CRM.Core.Domain.Customers.Repositories;

public sealed record CustomerOption(Guid Id, string Text);
public sealed record CustomerSupplier(Guid Id, string? CompanyName, string? AgentName, int CustomerCode);
public sealed record CustomerSummary(Guid Id, string? Mobile, string? Tel, int RoleType, string CustomerName, int CustomerCode, string? CompanyName, string? AgentName);
public sealed record CustomerPhone(Guid Id, string Name, string? Phone, int CustomerCode, string? Mobile, DateTime? ModifyDate, string? NormalizedAgentName, string? NormalizedCompanyName);
public sealed record CustomerTableRequest(string? Draw, int Skip, int Take, string? SortColumn, bool Ascending, string? Search, byte Juridical);
public sealed record CustomerTableResult(string? Draw, int RecordsFiltered, int RecordsTotal, List<Customer> Data);
public sealed record CustomerListFilter(int Skip, int Take, int RoleType, int Juridical, string? Search = null, string[]? Keywords = null);
public sealed record CustomerStatus(bool Active, bool Export);
public interface ICustomerRepository
{
    Task<Customer?> GetAsync(Guid id, bool includeDeleted, bool includeRelations, CancellationToken cancellationToken);
    Task<Customer?> GetByMobileAsync(string mobile, CancellationToken cancellationToken);
    Task<List<CustomerOption>> SearchOptionsAsync(string search, int? roleType, CancellationToken cancellationToken);
    Task<List<CustomerSupplier>> GetSuppliersAsync(CancellationToken cancellationToken);
    Task<List<Customer>> GetRelatedAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Customer>> GetDuplicatesAsync(Customer model, CancellationToken cancellationToken);
    Task<bool> HasLegalDuplicateAsync(Customer model, CancellationToken cancellationToken);
    Task<bool> HasAssignmentAsync(Guid customerId, Guid userId, CancellationToken cancellationToken);
    Task RemoveAssignmentAsync(Guid customerId, Guid userId, CancellationToken cancellationToken);
    Task AddAssignmentsAsync(Guid customerId, IReadOnlyList<Guid> userIds, CancellationToken cancellationToken);
    Task<List<AppUser>> GetAssignedUsersAsync(Guid customerId, CancellationToken cancellationToken);
    Task<string> GetUserNamesAsync(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken);
    Task<List<Customer>> GetListAsync(CustomerListFilter filter, string userId, bool readAll, CancellationToken cancellationToken);
    Task<CustomerTableResult> GetTableAsync(CustomerTableRequest filter, CancellationToken cancellationToken);
    Task<List<CustomerSummary>> GetSummariesAsync(CancellationToken cancellationToken);
    Task<List<CustomerPhone>> GetPhonebookAsync(string search, string[] keywords, CancellationToken cancellationToken);
    Task<List<Customer>> GetForNormalizationAsync(CancellationToken cancellationToken);
    Task<ASetting?> GetSettingsAsync(CancellationToken cancellationToken);
    Task<CustomerStatus> GetStatusAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteWithPreFactorsAsync(Customer model, CancellationToken cancellationToken);
    Task UpdateAsync(Customer model, CancellationToken cancellationToken);
    Task SyncDealsAsync(Customer model, CancellationToken cancellationToken);
    Task AddAsync(Customer model, Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task BeginWriteAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task<List<PreFactor>> GetPreFactorsAsync(Guid? customerId, Guid? dealId, bool includeDeleted, CancellationToken cancellationToken);
    Task<PreFactor?> GetPreFactorAsync(Guid id, CancellationToken cancellationToken);
    Task ChangePreFactorUsersAsync(Guid pfId, AppUser? salesUser, AppUser? supplyUser, CancellationToken cancellationToken);
}
public interface ICustomerExternalRepository
{
    Task<List<Invoice>> GetInvoicesAsync(int serialNumber, bool includeNotes, CancellationToken cancellationToken);
    Task<Dictionary<Guid, int>?> GetInvoiceCountsAsync(IReadOnlyList<PreFactor> preFactors, CancellationToken cancellationToken);
}
