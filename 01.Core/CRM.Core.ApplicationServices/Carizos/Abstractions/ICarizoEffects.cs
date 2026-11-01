using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.AppUsers.Entities;

namespace CRM.Core.ApplicationServices.Carizos.Abstractions;
public interface ICarizoEffects
{
    Task RefreshAsync(CancellationToken ct, Guid? taskId = null);
    Task<string> RenderCardsAsync(List<Carizo> tasks, AppUser user, int type, int location, CancellationToken ct);
    Task NotifyAsync(string userId, string message, CancellationToken ct);
    void Push(string userId, string title, string body, string tag);
    Task<object> RenderAsync(List<Carizo> tasks, AppUser user, int type, int location, bool reminders, CancellationToken ct);
}
