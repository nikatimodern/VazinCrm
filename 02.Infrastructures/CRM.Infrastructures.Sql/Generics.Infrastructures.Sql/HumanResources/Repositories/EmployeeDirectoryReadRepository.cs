using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.HumanResources.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.HumanResources.Repositories;

public sealed class EmployeeDirectoryReadRepository(CRMContextDb db) : IEmployeeDirectoryReadRepository
{
    public async Task<IReadOnlyList<EmployeeDirectoryItem>> SearchAsync(IReadOnlyCollection<Guid> authorizedEmployeeIds,
        string search, int offset, int limit, CancellationToken ct)
    {
        if (authorizedEmployeeIds is null || authorizedEmployeeIds.Count == 0) return Array.Empty<EmployeeDirectoryItem>();
        var ids = authorizedEmployeeIds.Distinct().ToArray();
        var query = db.HrEmployeeProfiles.AsNoTracking().Where(x => !x.IsDeleted && ids.Contains(x.Id));
        var term = search?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            if (term.Length > 100) return Array.Empty<EmployeeDirectoryItem>();
            query = query.Where(x => x.PersonnelCode.Contains(term) || x.FirstName.Contains(term) || x.LastName.Contains(term));
        }
        return await query.OrderBy(x => x.PersonnelCode).ThenBy(x => x.Id)
            .Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100))
            .Select(x => new EmployeeDirectoryItem(x.Id, x.PersonnelCode, x.FirstName, x.LastName)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmploymentHistoryItem>> GetHistoryAsync(Guid employeeId,
        IReadOnlyCollection<Guid> authorizedEmployeeIds, int offset, int limit, CancellationToken ct)
    {
        if (employeeId == Guid.Empty || authorizedEmployeeIds is null || !authorizedEmployeeIds.Contains(employeeId))
            return Array.Empty<EmploymentHistoryItem>();
        return await (from history in db.HrEmploymentRecords.AsNoTracking()
                      join employee in db.HrEmployeeProfiles.AsNoTracking() on history.EmployeeId equals employee.Id
                      where history.EmployeeId == employeeId && !history.IsDeleted && !employee.IsDeleted
                      orderby history.StartDate descending, history.Id
                      select new EmploymentHistoryItem(history.Id, history.StartDate, history.EndDate, history.EmploymentType))
            .Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmployeeAssignmentItem>> GetAssignmentsAsync(Guid employeeId,
        IReadOnlyCollection<Guid> authorizedEmployeeIds, int offset, int limit, CancellationToken ct)
    {
        if (employeeId == Guid.Empty || authorizedEmployeeIds is null || !authorizedEmployeeIds.Contains(employeeId))
            return Array.Empty<EmployeeAssignmentItem>();
        return await (from assignment in db.HrEmployeeAssignments.AsNoTracking()
                      join employee in db.HrEmployeeProfiles.AsNoTracking() on assignment.EmployeeId equals employee.Id
                      where assignment.EmployeeId == employeeId && !assignment.IsDeleted && !employee.IsDeleted
                      orderby assignment.EffectiveFrom descending, assignment.Id
                      select new EmployeeAssignmentItem(assignment.Id, assignment.DepartmentId, assignment.PositionId,
                          assignment.EffectiveFrom, assignment.EffectiveTo, assignment.IsPrimary))
            .Skip(Math.Max(0, offset)).Take(Math.Clamp(limit, 1, 100)).ToListAsync(ct);
    }
}
