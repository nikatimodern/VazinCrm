using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Abstractions.Identity;
using CRM.Core.ApplicationServices.Abstractions.Services;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Framework;
using CRM.Core.Domain.Cashes.Entities;
using CRM.Core.Domain.Cashes.Enums;
using CRM.Core.Domain.Cashes.Repositories;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.Attachments.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.Querying;
using CRM.Core.ApplicationServices.Cashes.Abstractions;
using CRM.Core.ApplicationServices.Cashes.Dtos;
using CRM.Core.ApplicationServices.Cashes.Services;

namespace CRM.Core.ApplicationServices.Cashes.Commands.PrepareCashGroupExport;
public sealed record CashExportData(int Status, string? Error, List<Cash> Items);
public sealed record PrepareCashGroupExportCommand(Guid Id, int? StartVoucherNumber) : ICommand<CashExportData>;
public sealed class PrepareCashGroupExportCommandHandler(ICashRepository repository, IApplicationDates dates) : ICommandHandler<PrepareCashGroupExportCommand, CashExportData>
{
    public async Task<CashExportData> HandleAsync(PrepareCashGroupExportCommand request, CancellationToken ct)
    {
        var items = await repository.GetGroupItemsAsync(request.Id, true, ct);
        if (items.Count == 0)
            return new(404, "اقلامی برای این گروه یافت نشد.", items);
        if (request.StartVoucherNumber is int start)
        {
            if (items[0].CashGroup?.IsExported == true)
                return new(400, "این گروه قبلاً خروجی گرفته است.", items);
            var used = await repository.GetUsedVoucherNumbersAsync(start, items.Count, ct);
            if (used.Count > 0)
                return new(400, $"شماره اعلامیه‌های {string.Join("، ", used)} قبلاً ثبت شده‌اند. لطفاً شماره شروع دیگری وارد کنید.", items);
            var voucher = start;
            foreach (var cash in items)
                cash.PaymentVoucherNumber = voucher++;
            var group = items[0].CashGroup;
            if (group != null)
            {
                group.IsExported = true;
                group.ModifyDate = DateTime.Now;
                group.ModifyDateTxt = dates.PersianNow();
            }

            await repository.SaveAsync(ct);
        }

        return new(200, null, items);
    }
}
