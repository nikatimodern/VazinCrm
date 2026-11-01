using CRM.Core.Domain.Attachments.Entities;

namespace CRM.Core.ApplicationServices.Transfers.Abstractions;
public interface ITransferEffects
{
    int ParseSearchNumber(string? value);
    IList<Attachment> SaveAttachments(IList<Attachment> attachments);
    Task NotifyPendingAsync(CancellationToken cancellationToken);
}
