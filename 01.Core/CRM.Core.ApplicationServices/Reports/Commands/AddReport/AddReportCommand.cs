using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Reports.Entities;

namespace CRM.Core.ApplicationServices.Reports.Commands.AddReport;

public sealed record AddReportCommand(Report Report, AppUser User, bool CanEditOthers)
    : ICommand<AddReportResult>;
