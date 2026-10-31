using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.ApplicationServices.Workspaces.Queries.GetWorkspacePage;
using CRM.Core.Domain.Workspaces.Repositories;
using CRM.Infrastructures.Sql.Workspaces.Repositories;
using CRM.EndPoint.FrontEnd.Services.Application;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class WorkspaceServicesRegistration
{
    public static IServiceCollection AddWorkspaceServices(this IServiceCollection services)
    {
        services.AddScoped<CurrentActor>();
        services.AddScoped<IApplicationAudit, ApplicationAudit>();
        services.AddScoped<IApplicationDates, ApplicationDates>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<IQueryHandler<GetWorkspacePageQuery, WorkspacePageResult>, GetWorkspacePageQueryHandler>();
        return services;
    }
}
