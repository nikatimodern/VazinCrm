using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
                new EmployeePersonalDetails(x.FatherName, x.NationalCode, x.BirthCertificateNumber, x.BirthDate, x.Mobile, x.Telephone, x.Email, x.Address, x.Education, x.EmergencyContactName, x.EmergencyContactPhone, x.Latitude, x.Longitude), x.EducationDocumentContent != null)).SingleOrDefaultAsync(ct);
    }

    public async Task<EmployeeEducationDocument> GetEducationDocumentAsync(Guid id, IReadOnlyCollection<Guid> allowedIds, CancellationToken ct)
    {
        if (allowedIds is null || !allowedIds.Contains(id)) return null;
        return await db.HrEmployeeProfiles.AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted && x.EducationDocumentContent != null)
            .Select(x => new EmployeeEducationDocument(x.EducationDocumentContent, x.EducationDocumentType))
            .SingleOrDefaultAsync(ct);
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
        // Serialize allocation, including soft-deleted rows, and keep the existing unique index.
        // Numeric legacy codes are preserved; the next code starts at 1001 or above the maximum.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        EmployeeProfile entity;
        try
        {
            var maximum = (await db.Database.SqlQueryRaw<long>(
                "SELECT COALESCE(MAX(TRY_CONVERT(bigint, [PersonnelCode])), CONVERT(bigint, 1000)) AS [Value] FROM [HrEmployeeProfiles] WITH (UPDLOCK, HOLDLOCK)")
                .ToListAsync(ct)).Single();
            if (maximum == long.MaxValue) return new(EmployeeSaveStatus.Conflict);
            var code = (Math.Max(1000L, maximum) + 1).ToString(CultureInfo.InvariantCulture);
            entity = new EmployeeProfile(code, first, last, accountId);
            entity.SetPersonalDetails(input.PersonalDetails ?? new EmployeePersonalDetails());
            if (input.EducationDocument != null) entity.SetEducationDocument(input.EducationDocument);
        }
        catch (ArgumentException) { return new(EmployeeSaveStatus.Invalid); }
        catch (SqlException ex) when (ex.Number == 1205) { return new(EmployeeSaveStatus.Conflict); }
        entity.UserCreatorID = actorId;
        entity.UserModifyID = actorId;
        db.HrEmployeeProfiles.Add(entity);
        var result = await PersistAsync(entity, ct);
        if (result.Status == EmployeeSaveStatus.Saved) await transaction.CommitAsync(ct);
        return result;
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
            if (input.EducationDocument != null) entity.SetEducationDocument(input.EducationDocument);
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
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 1205 })
        {
            db.Entry(entity).State = EntityState.Detached;
            return new(EmployeeSaveStatus.Conflict);
        }
    }
}
