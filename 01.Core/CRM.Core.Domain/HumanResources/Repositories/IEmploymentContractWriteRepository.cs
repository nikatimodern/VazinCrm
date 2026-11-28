using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.HumanResources.Entities;

namespace CRM.Core.Domain.HumanResources.Repositories;

public enum ContractOperation { Create = 1, Edit = 2, Renew = 3, Approve = 4, Cancel = 5, Terminate = 6 }
public enum ContractSaveStatus { Saved, Invalid, Denied, NotFound, Conflict, Overlap, InvalidPredecessor }
public sealed record ContractSaveResult(ContractSaveStatus Status, Guid? Id = null);
public sealed record ContractChange(Guid EmployeeId, Guid? ContractId, ContractOperation Operation,
    string Number, EmploymentContractTerm Term, DateOnly AgreementDate, DateOnly StartDate,
    DateOnly? EndDate, DateOnly? TerminationDate, string Reason, byte[] RowVersion);

public interface IEmploymentContractWriteRepository
{
    // Call only after server-side contract-specific permission and employee scope validation.
    Task<ContractSaveResult> SaveAsync(ContractChange change, string actorId, CancellationToken ct);
}
