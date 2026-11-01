using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.Carizos.Enum;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.ApplicationServices.Carizos.Services;

namespace CRM.Core.ApplicationServices.Carizos.Commands.AddSnooze;
public sealed record AddSnoozeCommand(Guid Id) : ICommand<int>;
public sealed class AddSnoozeCommandHandler(ICarizoRepository repository, ICarizoEffects effects) : ICommandHandler<AddSnoozeCommand, int>
{
    public async Task<int> HandleAsync(AddSnoozeCommand request, CancellationToken ct)
    {
        var id = request.Id;
        try
        {
            var data = await repository.GetAsync(id, true, ct);
            data.RemindDismiss = false;
            data.Remind = true;
            data.DateMiladi = DateTime.Now.AddMinutes(10);
            data.Date = BaseServices.GetDatePersian(data.DateMiladi.Value);
            data.RemindTime = data.DateMiladi.Value.ToString("HH:mm");
            await repository.SaveAsync(ct);
            await effects.RefreshAsync(ct, id);
            return 200;
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return 400;
        }
    }
}
