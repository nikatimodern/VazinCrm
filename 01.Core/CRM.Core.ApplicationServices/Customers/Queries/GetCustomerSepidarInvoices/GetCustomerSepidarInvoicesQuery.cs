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
namespace CRM.Core.ApplicationServices.Customers.Queries.GetCustomerSepidarInvoices;

public sealed record GetCustomerSepidarInvoicesQuery(int SerialNumber, int PassType) : IQuery<CustomerHtmlResult>;
public sealed class GetCustomerSepidarInvoicesQueryHandler(ICustomerRepository repository, ICustomerExternalRepository external, ICustomerPresentation presentation) : IQueryHandler<GetCustomerSepidarInvoicesQuery, CustomerHtmlResult>
{
    public async Task<CustomerHtmlResult> HandleAsync(GetCustomerSepidarInvoicesQuery request, CancellationToken ct)
    {
        var setting = await repository.GetSettingsAsync(ct);
        if (setting?.Sepidar != true) return new(204);
        var invoices = await external.GetInvoicesAsync(request.SerialNumber, request.PassType == 1, ct);
        return new(200, invoices.Count, await presentation.RenderInvoicesAsync(invoices, request.PassType, ct));
    }
}
