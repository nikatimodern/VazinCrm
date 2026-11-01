using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Customers.Abstractions;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ASettings.Entities;
using CRM.Core.Domain.Customers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.PreFactors.Entities;
using CRM.Core.Domain.Sepidar.Invoices.Entities;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Models.PreFactors;
using CRM.EndPoint.FrontEnd.Models.PushNotification;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
namespace CRM.EndPoint.FrontEnd.Services.Customers;

public sealed class CustomerEffects(LogSystemService log, IPushQueue push) : ICustomerEffects
{
    public string Normalize(string value) => TextService.NormalizeText(value);
    public async Task AuditAsync(Customer original, Customer current, LogActionEnum action, AppUser user, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); await log.LogChange(original, current, action, user); }
    public void NotifyAccepted(Customer customer, AppUser user)
    {
        var text = string.IsNullOrWhiteSpace(customer.CompanyName) ? $"{customer.CustomerCode} ({customer.AgentName})" : $"{customer.CustomerCode} {customer.CompanyName} ({customer.AgentName})";
        push.Enqueue(new PushQueueItem { UserId = customer.UserCreatorID, Title = "تایید مشتری", Body = $"مشتری {text} توسط {user.FullName} تایید شد.", Url = "/Customer/Index", Tag = "customer" + customer.CustomerCode });
    }
}
public sealed class CustomerPresentation(IViewRenderService views) : ICustomerPresentation
{
    public Task<string> RenderInvoicesAsync(List<Invoice> invoices, int passType, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); return views.RenderToStringAsync("GetSepidarFactors", invoices, new Dictionary<string, object> { { "PassType", passType } }); }
    public Task<string> RenderPreFactorsAsync(List<PreFactor> preFactors, IList<AppUser> supplyUsers, ASetting settings, int passType, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); return views.RenderToStringAsync("GetAllPreFactorPartial", new PrefactorViewModel { data = preFactors, UserTamin = supplyUsers, _Setting = settings }, new Dictionary<string, object> { { "PassType", passType } }); }
}
