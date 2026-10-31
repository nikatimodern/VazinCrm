using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Transfers.Abstractions;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.SignalR;

namespace CRM.EndPoint.FrontEnd.Services.Transfers;
public sealed class TransferEffects(FileSaver files, IHubContext<TaskHub> hub) : ITransferEffects
{
    public int ParseSearchNumber(string value) => TextService.convertToInt(value);
    public IList<Attachment> SaveAttachments(IList<Attachment> attachments) => files.SaveAttachments(attachments, _AttachType.transfer);
    public Task NotifyPendingAsync(CancellationToken ct) => hub.Clients.Groups("مدیر عامل", "مدیر ارشد").SendAsync("countOfPendingCartable", ct);
}
