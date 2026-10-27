using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Deals.Repositories;
using CRM.Core.Domain.Reports.Enum;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Framework;

namespace CRM.Core.ApplicationServices.Reports.Commands.AddReport;

public sealed class AddReportCommandHandler(
    IReportRepository reports,
    IDealRepository deals,
    IReportAuditLogger auditLogger) : ICommandHandler<AddReportCommand, AddReportResult>
{
    public async Task<AddReportResult> HandleAsync(AddReportCommand command, CancellationToken cancellationToken)
    {
        var model = command.Report;
        var isNew = model.Id == Guid.Empty;
        var original = isNew ? null : await reports.GetForUpdateAsync(model.Id, cancellationToken);

        if (!isNew)
        {
            if (original is null) return AddReportResult.NotFound;
            if (!command.CanEditOthers && original.UserCreatorID != command.User.Id)
                return AddReportResult.Forbidden;
        }

        model.ReportTypeTxt = Enum.GetValues<ReportTypeEnum>()
            .FirstOrDefault(type => type == model.ReportType).ToString().Replace("_", " ");

        if (isNew)
        {
            model.UserCreator = command.User.FullName;
            model.UserCreatorID = command.User.Id;
            model.RegDate = DateTime.Now;
            model.RegDateTxt = BaseServices.GetDatePersian();
            model.RegTimeTxt = model.RegDate.ToString("HH:mm:ss");
            reports.Add(model);
            await auditLogger.LogAsync(model, null, command.User, cancellationToken);
        }
        else
        {
            model.UserModify = command.User.FullName;
            model.UserModifyID = command.User.Id;
            model.CreateDate = original!.CreateDate;
            model.CreateDateTxt = original.CreateDateTxt;
            model.UserCreator = original.UserCreator;
            model.UserCreatorID = original.UserCreatorID;
            await auditLogger.LogAsync(original, model, command.User, cancellationToken);
            reports.Update(original, model);
        }

        if (model.DealId is Guid dealId)
        {
            var deal = await deals.GetForUpdateAsync(dealId, cancellationToken);
            if (deal is not null)
            {
                deal.ModifyDate = DateTime.Now;
                deal.ModifyDateTxt = BaseServices.GetDateTimePersian();
            }
        }

        await reports.SaveChangesAsync(cancellationToken);
        return isNew ? AddReportResult.Created : AddReportResult.Updated;
    }
}
