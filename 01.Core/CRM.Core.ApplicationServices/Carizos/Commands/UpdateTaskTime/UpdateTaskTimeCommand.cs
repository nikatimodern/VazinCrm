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

namespace CRM.Core.ApplicationServices.Carizos.Commands.UpdateTaskTime;
public sealed record UpdateTaskTimeCommand(Guid Id, string _Time) : ICommand<int>;
public sealed class UpdateTaskTimeCommandHandler(ICarizoRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, ICarizoEffects effects) : ICommandHandler<UpdateTaskTimeCommand, int>
{
    public async Task<int> HandleAsync(UpdateTaskTimeCommand request, CancellationToken ct)
    {
        var id = request.Id;
        var _Time = request._Time;
        try
        {
            var _user = (await actor.GetAsync(ct)).User;
            var oldData = await repository.GetAsync(id, false, ct);
            var data = await repository.GetAsync(id, true, ct);
            if (data.Date == string.Empty)
            {
                data.Date = dates.PersianToday();
                data.DateMiladi = dates.ParsePersian(data.Date);
            }

            data.RemindTime = _Time;
            TimeSpan t = TimeSpan.Parse(data.RemindTime);
            var Gdate = dates.ParsePersian(data.Date);
            data.DateMiladi = Gdate.Value.Date.Add(t);
            data.RemindDismiss = false;
            data.Remind = true;
            await audit.LogAsync(oldData, data, LogActionEnum.ویرایش, _user, ct);
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
