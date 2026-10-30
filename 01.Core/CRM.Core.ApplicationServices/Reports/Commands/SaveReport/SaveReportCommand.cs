using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Commands.AddReport;
using CRM.Core.Domain.Reports.Entities;

namespace CRM.Core.ApplicationServices.Reports.Commands.SaveReport;

public sealed record SaveReportCommand(Report Report) : ICommand<AddReportResult>;

public sealed class SaveReportCommandHandler(
    ICqrsDispatcher dispatcher,
    IReportUserContext users,
    IReportNotifications notifications) : ICommandHandler<SaveReportCommand, AddReportResult>
{
    public async Task<AddReportResult> HandleAsync(SaveReportCommand request, CancellationToken cancellationToken)
    {
        var context = await users.GetAsync(cancellationToken);
        if (context is null) return AddReportResult.NotFound;
        var result = await dispatcher.CommandAsync(
            new AddReportCommand(request.Report, context.User, context.CanEditOthers), cancellationToken);
        if (result is AddReportResult.Created or AddReportResult.Updated)
            await notifications.ChangedAsync(context.User.Id, cancellationToken);
        return result;
    }
}
