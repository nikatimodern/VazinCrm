using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Cashes.Abstractions;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using Microsoft.AspNetCore.Identity;

namespace CRM.EndPoint.FrontEnd.Services.Cashes;
public sealed class CashEffects(FileSaver files, UserManager<AppUser> users, Sepidar1Service first, Sepidar2Service second) : ICashEffects
{
    public int ParseNumber(string value) => TextService.convertToInt(value ?? "");
    public string Normalize(string value) => TextService.NormalizePersianText(value);
    public IList<Attachment> SaveAttachments(IList<Attachment> attachments) => files.SaveAttachments(attachments, _AttachType.Cash);
    public async Task SetCashierRoleAsync(Guid userId, bool enabled, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw new InvalidOperationException("User not found.");
        var result = enabled ? await users.AddToRoleAsync(user, "تنخواه دار") : await users.RemoveFromRoleAsync(user, "تنخواه دار");
        if (!result.Succeeded)
            throw new InvalidOperationException("Unable to update cashier role.");
    }

    public Task<(string EntityName, string EntityId)?> SendAsync(Guid id, Cash cash, string tafsiliCode, int centerId, string cashCode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        SepidarService service = id == Guid.Parse("2f3d8024-6c2c-491f-c921-08df26b00138") ? first : id == Guid.Parse("d6c3e8a7-1435-47d5-0489-08df26c5e2ca") ? second : throw new InvalidOperationException("Invalid external software.");
        return service.SendCashToSepidar(cash, tafsiliCode, centerId, cashCode);
    }
}
