using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Abstractions;
using CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmployeeProfile;
using CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmploymentContract;
using CRM.Core.ApplicationServices.HumanResources.Queries.GetEmployeeDirectory;
using CRM.Core.ApplicationServices.HumanResources.Queries.GetEducationDocument;
using CRM.Core.Domain.HumanResources.Repositories;
using CRM.Infrastructures.Sql.HumanResources.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.EndPoint.FrontEnd.Composition;

internal static class HumanResourcesRegistration
{
    public static IServiceCollection AddHumanResources(this IServiceCollection services)
    {
        services.AddScoped<IHrEmployeeAccess, LockedHrEmployeeAccess>();
        services.AddScoped<IEmployeeDirectoryReadRepository, EmployeeDirectoryReadRepository>();
        services.AddScoped<IEmployeeProfileRepository, EmployeeProfileRepository>();
        services.AddScoped<IEmploymentContractReadRepository, EmploymentContractReadRepository>();
        services.AddScoped<IQueryHandler<GetEmployeeDirectoryQuery, EmployeeDirectoryPage>, GetEmployeeDirectoryQueryHandler>();
        services.AddScoped<ICommandHandler<SaveEmployeeProfileCommand, EmployeeSaveResult>, SaveEmployeeProfileCommandHandler>();
        services.AddScoped<IEmploymentContractWriteRepository, EmploymentContractWriteRepository>();
        services.AddScoped<ICommandHandler<SaveEmploymentContractCommand, ContractSaveResult>, SaveEmploymentContractCommandHandler>();
        services.AddScoped<IQueryHandler<GetEducationDocumentQuery, EmployeeEducationDocument>, GetEducationDocumentQueryHandler>();
        return services;
    }
}
