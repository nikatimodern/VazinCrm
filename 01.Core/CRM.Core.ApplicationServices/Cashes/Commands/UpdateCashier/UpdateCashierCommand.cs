using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cashes.Abstractions;
using CRM.Core.ApplicationServices.Cashes.Dtos;
using CRM.Core.ApplicationServices.Cashes.Services;

namespace CRM.Core.ApplicationServices.Cashes.Commands.UpdateCashier;
public sealed record UpdateCashierCommand(Cashier Model) : ICommand<CashOutcome>;
public sealed class UpdateCashierCommandHandler(ICashRepository repository) : ICommandHandler<UpdateCashierCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(UpdateCashierCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null || model.Id == Guid.Empty)
            return new(400, "اطلاعات معتبر نیست.");
        var cashier = await repository.GetCashierAsync(model.Id, true, ct);
        if (cashier == null)
            return new(404);
        cashier.Title = model.Title?.Trim();
        cashier.UserId = model.UserId;
        cashier.UserTxt = model.UserTxt;
        cashier.CashierExternalSoftwares = model.CashierExternalSoftwares;
        await repository.SaveAsync(ct);
        return new(200);
    }
}
