using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.ApplicationServices.Accountancies.Dtos;

namespace CRM.Core.ApplicationServices.Accountancies.Abstractions;
public interface IAccountancyEffects
{
    int ParseNumber(string? value);
    Task<string> RenderPaymentsAsync(PaymentPartialData model, CancellationToken ct);
    Task RejectNotificationAsync(Accountancy acc, AppUser user, CancellationToken ct);
    Task SetTasksAsync(Accountancy acc, bool approved, CancellationToken ct);
    Task RefreshTasksAsync(CancellationToken ct);
    Task NotifyPendingAsync(CancellationToken ct);
}
