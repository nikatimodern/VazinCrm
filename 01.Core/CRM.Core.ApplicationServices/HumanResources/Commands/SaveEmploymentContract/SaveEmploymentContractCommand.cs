using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Abstractions;
using CRM.Core.Domain.HumanResources.Repositories;

namespace CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmploymentContract;

public sealed record SaveEmploymentContractCommand(ContractChange Change) : ICommand<ContractSaveResult>;

public sealed class SaveEmploymentContractCommandHandler(IHrEmployeeAccess access,
    IEmploymentContractWriteRepository repository)
    : ICommandHandler<SaveEmploymentContractCommand, ContractSaveResult>
{
    public async Task<ContractSaveResult> HandleAsync(SaveEmploymentContractCommand command, CancellationToken ct)
    {
        var change = command.Change;
        var rights = await access.GetAsync(ct);
        if (change is null || !rights.CanView || string.IsNullOrWhiteSpace(rights.ActorId) ||
            !rights.ViewEmployeeIds.Contains(change.EmployeeId) ||
            !rights.ContractViewEmployeeIds.Contains(change.EmployeeId))
            return new(ContractSaveStatus.Denied);
        var allowed = change.Operation switch
        {
            ContractOperation.Create or ContractOperation.Renew => rights.ContractCreateEmployeeIds,
            ContractOperation.Edit or ContractOperation.Cancel => rights.ContractEditEmployeeIds,
            ContractOperation.Approve => rights.ContractApproveEmployeeIds,
            ContractOperation.Terminate => rights.ContractTerminateEmployeeIds,
            _ => Array.Empty<Guid>()
        };
        if (!allowed.Contains(change.EmployeeId)) return new(ContractSaveStatus.Denied);
        return await repository.SaveAsync(change, rights.ActorId, ct);
    }
}
