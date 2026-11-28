using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Abstractions;
using CRM.Core.Domain.HumanResources.Repositories;
using CRM.Core.Domain.HumanResources.Entities;

namespace CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmployeeProfile;

public sealed record SaveEmployeeProfileCommand(Guid? Id, string FirstName,
    string LastName, string AppUserId, byte[] RowVersion, EmployeePersonalDetails PersonalDetails, byte[] EducationDocument) : ICommand<EmployeeSaveResult>;

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
            if (!rights.ViewEmployeeIds.Contains(command.Id.Value) || !rights.EditEmployeeIds.Contains(command.Id.Value) ||
                (command.EducationDocument != null && !rights.EducationDocumentEditIds.Contains(command.Id.Value)))
                return new(EmployeeSaveStatus.Denied);
            return await repository.EditAsync(new(command.Id.Value, command.FirstName, command.LastName, command.RowVersion, command.PersonalDetails, command.EducationDocument),
                rights.ActorId, rights.EditEmployeeIds, ct);
        }
        if (!rights.CanCreate || (command.EducationDocument != null && !rights.CanUploadEducationOnCreate)) return new(EmployeeSaveStatus.Denied);
        return await repository.CreateAsync(new(command.FirstName, command.LastName, command.AppUserId, command.PersonalDetails, command.EducationDocument),
            rights.ActorId, rights.ImportAccountIds, ct);
    }
}
