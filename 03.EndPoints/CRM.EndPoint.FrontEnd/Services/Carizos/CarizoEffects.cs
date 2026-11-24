using CRM.Core.Domain.AccessCatalog.Repositories;
using CRM.Core.Domain.AccessCatalog.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Carizos.Abstractions;
using CRM.Core.Domain.Carizos.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Models.Carizo;
using CRM.EndPoint.FrontEnd.Models.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace CRM.EndPoint.FrontEnd.Services.Carizos;
public sealed class CarizoEffects(IHubContext<TaskHub> hub, IPushQueue push, IViewRenderService views, IAccessScopeService scopes, IConfiguration configuration) : ICarizoEffects
{
    public Task RefreshAsync(CancellationToken ct, Guid? taskId = null) => hub.Clients.All.SendAsync("RefreshTasks", ct);
    public Task<string> RenderCardsAsync(List<CRM.Core.Domain.Carizos.Entities.Carizo> tasks, AppUser user, int type, int location, CancellationToken ct) => views.RenderToStringAsync("~/Views/Carizo/GetAllTask.cshtml", new CarizoViewModel { CurrentUser = user, Data = tasks, _Type = type, loc = location });
    public async Task NotifyAsync(string userId, string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;
        if (configuration.GetValue<bool>("AccessCatalog:EnforceCarizoPermissions") &&
            await scopes.GetEffectiveScopeAsync(userId, "Carizo.View", ct) is not (AccessScopes.Own or AccessScopes.All or AccessScopes.SelectedUsers or AccessScopes.AllExceptSelectedUsers)) return;
        // A generic notification avoids exposing task text before row-level access is rechecked.
        await hub.Clients.User(userId).SendAsync("NotifTask",
            configuration.GetValue<bool>("AccessCatalog:EnforceCarizoPermissions") ? "فعالیت کاریز شما تغییر کرد." : message, ct);
    }
    public void Push(string userId, string title, string body, string tag)
    {
        var enforced = configuration.GetValue<bool>("AccessCatalog:EnforceCarizoPermissions");
        push.Enqueue(new PushQueueItem { UserId = userId, Title = enforced ? "کاریز" : title,
            Body = enforced ? "فعالیت شما تغییر کرد." : body, Tag = enforced ? "carizo" : tag, Url = "/Carizo/Index" });
    }
    public async Task<object> RenderAsync(List<CRM.Core.Domain.Carizos.Entities.Carizo> tasks, AppUser user, int type, int location, bool reminders, CancellationToken ct) => new
    {
        count = tasks.Count,
        html = reminders ? await views.RenderToStringAsync("GetRemind", tasks) : await RenderCardsAsync(tasks, user, type, location, ct)
    };
}
