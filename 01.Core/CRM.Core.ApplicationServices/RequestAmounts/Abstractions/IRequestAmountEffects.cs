using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.RequestAmounts.Repositories;

namespace CRM.Core.ApplicationServices.RequestAmounts.Abstractions;
public interface IRequestAmountEffects
{
    IList<Attachment> SaveAttachments(IList<Attachment> attachments);
    int ParseNumber(string? value);
    Task NotifyAsync(string[] roles, CancellationToken ct);
    Task<object> RenderAsync(RequestAmountList data, CancellationToken ct);
}
