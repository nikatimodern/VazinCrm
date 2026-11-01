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
public sealed class CarizoEffects(IHubContext<TaskHub> hub, IPushQueue push, IViewRenderService views) : ICarizoEffects
{
    public Task RefreshAsync(CancellationToken ct, Guid? taskId = null) => taskId.HasValue ? hub.Clients.All.SendAsync("TaskChanged", taskId.Value, ct) : hub.Clients.All.SendAsync("RefreshTasks", ct);
    public Task<string> RenderCardsAsync(List<CRM.Core.Domain.Carizos.Entities.Carizo> tasks, AppUser user, int type, int location, CancellationToken ct) => views.RenderToStringAsync("GetAllTask", new CarizoViewModel { CurrentUser = user, Data = tasks, _Type = type, loc = location });
    public Task NotifyAsync(string userId, string message, CancellationToken ct) => hub.Clients.User(userId).SendAsync("NotifTask", message, ct);
    public void Push(string userId, string title, string body, string tag) => push.Enqueue(new PushQueueItem { UserId = userId, Title = title, Body = body, Tag = tag, Url = "/Carizo/Index" });
    public async Task<object> RenderAsync(List<CRM.Core.Domain.Carizos.Entities.Carizo> tasks, AppUser user, int type, int location, bool reminders, CancellationToken ct) => new
    {
        count = tasks.Count,
        html = reminders ? await views.RenderToStringAsync("GetRemind", tasks) : await RenderCardsAsync(tasks, user, type, location, ct)
    };
}
