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

namespace CRM.Core.ApplicationServices.Cashes.Commands.AddCash;
public sealed record AddCashCommand(Cash Model) : ICommand<CashOutcome>;
public sealed class AddCashCommandHandler(ICashRepository repository, CurrentActor actor, IApplicationDates dates, IApplicationAudit audit, ICashEffects effects, CashWorkflow workflow) : ICommandHandler<AddCashCommand, CashOutcome>
{
    public async Task<CashOutcome> HandleAsync(AddCashCommand request, CancellationToken ct)
    {
        var model = request.Model;
        if (model == null)
            return new(400);
        if (model.Amount <= 0)
            return new(400, "مبلغ هزینه برای معتبر نیست.");
        if (model.CashierId is not Guid cashierId)
            return new(400, "تنخواه‌دار معتبر نیست.");
        var cashier = await repository.GetCashierAsync(cashierId, false, ct);
        if (cashier == null)
            return new(404, "تنخواه‌دار یافت نشد.");
        model.CashExternalSoftwares = cashier.CashierExternalSoftwares.Select(x => new CashExternalSoftware { ExternalSoftwareId = x.ExternalSoftwareId, TafsiliCode = x.TafsiliCode, }).ToList();
        await repository.BeginAsync(ct);
        try
        {
            var user = (await actor.GetAsync(ct)).User;
            model.CostDateMiladi = dates.ParsePersian(model.CostDate) ?? DateTime.Now;
            model.StatusTxt = model.Status.ToString().Replace("_", " ");
            if (model.Id == Guid.Empty)
            {
                model.UserCreator = user.FullName;
                model.UserCreatorID = user.Id;
                repository.Add(model);
                await repository.SaveAsync(ct);
                await audit.LogAsync(model, null, LogActionEnum.ثبت, user, ct);
            }
            else
            {
                var oldCash = await repository.GetAsync(model.Id, true, ct);
                if (oldCash == null) return new(404, "رکوردی یافت نشد!");
                model.UserCreatorID = oldCash.UserCreatorID;
                model.UserCreator = oldCash.UserCreator;
                model.CreateDate = oldCash.CreateDate;
                model.CreateDateTxt = oldCash.CreateDateTxt;
                model.Serial = oldCash.Serial;
                model.ModifyDate = DateTime.Now;
                model.ModifyDateTxt = dates.PersianNow();
                model.UserModify = user.FullName;
                model.UserModifyID = user.Id;
                await audit.LogAsync(oldCash, model, LogActionEnum.ویرایش, user, ct, ["CashOrderHeads"]);
                repository.Apply(oldCash, model);
                oldCash.CashOrderHeads = model.CashOrderHeads;
            }

            var attach = effects.SaveAttachments(model.Attachments ?? []);
            foreach (var att in attach ?? [])
            {
                att.EntityId = model.Id;
            }

            repository.SaveAttachments(attach ?? []);
            await workflow.SyncCashAccountancyDebit(model, user, ct);
            await repository.SaveAsync(ct);
            await repository.CommitAsync(ct);
            return new(200);
        }
        catch (OperationCanceledException)when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await repository.RollbackAsync(CancellationToken.None);
            return new(400);
        }
    }
}
