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
        return await ReadAsync(new[] { employeeId }, null, null, offset, limit, ct);
    }

    public async Task<IReadOnlyList<EmploymentContractItem>> GetApprovedEndingBetweenAsync(
        IReadOnlyCollection<Guid> authorizedEmployeeIds, DateOnly from, DateOnly through,
        int offset, int limit, CancellationToken ct)
    {
        if (authorizedEmployeeIds is null || authorizedEmployeeIds.Count == 0 || through < from)
            return Array.Empty<EmploymentContractItem>();
        return await ReadAsync(authorizedEmployeeIds.Distinct().ToArray(), from, through, offset, limit, ct);
    }

    private async Task<IReadOnlyList<EmploymentContractItem>> ReadAsync(Guid[] employeeIds,
        DateOnly? from, DateOnly? through, int offset, int limit, CancellationToken ct)
    {
        var query =
            from contract in db.HrEmploymentContracts.AsNoTracking()
            join employee in db.HrEmployeeProfiles.AsNoTracking() on contract.EmployeeId equals employee.Id
            where employeeIds.Contains(employee.Id) && !employee.IsDeleted && !contract.IsDeleted
            select new { Contract = contract, Employee = employee };

        // Apply all filters and ordering to mapped entities before constructing the DTO.
        if (from.HasValue && through.HasValue)
        {
            var start = from.Value;
            var end = through.Value;
            query = query.Where(x => x.Contract.Status == EmploymentContractStatus.Approved &&
                x.Contract.EndDate.HasValue && x.Contract.EndDate.Value >= start &&
                x.Contract.EndDate.Value <= end);
        }

        var ordered = from.HasValue && through.HasValue
            ? query.OrderBy(x => x.Contract.EndDate).ThenBy(x => x.Contract.Id)
            : query.OrderByDescending(x => x.Contract.StartDate).ThenBy(x => x.Contract.Id);

        return await ordered.Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100))
            .Select(x => new EmploymentContractItem(x.Contract.Id, x.Employee.Id, x.Employee.PersonnelCode,
                x.Employee.FirstName, x.Employee.LastName, x.Contract.ContractNumber, x.Contract.Term,
                x.Contract.AgreementDate, x.Contract.StartDate, x.Contract.EndDate, x.Contract.Status,
                x.Contract.TerminatedOn, x.Contract.PreviousContractId, EF.Property<byte[]>(x.Contract, "RowVersion")))
            .ToListAsync(ct);
    }
}
