using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Supplies.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Supplies.Models;
using CRM.EndPoint.FrontEnd.Services.AI;
using CRM.EndPoint.FrontEnd.Services.LogSystem;

namespace CRM.EndPoint.FrontEnd.Services;
public sealed class SupplyIntegrationServices(DateTimeServices dates, FileSaver files, LogSystemService audit, AiService ai) : ISupplyIntegrationServices
{
    public DateTime? GetDateMiladi(string value) => dates.GetDateMiladi(value);
    public string GetDateTimePersian() => dates.GetDateTimePersian();
    public IList<Attachment> SaveAttachments(IList<Attachment> attachments, _AttachType type, bool removeFile = true) => files.SaveAttachments(attachments, type, null, removeFile);
    public async Task LogChangeAsync<T>(T original, T updated, LogActionEnum action, AppUser user, CancellationToken cancellationToken, List<string> collections = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await audit.LogChange(original, updated, action, user, collections);
    }

    public async Task<SupplyResult> ProcessInvoiceAsync(Attachment attachment, CancellationToken cancellationToken)
    {
        var result = await ai.ProcessSupplyFactor(attachment, cancellationToken);
        return result.Success ? SupplyResult.Success(result.Data) : SupplyResult.Invalid(result.Error);
    }
}
