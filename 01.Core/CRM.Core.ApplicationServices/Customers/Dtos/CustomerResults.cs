using CRM.Core.Domain.Customers.Entities;
namespace CRM.Core.ApplicationServices.Customers.Dtos;

public sealed record CustomerListResult(List<Customer> Customers, bool CanAccept);
public sealed record CustomerProfileResult(int Status, Guid Id = default, string? Name = null, string? SalesExperts = null);
public sealed record CustomerHtmlResult(int Status, int Count = 0, string Html = "");
public sealed record CustomerMutationResult(int Status, object? Value = null);
public sealed record PreFactorUserOption(Guid Value, string Text, bool Target);
public sealed record PreFactorUsersResult(List<PreFactorUserOption> SalesUsers, List<PreFactorUserOption> TaminUsers);
