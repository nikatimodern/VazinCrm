using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Dtos;
using CRM.Core.ApplicationServices.Reports.Commands.AddReport;
namespace CRM.Core.ApplicationServices.Reports.Commands.DeleteReport;
public sealed record DeleteReportCommand(Guid Id) : ICommand<DeleteReportResult>;
public sealed class DeleteReportCommandHandler(IReportRepository repository, IReportUserContext users, IReportAuditLogger audit, IReportNotifications notifications) : ICommandHandler<DeleteReportCommand, DeleteReportResult>
{
    public async Task<DeleteReportResult> HandleAsync(DeleteReportCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(cancellationToken);
        if (user is null) return DeleteReportResult.NotFound;
        var report = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (report is null) return DeleteReportResult.NotFound;
        if (!user.CanEditOthers && report.UserCreatorID != user.User.Id) return DeleteReportResult.Forbidden;
        await audit.DeleteAsync(report, user.User, cancellationToken);
        report.IsDeleted = true;
        report.UserModify = user.User.FullName;
        report.UserModifyID = user.User.Id;
        await repository.SaveChangesAsync(cancellationToken);
        await notifications.ChangedAsync(user.User.Id, cancellationToken);
        return DeleteReportResult.Deleted;
    }
}

public enum DeleteReportResult { Deleted, Forbidden, NotFound }
