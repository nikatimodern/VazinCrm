using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.QualityControls.Commands.ChangeQualityControlContactHistory;
using CRM.Core.ApplicationServices.QualityControls.Commands.SaveQualityControlItems;
using CRM.Core.ApplicationServices.QualityControls.Commands.SetQualityControlCustomerContact;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlContactHistory;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomerForm;
using CRM.Core.ApplicationServices.QualityControls.Queries.GetQualityControlCustomers;
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
        services.AddScoped<IQueryHandler<GetQualityControlCustomersQuery, QualityControlCustomerListResult>, GetQualityControlCustomersQueryHandler>();
        services.AddScoped<IQueryHandler<GetQualityControlCustomerFormQuery, QualityControlCustomerForm?>, GetQualityControlCustomerFormQueryHandler>();
        services.AddScoped<ICommandHandler<SaveQualityControlItemsCommand, SaveQualityControlItemsResult>, SaveQualityControlItemsCommandHandler>();
        services.AddScoped<ICommandHandler<SetQualityControlCustomerContactCommand, SetQualityControlCustomerContactResult>, SetQualityControlCustomerContactCommandHandler>();
        services.AddScoped<IQueryHandler<GetQualityControlContactHistoryQuery, QualityControlContactHistoryPage?>, GetQualityControlContactHistoryQueryHandler>();
        services.AddScoped<ICommandHandler<ChangeQualityControlContactHistoryCommand, ChangeQualityControlContactHistoryResult>, ChangeQualityControlContactHistoryCommandHandler>();
        return services;
    }
}
