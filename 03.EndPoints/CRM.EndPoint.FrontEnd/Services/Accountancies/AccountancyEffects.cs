using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Accountancies.Abstractions;
using CRM.Core.ApplicationServices.Accountancies.Dtos;
using CRM.Core.Domain;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Models.Accountancy;
using CRM.EndPoint.FrontEnd.Models.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using CRM.EndPoint.FrontEnd.Services.Carizo;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace CRM.EndPoint.FrontEnd.Services.Accountancies;
public sealed class AccountancyEffects(Service service, TaskServices tasks, IHubContext<TaskHub> hub, IPushQueue push, IViewRenderService views) : IAccountancyEffects
{
    public int ParseNumber(string value) => TextService.convertToInt(value ?? "");
    public Task<string> RenderPaymentsAsync(PaymentPartialData x, CancellationToken ct) => views.RenderToStringAsync("GetAllPaymentPartial", new PaymentPartialVModel { Items = x.Items, UserId = x.UserId, IsCreatorParent = x.IsCreatorParent, IsAdmin = x.IsAdmin, IsSalesAdmin = x.IsSalesAdmin, IsManagingDirector = x.IsManagingDirector, IsTamin = x.IsTamin, IsFinancial = x.IsFinancial, IsCustomer = x.IsCustomer, Type = x.Type, PassType = x.PassType });
    public async Task RejectNotificationAsync(Accountancy acc, AppUser user, CancellationToken ct)
    {
        var message = $"<p>{acc.AccTypeTxt} شماره {acc.SerialNum} مربوط به پیش‌فاکتور {acc.pfSerialNumber} (مشتری {acc.CustomerName}) با توضیحات زیر نیاز به اصلاح دارد:</p>";
        message += $"توضیحات: <u>'{acc.CommentText?.Trim().Replace("\n", " ")}'</u>";
        await service.AddEmail($"{acc.AccTypeTxt} نیاز به اصلاح", message, user, acc.UserCreatorID, acc.UserCreator, EntityTypeEnum.Accountancy, acc.Id);
        push.Enqueue(new PushQueueItem { UserId = acc.UserCreatorID, Title = "واریزی نیاز به اصلاح", Body = $"{acc.AccTypeTxt} مربوط به پیش‌فاکتور {acc.pfSerialNumber} نیاز به اصلاح دارد.", Url = "/Email/Index", Tag = "acc" + acc.SerialNum });
    }

    public async Task SetTasksAsync(Accountancy acc, bool approved, CancellationToken ct)
    {
        if (approved)
            await tasks.CreatePreFactorAccountancyTasks(acc);
        else
            await tasks.RemoveAccountancyTasks(acc.Id);
    }

    public Task RefreshTasksAsync(CancellationToken ct) => hub.Clients.All.SendAsync("RefreshTasks", ct);
    public Task NotifyPendingAsync(CancellationToken ct) => hub.Clients.Groups("کارشناس مالی", "مدیر تامین", "کارشناس تامین", "مدیر ارشد").SendAsync("countOfPendingCartable", ct);
}
