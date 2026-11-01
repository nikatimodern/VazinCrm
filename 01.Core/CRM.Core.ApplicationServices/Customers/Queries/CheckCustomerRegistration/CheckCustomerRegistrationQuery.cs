using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Customers.Abstractions;
using CRM.Core.ApplicationServices.Customers.Dtos;
using CRM.Core.ApplicationServices.Customers.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.Customers.Repositories;
using CRM.Core.Domain.Customers.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
namespace CRM.Core.ApplicationServices.Customers.Queries.CheckCustomerRegistration;

public sealed record CheckCustomerRegistrationQuery(Customer Model) : IQuery<bool?>;
public sealed class CheckCustomerRegistrationQueryHandler(ICustomerRepository repository) : IQueryHandler<CheckCustomerRegistrationQuery, bool?>
{
    public async Task<bool?> HandleAsync(CheckCustomerRegistrationQuery request, CancellationToken ct)
    {
        return request.Model.Juridical == 1 ? await repository.HasLegalDuplicateAsync(request.Model, ct) : null;
    }
}
