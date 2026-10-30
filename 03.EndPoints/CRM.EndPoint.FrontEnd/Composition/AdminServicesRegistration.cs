using System.Collections.Generic;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Administration.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Infrastructures.Sql.AppUsers.Repositories;
using Microsoft.Extensions.DependencyInjection;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminPage;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminUsers;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminUser;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminUserRoles;
using CRM.Core.ApplicationServices.Administration.Commands.CreateAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.CreateLegacyAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.DeleteAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.UpdateAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.RemoveAdminUserRole;
using CRM.Core.ApplicationServices.Administration.Commands.UpdateAdminUserRoles;

namespace CRM.EndPoint.FrontEnd.Composition;
public static class AdminServicesRegistration
{
    public static IServiceCollection AddAdminServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IQueryHandler<GetAdminPageQuery, List<AdminRole>>, GetAdminPageQueryHandler>();
        services.AddScoped<IQueryHandler<GetAdminUsersQuery, AdminUserPage>, GetAdminUsersQueryHandler>();
        services.AddScoped<IQueryHandler<GetAdminUserQuery, AppUser?>, GetAdminUserQueryHandler>();
        services.AddScoped<IQueryHandler<GetAdminUserRolesQuery, IList<string>?>, GetAdminUserRolesQueryHandler>();
        services.AddScoped<ICommandHandler<CreateAdminUserCommand, AdminMutationResult>, CreateAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<CreateLegacyAdminUserCommand, List<AccountIdentityError>>, CreateLegacyAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteAdminUserCommand, AdminMutationResult>, DeleteAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAdminUserCommand, AdminUserEditResult>, UpdateAdminUserCommandHandler>();
        services.AddScoped<ICommandHandler<RemoveAdminUserRoleCommand, AdminMutationResult>, RemoveAdminUserRoleCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateAdminUserRolesCommand, AdminMutationResult>, UpdateAdminUserRolesCommandHandler>();
        return services;
    }
}
