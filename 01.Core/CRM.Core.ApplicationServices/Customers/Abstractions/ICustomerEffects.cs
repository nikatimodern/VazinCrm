using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
namespace CRM.Core.ApplicationServices.Customers.Abstractions;

public interface ICustomerEffects
{
    string Normalize(string? value);
    Task AuditAsync(Customer original, Customer? current, LogActionEnum action, AppUser user, CancellationToken cancellationToken);
    void NotifyAccepted(Customer customer, AppUser user);
}
public interface ICustomerPresentation
{
    Task<string> RenderInvoicesAsync(List<Invoice> invoices, int passType, CancellationToken cancellationToken);
    Task<string> RenderPreFactorsAsync(List<PreFactor> preFactors, IList<AppUser> supplyUsers, ASetting? settings, int passType, CancellationToken cancellationToken);
}
