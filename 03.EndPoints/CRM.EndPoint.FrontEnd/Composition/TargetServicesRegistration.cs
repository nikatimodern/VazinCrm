using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Targets.Commands.SaveTarget;
using CRM.Core.ApplicationServices.Targets.Queries.GetTarget;
using CRM.Core.ApplicationServices.Targets.Queries.GetTargetSetup;
using CRM.Core.ApplicationServices.Targets.Queries.GetTargets;
using CRM.Core.Domain.Targets.Models;
using CRM.Core.Domain.Targets.Repositories;
using CRM.Infrastructures.Sql.Targets.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Composition;

public static class TargetServicesRegistration
{
    public static IServiceCollection AddTargetServices(this IServiceCollection services)
    {
        services.AddScoped<ITargetRepository, TargetRepository>();
        services.AddScoped<IQueryHandler<GetTargetSetupQuery, TargetSetup>, GetTargetSetupQueryHandler>();
        services.AddScoped<IQueryHandler<GetTargetsQuery, IReadOnlyList<TargetPlanSummary>>, GetTargetsQueryHandler>();
        services.AddScoped<IQueryHandler<GetTargetQuery, TargetPlanDetails?>, GetTargetQueryHandler>();
        services.AddScoped<ICommandHandler<SaveTargetCommand, TargetSaveResult>, SaveTargetCommandHandler>();
        return services;
    }
}
