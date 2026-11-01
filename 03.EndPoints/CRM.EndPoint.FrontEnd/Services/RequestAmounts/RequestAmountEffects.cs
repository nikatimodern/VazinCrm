using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
using CRM.Core.Domain.RequestAmounts.Repositories;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.EndPoint.FrontEnd.Models.RequestAmount;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using CRM.EndPoint.FrontEnd.IServices.ViewRender;
using Microsoft.AspNetCore.SignalR;

namespace CRM.EndPoint.FrontEnd.Services.RequestAmounts;
public sealed class RequestAmountEffects(FileSaver files, IHubContext<TaskHub> hub, IViewRenderService views) : IRequestAmountEffects
{
    public IList<Attachment> SaveAttachments(IList<Attachment> attachments) => files.SaveAttachments(attachments, _AttachType.Payment);
    public int ParseNumber(string value) => TextService.convertToInt(value ?? "");
    public Task NotifyAsync(string[] roles, CancellationToken ct) => hub.Clients.Groups(roles).SendAsync("countOfPendingCartable", ct);
    public async Task<object> RenderAsync(RequestAmountList data, CancellationToken ct) => new
    {
        count = data.Data.Count,
        html = await views.RenderToStringAsync("GetAllRequestAmountsPartial", data.Data.Select(x => new RequestAmountVModel { RequestAmount = x.RequestAmount, SupplyFactorSerials = x.SupplyFactorSerials, OrderHeadSerials = x.OrderHeadSerials, CustomerNames = x.CustomerNames, TotalPaymentAmount = x.TotalPaymentAmount, SupplierDebt = x.SupplierDebt, TotalSupplyFactor = x.TotalSupplyFactor, TotalSupplyPayment = x.TotalSupplyPayment }).ToList()),
        filterCounts = data.FilterCounts
    };
}
