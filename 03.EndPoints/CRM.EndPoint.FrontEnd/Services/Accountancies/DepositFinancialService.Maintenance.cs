using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.Framework;
using static CRM.EndPoint.FrontEnd.Services.SepidarServices.SepidarService;

namespace CRM.EndPoint.FrontEnd.Services.Accountancies;

public sealed record DepositMaintenanceRequest(Guid Id, Guid SoftwareId, Guid MappingId,
    DepositChequeAction Action, bool Edit, string? Token, DepositTransferRequest? Document,
    int PayeeId = 0, int AccountId = 0, string? Description = null);

public sealed partial class DepositFinancialService
{
    public async Task<object> PreviewMaintenanceAsync(DepositMaintenanceRequest request, CancellationToken ct)
    {
        var (source, mapping, destination) = await ResolveDepositMaintenanceAsync(request, ct);
        await EnsureDepositMaintenanceSyncedAsync(request, source, mapping, destination, ct);
        var type = (int)source.PayType;
        var details = await destination.GetDepositMaintenanceAsync(ReadId(mapping), type, request.Action,
            request.Edit, request.SoftwareId, repository, ct);
        object? options = null;
        if (details.Impact.Blockers.Count == 0 && request.Edit)
            options = request.Action == DepositChequeAction.Spend
                ? await destination.GetChequeOperationOptionsAsync(request.Action, ct)
                : await destination.GetDepositOptionsAsync(type, ct);
        return new { details.Impact, details.Input, details.Amount, details.PaymentInput, Type = type,
            DateText = BaseServices.GetDatePersian(details.Impact.Date), DueText = BaseServices.GetDatePersian(details.Due), Options = options };
    }

    public async Task<object> SubmitMaintenanceAsync(DepositMaintenanceRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new SepidarTransferValidationException("تأیید عملیات معتبر نیست.");
        await using var guard = await repository.LockAsync(request.Id, ct);
        var (source, mapping, destination) = await ResolveDepositMaintenanceAsync(request, ct);
        await EnsureDepositMaintenanceSyncedAsync(request, source, mapping, destination, ct);
        DepositInput? input = null;
        DateTime? due = null;
        if (request.Edit && request.Action == DepositChequeAction.None)
        {
            var fields = request.Document ?? throw new SepidarTransferValidationException("اطلاعات ویرایش را وارد کنید.");
            if (fields.Description?.Length > 250 || fields.NoteDescription?.Length > 4000)
                throw new SepidarTransferValidationException("طول شرح سند یا قلم بیش از حد مجاز است.");
            input = new((int)source.PayType, fields.CustomerId, fields.BankAccountId, fields.CashId, fields.BankId,
                Normalize(fields.Reference), Normalize(fields.Sayad), Normalize(fields.AccountNo), fields.Description?.Trim() ?? "",
                Normalize(fields.SecondNumber), fields.ChequeOwner?.Trim(), Normalize(fields.BranchCode), fields.BranchTitle?.Trim(),
                fields.NoteDescription?.Trim(), new(fields.DebitType, fields.DebitAccountId, fields.DebitDetailId),
                new(fields.CreditType, fields.CreditAccountId, fields.CreditDetailId));
            if (source.PayType == PayTypeEnum.چکی)
                due = ParseDate(fields.DueText, null);
        }
        var modifier = await users.GetCurrentExternalUserIdAsync(request.SoftwareId, destination, ct);
        var result = await destination.MaintainDepositAsync(ReadId(mapping), (int)source.PayType, request.Action, request.Edit,
            request.Token, modifier, input, due, request.PayeeId, request.AccountId, request.Description,
            request.SoftwareId, repository, ct);
        if (result.Deleted && request.Action == DepositChequeAction.None)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await repository.RemoveDestinationMappingAsync(mapping, timeout.Token);
        }
        return result;
    }

    private async Task EnsureDepositMaintenanceSyncedAsync(DepositMaintenanceRequest request, Accountancy source,
        ExternalMapping mapping, SepidarService destination, CancellationToken ct)
    {
        if (!request.Edit || request.Action is not (DepositChequeAction.None or DepositChequeAction.Spend))
            return;
        var sync = await destination.GetDepositSyncSummaryAsync(ReadId(mapping),
            GetSyncSource(source, request.SoftwareId, await GetSyncMappingsAsync(source, ct)), ct);
        if (request.Action == DepositChequeAction.None && sync.Changes.Count > 0 ||
            request.Action == DepositChequeAction.Spend && (sync.PaymentChanged || sync.PaymentRestriction != null))
            throw new SepidarTransferValidationException(sync.PaymentRestriction ?? sync.ReceiptRestriction ??
                "ابتدا مغایرت سند را از روی کارت سوابق برطرف کنید.");
    }

    private async Task<(Accountancy Source, ExternalMapping Mapping, SepidarService Destination)> ResolveDepositMaintenanceAsync(
        DepositMaintenanceRequest request, CancellationToken ct)
    {
        if (request.Id == Guid.Empty || request.MappingId == Guid.Empty || !Enum.IsDefined(request.Action))
            throw new SepidarTransferValidationException("ارتباط مالی واریزی معتبر نیست.");
        var source = await SourceAsync(request.Id, ct);
        if (request.Action != DepositChequeAction.None && source.PayType != PayTypeEnum.چکی)
            throw new SepidarTransferValidationException("عملیات چک برای این واریزی معتبر نیست.");
        var destination = Destination(request.SoftwareId);
        var mapping = (await repository.GetMappingsAsync(source.Id, null, null, ct))
            .SingleOrDefault(x => x.Id == request.MappingId && x.ExternalSoftwareId == request.SoftwareId);
        var entity = source.PayType == PayTypeEnum.تهاتری ? "DebitCreditNote" : "ReceiptHeader";
        if (mapping == null || mapping.ExEntityName != entity || mapping.HasPendingExternalId() ||
            await repository.HasOtherDestinationMappingAsync(mapping, ReadId(mapping), ct))
            throw new SepidarTransferValidationException("ارتباط معتبر و یکتای سند مقصد پیدا نشد.");
        return (source, mapping, destination);
    }
}
