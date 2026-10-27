using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Reports.Abstractions;
using CRM.Core.ApplicationServices.Reports.Commands;
using CRM.Core.ApplicationServices.Reports.Dtos;
using CRM.Core.Domain.Deals.Repositories;
using CRM.Infrastructures.Sql.Deals.Repositories;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.Core.ApplicationServices.Reports.Handlers;
using CRM.Core.ApplicationServices.Reports.Queries;
using CRM.Core.Domain.Reports.Entities;
using CRM.Core.Domain.Reports.Repositories;
using CRM.Infrastructures.Sql.Reports.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Composition;

public static class ApplicationServicesRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetReportQuery, Report>, GetReportQueryHandler>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<ICommandHandler<AddReportCommand, AddReportResult>, AddReportCommandHandler>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IReportAuditLogger, ReportAuditLogger>();
        return services;
    }
}
