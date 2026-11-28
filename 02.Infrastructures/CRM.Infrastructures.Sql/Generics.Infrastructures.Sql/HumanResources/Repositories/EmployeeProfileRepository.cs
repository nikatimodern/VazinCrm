using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.HumanResources.Entities;
using CRM.Core.Domain.HumanResources.Repositories;
using Generics.Infrastructures.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.HumanResources.Repositories;

// No Identity writes; account verification is a separate read across contexts.
// Database uniqueness is the final guard against concurrent duplicate imports.
public sealed class EmployeeProfileRepository(CRMContextDb db, UserContextDb users) : IEmployeeProfileRepository
{
    public async Task<EmployeeProfileDetail> GetAsync(Guid id, IReadOnlyCollection<Guid> allowedIds, CancellationToken ct)
    {
        if (allowedIds is null || !allowedIds.Contains(id)) return null;
        return await db.HrEmployeeProfiles.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new EmployeeProfileDetail(x.Id, x.PersonnelCode, x.FirstName, x.LastName,
                x.AppUserId != null, EF.Property<byte[]>(x, "RowVersion"),
                new EmployeePersonalDetails(x.FatherName, x.NationalCode, x.BirthCertificateNumber, x.BirthDate, x.Mobile, x.Telephone, x.Email, x.Address, x.Education, x.EmergencyContactName, x.EmergencyContactPhone))).SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<EmployeeAccountCandidate>> GetCandidatesAsync(IReadOnlyCollection<string> allowedAccountIds, CancellationToken ct)
    {
        if (allowedAccountIds is null || allowedAccountIds.Count == 0) return Array.Empty<EmployeeAccountCandidate>();
        var ids = allowedAccountIds.Distinct().ToArray();
        var linked = await db.HrEmployeeProfiles.AsNoTracking().Where(x => x.AppUserId != null && ids.Contains(x.AppUserId))
            .Select(x => x.AppUserId).ToListAsync(ct);
        return await users.Users.AsNoTracking().Where(x => x.IsEmployee && ids.Contains(x.Id) && !linked.Contains(x.Id))
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id).Take(100)
            .Select(x => new EmployeeAccountCandidate(x.Id, x.FirstName, x.LastName)).ToListAsync(ct);
    }

    public async Task<EmployeeSaveResult> CreateAsync(CreateEmployeeProfileInput input, string actorId,
        IReadOnlyCollection<string> allowedAccountIds, CancellationToken ct)
    {
        if (input is null || string.IsNullOrWhiteSpace(actorId)) return new(EmployeeSaveStatus.Invalid);
        var accountId = string.IsNullOrWhiteSpace(input.AppUserId) ? null : input.AppUserId.Trim();
        var first = input.FirstName;
        var last = input.LastName;
        if (accountId != null)
        {
            if (allowedAccountIds is null || !allowedAccountIds.Contains(accountId)) return new(EmployeeSaveStatus.Denied);
            var account = await users.Users.AsNoTracking().Where(x => x.Id == accountId && x.IsEmployee)
                .Select(x => new { x.FirstName, x.LastName }).SingleOrDefaultAsync(ct);
            if (account is null) return new(EmployeeSaveStatus.Invalid);
            first = account.FirstName;
            last = account.LastName;
        }
        EmployeeProfile entity;
        try
        {
            entity = new EmployeeProfile(input.PersonnelCode, first, last, accountId);
            entity.SetPersonalDetails(input.PersonalDetails ?? new EmployeePersonalDetails());
        }
        catch (ArgumentException) { return new(EmployeeSaveStatus.Invalid); }
        entity.UserCreatorID = actorId;
        entity.UserModifyID = actorId;
        db.HrEmployeeProfiles.Add(entity);
        return await PersistAsync(entity, ct);
    }

    public async Task<EmployeeSaveResult> EditAsync(EditEmployeeProfileInput input, string actorId,
        IReadOnlyCollection<Guid> allowedIds, CancellationToken ct)
    {
        if (input is null || string.IsNullOrWhiteSpace(actorId) || input.RowVersion?.Length != 8)
            return new(EmployeeSaveStatus.Invalid);
        if (allowedIds is null || !allowedIds.Contains(input.Id)) return new(EmployeeSaveStatus.Denied);
        var entity = await db.HrEmployeeProfiles.SingleOrDefaultAsync(x => x.Id == input.Id && !x.IsDeleted, ct);
        if (entity is null) return new(EmployeeSaveStatus.NotFound);
        try
        {
            entity.SetName(input.FirstName, input.LastName);
            entity.SetPersonalDetails(input.PersonalDetails ?? new EmployeePersonalDetails());
        }
        catch (ArgumentException)
        {
            db.Entry(entity).State = EntityState.Detached;
            return new(EmployeeSaveStatus.Invalid);
        }
        db.Entry(entity).Property<byte[]>("RowVersion").OriginalValue = input.RowVersion;
        entity.UserModifyID = actorId;
        entity.ModifyDate = DateTime.Now;
        entity.ModifyDateTxt = CRM.Framework.BaseServices.GetDateTimePersian();
        return await PersistAsync(entity, ct);
    }

    private async Task<EmployeeSaveResult> PersistAsync(EmployeeProfile entity, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return new(EmployeeSaveStatus.Saved, entity.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.Entry(entity).State = EntityState.Detached;
            return new(EmployeeSaveStatus.Conflict);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            db.Entry(entity).State = EntityState.Detached;
            return new(EmployeeSaveStatus.Conflict);
        }
    }
}
