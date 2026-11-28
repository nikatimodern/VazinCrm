using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.HumanResources.Entities;
using CRM.Core.Domain.HumanResources.Repositories;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.Infrastructures.Sql.HumanResources.Repositories;

public sealed class EmploymentContractWriteRepository(CRMContextDb db) : IEmploymentContractWriteRepository
{
    public async Task<ContractSaveResult> SaveAsync(ContractChange change, string actorId, CancellationToken ct)
    {
        if (change is null || change.EmployeeId == Guid.Empty || string.IsNullOrWhiteSpace(actorId) ||
            !Enum.IsDefined(change.Operation))
            return new(ContractSaveStatus.Invalid);
        if (change.Operation == ContractOperation.Create
            ? change.ContractId.HasValue
            : !change.ContractId.HasValue || change.RowVersion?.Length != 8)
            return new(ContractSaveStatus.Invalid);

        // Serializable protects approval overlap and successor checks from concurrent writers.
        // CRMContextDb does not currently enable the SQL retry execution strategy.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (!await db.HrEmployeeProfiles.AnyAsync(x => x.Id == change.EmployeeId && !x.IsDeleted, ct))
                return new(ContractSaveStatus.NotFound);

            EmploymentContract entity = null;
            if (change.ContractId.HasValue)
            {
                entity = await db.HrEmploymentContracts.SingleOrDefaultAsync(x =>
                    x.Id == change.ContractId.Value && x.EmployeeId == change.EmployeeId && !x.IsDeleted, ct);
                if (entity is null) return new(ContractSaveStatus.NotFound);
                var token = db.Entry(entity).Property<byte[]>("RowVersion");
                if (!token.CurrentValue.SequenceEqual(change.RowVersion)) return new(ContractSaveStatus.Conflict);
                token.OriginalValue = change.RowVersion;
            }

            var now = DateTimeOffset.UtcNow;
            switch (change.Operation)
            {
                case ContractOperation.Create:
                    entity = new EmploymentContract(change.EmployeeId, change.Number, change.Term,
                        change.AgreementDate, change.StartDate, change.EndDate);
                    Add(entity, actorId);
                    break;
                case ContractOperation.Edit:
                    entity.SetTerms(change.Number, change.Term, change.AgreementDate, change.StartDate, change.EndDate);
                    if (!await ValidPredecessorAsync(entity, ct)) return new(ContractSaveStatus.InvalidPredecessor);
                    break;
                case ContractOperation.Renew:
                    if (await db.HrEmploymentContracts.AnyAsync(x => x.EmployeeId == entity.EmployeeId &&
                        x.PreviousContractId == entity.Id && !x.IsDeleted && x.Status != EmploymentContractStatus.Cancelled, ct))
                        return new(ContractSaveStatus.InvalidPredecessor);
                    entity = entity.CreateRenewalDraft(change.Number, change.Term,
                        change.AgreementDate, change.StartDate, change.EndDate);
                    Add(entity, actorId);
                    break;
                case ContractOperation.Approve:
                    if (!await ValidPredecessorAsync(entity, ct)) return new(ContractSaveStatus.InvalidPredecessor);
                    var start = entity.StartDate;
                    var end = entity.EndDate ?? DateOnly.MaxValue;
                    // Termination dates are inclusive. A terminated record still occupies its historical interval.
                    if (await db.HrEmploymentContracts.AnyAsync(x => x.EmployeeId == entity.EmployeeId &&
                        x.Id != entity.Id && !x.IsDeleted &&
                        (x.Status == EmploymentContractStatus.Approved || x.Status == EmploymentContractStatus.Terminated) &&
                        x.StartDate <= end && (x.TerminatedOn ?? x.EndDate ?? DateOnly.MaxValue) >= start, ct))
                        return new(ContractSaveStatus.Overlap);
                    entity.Approve(actorId, now);
                    break;
                case ContractOperation.Cancel:
                    entity.CancelDraft(actorId, change.Reason, now);
                    break;
                case ContractOperation.Terminate:
                    if (!change.TerminationDate.HasValue) return new(ContractSaveStatus.Invalid);
                    // Existing non-cancelled successor needs a reviewed resolution before terminating its predecessor.
                    if (await db.HrEmploymentContracts.AnyAsync(x => x.EmployeeId == entity.EmployeeId &&
                        x.PreviousContractId == entity.Id && !x.IsDeleted && x.Status != EmploymentContractStatus.Cancelled, ct))
                        return new(ContractSaveStatus.InvalidPredecessor);
                    entity.Terminate(change.TerminationDate.Value, actorId, change.Reason, now);
                    break;
                default:
                    return new(ContractSaveStatus.Invalid);
            }

            entity.UserModifyID = actorId;
            entity.ModifyDate = DateTime.Now;
            entity.ModifyDateTxt = CRM.Framework.BaseServices.GetDateTimePersian();
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(ContractSaveStatus.Saved, entity.Id);
        }
        catch (ArgumentException) { return new(ContractSaveStatus.Invalid); }
        catch (InvalidOperationException) { return new(ContractSaveStatus.Invalid); }
        catch (DbUpdateConcurrencyException) { return new(ContractSaveStatus.Conflict); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 or 1205 })
        { return new(ContractSaveStatus.Conflict); }
        catch (SqlException ex) when (ex.Number == 1205)
        { return new(ContractSaveStatus.Conflict); }
        finally
        {
            // This isolated command owns the context's tracked HR changes.
            foreach (var entry in db.ChangeTracker.Entries<EmploymentContract>().ToArray())
                entry.State = EntityState.Detached;
        }
    }

    private void Add(EmploymentContract entity, string actorId)
    {
        entity.UserCreatorID = actorId;
        db.HrEmploymentContracts.Add(entity);
    }

    private async Task<bool> ValidPredecessorAsync(EmploymentContract entity, CancellationToken ct)
    {
        if (!entity.PreviousContractId.HasValue) return true;
        var predecessorId = entity.PreviousContractId.Value;
        var predecessor = await db.HrEmploymentContracts.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == predecessorId && x.EmployeeId == entity.EmployeeId && !x.IsDeleted, ct);
        if (predecessor is null || predecessor.Status != EmploymentContractStatus.Approved ||
            !predecessor.EndDate.HasValue || entity.StartDate <= predecessor.EndDate.Value)
            return false;
        return !await db.HrEmploymentContracts.AnyAsync(x => x.Id != entity.Id &&
            x.EmployeeId == entity.EmployeeId && x.PreviousContractId == predecessorId && !x.IsDeleted &&
            x.Status != EmploymentContractStatus.Cancelled, ct);
    }
}
