using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.HumanResources.Entities;
using CRM.Core.Domain.HumanResources.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.HumanResources.Repositories;

public sealed class EmploymentContractReadRepository(CRMContextDb db) : IEmploymentContractReadRepository
{
    public async Task<IReadOnlyList<EmploymentContractItem>> GetEmployeeContractsAsync(Guid employeeId,
        IReadOnlyCollection<Guid> authorizedEmployeeIds, int offset, int limit, CancellationToken ct)
    {
        if (employeeId == Guid.Empty || authorizedEmployeeIds is null || !authorizedEmployeeIds.Contains(employeeId))
            return Array.Empty<EmploymentContractItem>();
        return await Scoped(new[] { employeeId }).OrderByDescending(x => x.StartDate).ThenBy(x => x.Id)
            .Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmploymentContractItem>> GetApprovedEndingBetweenAsync(
        IReadOnlyCollection<Guid> authorizedEmployeeIds, DateOnly from, DateOnly through,
        int offset, int limit, CancellationToken ct)
    {
        if (authorizedEmployeeIds is null || authorizedEmployeeIds.Count == 0 || through < from)
            return Array.Empty<EmploymentContractItem>();
        return await Scoped(authorizedEmployeeIds.Distinct().ToArray())
            .Where(x => x.Status == EmploymentContractStatus.Approved && x.EndDate.HasValue &&
                        x.EndDate.Value >= from && x.EndDate.Value <= through)
            .OrderBy(x => x.EndDate).ThenBy(x => x.Id)
            .Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct);
    }

    private IQueryable<EmploymentContractItem> Scoped(Guid[] employeeIds) =>
        from contract in db.HrEmploymentContracts.AsNoTracking()
        join employee in db.HrEmployeeProfiles.AsNoTracking() on contract.EmployeeId equals employee.Id
        where employeeIds.Contains(employee.Id) && !employee.IsDeleted && !contract.IsDeleted
        select new EmploymentContractItem(contract.Id, employee.Id, employee.PersonnelCode,
            employee.FirstName, employee.LastName, contract.ContractNumber, contract.Term,
            contract.AgreementDate, contract.StartDate, contract.EndDate, contract.Status,
            contract.TerminatedOn, contract.PreviousContractId);
}
