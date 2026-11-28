using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Abstractions;
using CRM.Core.Domain.HumanResources.Repositories;

namespace CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmployeeProfile;

public sealed record SaveEmployeeProfileCommand(Guid? Id, string PersonnelCode, string FirstName,
    string LastName, string AppUserId, byte[] RowVersion) : ICommand<EmployeeSaveResult>;

public sealed class SaveEmployeeProfileCommandHandler(IHrEmployeeAccess access, IEmployeeProfileRepository repository)
    : ICommandHandler<SaveEmployeeProfileCommand, EmployeeSaveResult>
{
    public async Task<EmployeeSaveResult> HandleAsync(SaveEmployeeProfileCommand command, CancellationToken ct)
    {
        var rights = await access.GetAsync(ct);
        if (!rights.CanView || string.IsNullOrWhiteSpace(rights.ActorId))
            return new(EmployeeSaveStatus.Denied);
        if (command.Id.HasValue)
        {
            if (!rights.ViewEmployeeIds.Contains(command.Id.Value) || !rights.EditEmployeeIds.Contains(command.Id.Value))
                return new(EmployeeSaveStatus.Denied);
            return await repository.EditAsync(new(command.Id.Value, command.FirstName, command.LastName, command.RowVersion),
                rights.ActorId, rights.EditEmployeeIds, ct);
        }
        if (!rights.CanCreate) return new(EmployeeSaveStatus.Denied);
        return await repository.CreateAsync(new(command.PersonnelCode, command.FirstName, command.LastName, command.AppUserId),
            rights.ActorId, rights.ImportAccountIds, ct);
    }
}
