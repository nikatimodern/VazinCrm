using CRM.Core.Domain.ASettings.Enums;
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

namespace CRM.Core.ApplicationServices.Cashes.Commands.SendCashesToSoftware;
public sealed record SendCashesToSoftwareCommand(IList<Guid> Ids, IList<Guid> SoftwareIds) : ICommand<CashOutcome>;
public sealed class SendCashesToSoftwareCommandHandler(ICashRepository repository, ICashExternalRepository external, CurrentActor actor, ICashEffects effects) : ICommandHandler<SendCashesToSoftwareCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(SendCashesToSoftwareCommand request, CancellationToken ct)
    {
        try
        {
            var current = await actor.GetAsync(ct);
            foreach (var software in request.SoftwareIds ?? [])
            {
                if (software != Guid.Parse("2f3d8024-6c2c-491f-c921-08df26b00138") && software != Guid.Parse("d6c3e8a7-1435-47d5-0489-08df26c5e2ca"))
                    return new(400, "شناسه نرم‌افزار نامعتبر است");
                var cashCode = await external.GetCashAccountAsync(software, ct);
                if (cashCode == null)
                    throw new InvalidOperationException("صندوق سپیدار تعریف نشده است");
                foreach (var id in request.Ids ?? [])
                {
                    if (await external.IsMappedAsync(id, software, ct))
                        continue;
                    var cash = await repository.GetAsync(id, true, ct);
                    if (cash?.CostCenter == null || cash.CostTitle == null || cash.Status != CashStatusEnum.تایید_شده)
                        continue;
                    var code = cash.CashExternalSoftwares.FirstOrDefault(m => m.ExternalSoftwareId == software)?.TafsiliCode;
                    var centerMapping = await external.GetCostCenterMappingAsync(cash.CostCenterId, software, ct);
                    if (!int.TryParse(centerMapping, out var centerId))
                        throw new InvalidOperationException($"مرکز هزینه با عنوان ({cash.CostCenter.Title}) در سپیدار پیدا نشد.");
                    var result = await effects.SendAsync(software, cash, code ?? "", centerId, cashCode, ct);
                    if (result.HasValue)
                    {
                        external.AddMapping(new ExternalMapping { ExternalSoftwareId = software, EntityName = nameof(cash), EntityId = cash.Id, MapType = ExternalMappingTypeEnum.تنخواه, MapTypeTxt = nameof(ExternalMappingTypeEnum.تنخواه), UserCreatorID = current.User.Id, UserCreator = current.User.FullName, ExEntityName = result.Value.EntityName, ExEntityId = result.Value.EntityId });
                        await repository.SaveAsync(ct);
                    }
                }
            }

            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new(400, ex.Message);
        }
    }
}
