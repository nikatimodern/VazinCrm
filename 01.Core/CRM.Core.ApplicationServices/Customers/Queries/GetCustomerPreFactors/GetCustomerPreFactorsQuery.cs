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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerPreFactors;

public sealed record GetCustomerPreFactorsQuery(Guid? CustomerId, Guid? DealId, bool ShowDeleted) : IQuery<CustomerHtmlResult>;
public sealed class GetCustomerPreFactorsQueryHandler(ICustomerRepository repository, CustomerAccess access, IAccountUserRepository users, ICustomerExternalRepository external, ICustomerPresentation presentation) : IQueryHandler<GetCustomerPreFactorsQuery, CustomerHtmlResult>
{
    public async Task<CustomerHtmlResult> HandleAsync(GetCustomerPreFactorsQuery request, CancellationToken ct)
    {
        if (!request.CustomerId.HasValue && !request.DealId.HasValue) return new(200);
        var identity = await access.GetAsync(ct);
        var setting = await repository.GetSettingsAsync(ct);
        var data = await repository.GetPreFactorsAsync(request.CustomerId, request.DealId, request.ShowDeleted && !identity.IsCustomer, ct);
        if (setting?.Sepidar == true)
        {
            var counts = await external.GetInvoiceCountsAsync(data, ct);
            if (counts != null) foreach (var item in data) item.CountFactorSepidar = counts.GetValueOrDefault(item.Id);
        }
        var supplyUsers = await users.GetInRoleAsync("کارشناس تامین", ct);
        var passType = request.DealId.HasValue ? 4 : 1;
        return new(200, data.Count, await presentation.RenderPreFactorsAsync(data, supplyUsers, setting, passType, ct));
    }
}
