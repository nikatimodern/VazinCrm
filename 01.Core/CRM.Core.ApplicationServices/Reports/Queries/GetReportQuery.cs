using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.Domain.Reports.Entities;

namespace CRM.Core.ApplicationServices.Reports.Queries;

public sealed record GetReportQuery(Guid Id) : IQuery<Report?>;
