using System.Collections.Generic;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControl;
using CRM.Core.ApplicationServices.QualityControls.Commands.DeleteQualityControl;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlDeals;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlForm;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlQuestions;
using CRM.Core.Domain.QualityControls.Models;
using CRM.Core.Domain.QualityControls.Repositories;
using CRM.Infrastructures.Sql.QualityControls.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Composition;

public static class QualityControlServicesRegistration
{
    public static IServiceCollection AddQualityControlServices(this IServiceCollection services)
    {
        services.AddScoped<IQualityControlRepository, QualityControlRepository>();
        services.AddScoped<IQueryHandler<GetQualityControlDealsQuery, QualityControlListResult>, GetQualityControlDealsQueryHandler>();
        services.AddScoped<IQueryHandler<GetQualityControlFormQuery, QualityControlForm>, GetQualityControlFormQueryHandler>();
        services.AddScoped<IQueryHandler<GetQualityControlQuestionsQuery, IReadOnlyList<QualityControlQuestionDto>>, GetQualityControlQuestionsQueryHandler>();
        services.AddScoped<ICommandHandler<SaveQualityControlCommand, QualityControlSaveResult>, SaveQualityControlCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteQualityControlCommand, QualityControlSaveResult>, DeleteQualityControlCommandHandler>();
        return services;
    }
}
