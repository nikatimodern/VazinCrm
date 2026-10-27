using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Supplies.Models;

namespace CRM.Core.ApplicationServices.Supplies.Dtos;
public interface ISupplyIntegrationServices
{
    DateTime? GetDateMiladi(string value);
    string GetDateTimePersian();
    IList<Attachment> SaveAttachments(IList<Attachment> attachments, _AttachType type, bool removeFile = true);
    Task LogChangeAsync<T>(T original, T updated, LogActionEnum action, AppUser user, CancellationToken cancellationToken, List<string>? collections = null);
    Task<SupplyResult> ProcessInvoiceAsync(Attachment attachment, CancellationToken cancellationToken);
}
