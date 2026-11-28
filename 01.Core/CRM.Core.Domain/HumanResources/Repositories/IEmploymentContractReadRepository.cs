using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.HumanResources.Entities;

namespace CRM.Core.Domain.HumanResources.Repositories;

public sealed record EmploymentContractItem(Guid Id, Guid EmployeeId, string PersonnelCode,
    string FirstName, string LastName, string ContractNumber, EmploymentContractTerm Term,
    DateOnly AgreementDate, DateOnly StartDate, DateOnly? EndDate, EmploymentContractStatus Status,
    DateOnly? TerminatedOn, Guid? PreviousContractId, byte[] RowVersion);

// Scope must be resolved on the server for the contract-specific permission.
// Empty scope denies all access. No confidential payroll or file content is returned.
public interface IEmploymentContractReadRepository
{
    Task<IReadOnlyList<EmploymentContractItem>> GetEmployeeContractsAsync(Guid employeeId,
        IReadOnlyCollection<Guid> authorizedEmployeeIds, int offset, int limit, CancellationToken ct);

    Task<IReadOnlyList<EmploymentContractItem>> GetApprovedEndingBetweenAsync(
        IReadOnlyCollection<Guid> authorizedEmployeeIds, DateOnly from, DateOnly through,
        int offset, int limit, CancellationToken ct);
}
