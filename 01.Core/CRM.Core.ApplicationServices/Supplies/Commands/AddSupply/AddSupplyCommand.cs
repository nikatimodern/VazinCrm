using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Supplies.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.Core.Domain.Supplies.Repositories;

namespace CRM.Core.ApplicationServices.Supplies.Commands.AddSupply;
public sealed record AddSupplyCommand(List<Supply> Model, AppUser User) : ICommand<SupplyResult>;
public sealed class AddSupplyCommandHandler(ISupplyWriteRepository repository, ISupplyIntegrationServices integrations) : ICommandHandler<AddSupplyCommand, SupplyResult>
{
    public async Task<SupplyResult> HandleAsync(AddSupplyCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        var user = command.User;
        if (model == null)
            return SupplyResult.Invalid();
        try
        {
            var setting = await repository.GetSettingsAsync(cancellationToken);
            foreach (var item in model)
            {
                if (item.Id == Guid.Empty)
                {
                    item.UserCreator = user.FullName;
                    item.UserCreatorID = user.Id;
                    repository.AddSupply(item);
                    await integrations.LogChangeAsync(item, null, LogActionEnum.ثبت, user, cancellationToken);
                }
                else
                {
                    item.UserModify = user.FullName;
                    item.UserModifyID = user.Id;
                    item.ModifyDate = DateTime.Now;
                    item.ModifyDateTxt = integrations.GetDateTimePersian();
                    var oldSupply = await repository.GetSupplyForAuditAsync(item.Id, cancellationToken);
                    await integrations.LogChangeAsync(oldSupply, item, LogActionEnum.ویرایش, user, cancellationToken);
                    repository.UpdateSupply(item);
                }
            }

            if (setting.SetAmountByRoleType != SetAmountRoleEnum.فروش)
            {
                var first = model.FirstOrDefault();
                if (first == null)
                    return SupplyResult.Invalid();
                var pf = await repository.GetPreFactorForUpdateAsync(first.PfId, cancellationToken);
                if (pf == null)
                    return SupplyResult.Invalid();
                if (pf.TaminStep != TaminStepEnum.تایید_شده)
                {
                    pf.TaminStep = TaminStepEnum.در_انتظار_تایید;
                    pf.TaminStepTxt = nameof(TaminStepEnum.در_انتظار_تایید).Replace("_", " ");
                }
            }

            await repository.SaveChangesAsync(cancellationToken);
            return SupplyResult.Success(true);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupplyResult.Invalid();
        }
    }
}
