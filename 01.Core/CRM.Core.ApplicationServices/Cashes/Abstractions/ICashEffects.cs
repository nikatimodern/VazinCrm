using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Attachments.Entities;

namespace CRM.Core.ApplicationServices.Cashes.Abstractions;
public interface ICashEffects
{
    int ParseNumber(string? value);
    string Normalize(string value);
    IList<Attachment> SaveAttachments(IList<Attachment> attachments);
    Task SetCashierRoleAsync(Guid userId, bool enabled, CancellationToken ct);
    Task<(string EntityName, string EntityId)?> SendAsync(Guid softwareId, Cash cash, string tafsiliCode, int costCenterId, string cashCode, CancellationToken ct);
}
