using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Checks.Entities;
using CRM.Core.Domain.Checks.Enums;
using CRM.Core.Domain.Checks.Repositories;

namespace CRM.Core.ApplicationServices.Checks.Commands.CreateCheckbook;
public sealed record CreateCheckbookCommand(Checkbook Model) : ICommand<int>;
public sealed class CreateCheckbookCommandHandler(ICheckRepository repository, CurrentActor actor, IApplicationAudit audit) : ICommandHandler<CreateCheckbookCommand, int>
{
    public async Task<int> HandleAsync(CreateCheckbookCommand request, CancellationToken ct)
    {
        var user = (await actor.GetAsync(ct)).User;
        var model = request.Model;
        model.UserCreator = user.FullName;
        model.UserCreatorID = user.Id;
        await audit.LogAsync(model, null, LogActionEnum.ثبت, user, ct);
        repository.AddCheckbook(model);
        await repository.SaveChangesAsync(ct);
        return 200;
    }
}
