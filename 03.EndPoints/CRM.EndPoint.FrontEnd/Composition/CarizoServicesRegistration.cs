using CRM.Core.ApplicationServices.Carizos.Queries.GetTaskCards;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.Domain.Carizos.Repositories;
using CRM.Core.Domain.Carizos.Entities;
using CRM.EndPoint.FrontEnd.Services.Carizos;
using CRM.Infrastructures.Sql.Carizos.Repositories;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.Carizos.Commands.UpdateTaskTime;
using CRM.Core.ApplicationServices.Carizos.Commands.AddSnooze;
using CRM.Core.ApplicationServices.Carizos.Commands.TaskToggle;
using CRM.Core.ApplicationServices.Carizos.Commands.DismissTask;
using CRM.Core.ApplicationServices.Carizos.Commands.ShowTask;
using CRM.Core.ApplicationServices.Carizos.Commands.UnCheckTask;
using CRM.Core.ApplicationServices.Carizos.Commands.FinishTask;
using CRM.Core.ApplicationServices.Carizos.Commands.DelTask;
using CRM.Core.ApplicationServices.Carizos.Commands.SaveTask;
using CRM.Core.ApplicationServices.Carizos.Queries.GetTask;
using CRM.Core.ApplicationServices.Carizos.Queries.GetTaskReminders;
using CRM.Core.ApplicationServices.Carizos.Queries.GetTaskList;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class CarizoServicesRegistration
{
    public static IServiceCollection AddCarizoServices(this IServiceCollection services)
    {
        services.AddScoped<ICarizoRepository, CarizoRepository>();
        services.AddScoped<ICarizoEffects, CarizoEffects>();
        services.AddScoped<ICommandHandler<UpdateTaskTimeCommand, int>, UpdateTaskTimeCommandHandler>();
        services.AddScoped<ICommandHandler<AddSnoozeCommand, int>, AddSnoozeCommandHandler>();
        services.AddScoped<ICommandHandler<TaskToggleCommand, int>, TaskToggleCommandHandler>();
        services.AddScoped<ICommandHandler<DismissTaskCommand, int>, DismissTaskCommandHandler>();
        services.AddScoped<ICommandHandler<ShowTaskCommand, int>, ShowTaskCommandHandler>();
        services.AddScoped<ICommandHandler<UnCheckTaskCommand, object>, UnCheckTaskCommandHandler>();
        services.AddScoped<ICommandHandler<FinishTaskCommand, object>, FinishTaskCommandHandler>();
        services.AddScoped<ICommandHandler<DelTaskCommand, int>, DelTaskCommandHandler>();
        services.AddScoped<ICommandHandler<SaveTaskCommand, object>, SaveTaskCommandHandler>();
        services.AddScoped<IQueryHandler<GetTaskQuery, Carizo?>, GetTaskQueryHandler>();
        services.AddScoped<IQueryHandler<GetTaskRemindersQuery, object>, GetTaskRemindersQueryHandler>();
        services.AddScoped<IQueryHandler<GetTaskListQuery, object>, GetTaskListQueryHandler>();
        services.AddScoped<IQueryHandler<GetTaskCardsQuery, TaskCardsResult>, GetTaskCardsQueryHandler>();
        return services;
    }
}
