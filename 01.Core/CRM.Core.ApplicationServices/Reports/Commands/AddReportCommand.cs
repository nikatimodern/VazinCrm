using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Reports.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Reports.Entities;

namespace CRM.Core.ApplicationServices.Reports.Commands;

public sealed record AddReportCommand(Report Report, AppUser User, bool CanEditOthers)
    : ICommand<AddReportResult>;
