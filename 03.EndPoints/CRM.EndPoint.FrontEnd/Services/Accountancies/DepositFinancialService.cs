using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.Framework;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.EndPoint.FrontEnd.Services.Accountancies;

public sealed record DepositTransferRequest(Guid Id, Guid SoftwareId, int CustomerId, int BankAccountId,
    int CashId, int BankId, string? DueText, string? Reference, string? Sayad, string? AccountNo,
    string? Description, string? PreviewToken, int CreditAccountId,
    string? SecondNumber, string? ChequeOwner, string? BranchCode, string? BranchTitle,
    string? NoteDescription, int DebitAccountId, SepidarService.DepositPartyType DebitType,
    SepidarService.DepositPartyType CreditType, int DebitDetailId, int CreditDetailId, string? ChequeAmount = null);

public sealed partial class DepositFinancialService(IDepositFinancialRepository repository, Sepidar1Service first,
    Sepidar2Service second, SepidarUserResolver users)
{
    private const ExternalMappingTypeEnum DepositType = ExternalMappingTypeEnum.واریزی;

    public async Task<object> HistoryAsync(Guid id, CancellationToken ct)
    {
        var source = await SourceAsync(id, ct);
        var maps = await repository.GetMappingsAsync(id, null, null, ct);
        var entries = await Task.WhenAll((await repository.GetDestinationsAsync(ct))
            .Where(x => x.Id == Sepidar1SoftwareId || x.Id == Sepidar2SoftwareId).Select(async software => {
                var mapping = maps.SingleOrDefault(x => x.ExternalSoftwareId == software.Id);
                var service = Destination(software.Id);
                var document = mapping == null ? null : await FindAsync(service, mapping, ct);
                var cheque = document != null && source.PayType == PayTypeEnum.چکی
                    ? await GetChequeHistoryAsync(service, source, ReadId(mapping!), ct) : null;
                return new { software.Id, software.Title, Document = document, MappingId = mapping?.Id,
                    RegisteredAt = mapping?.CreateDateTxt, RegisteredBy = mapping?.UserCreator,
                    DateText = BaseServices.GetDatePersian(document?.Date), Missing = mapping != null && document == null && !mapping.HasPendingExternalId(),
                    Pending = mapping?.HasPendingExternalId() == true, Cheque = cheque };
            }));
        return new { PayType = (int)source.PayType, source.SerialNum, source.CustomerName, Entries = entries };
    }

    public async Task<object> PrepareAsync(DepositTransferRequest request, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        var service = Destination(request.SoftwareId);
        var maps = (await repository.GetMappingsAsync(source.Id, source.CustomerId, source.BankAccountId, ct))
            .Where(x => x.ExternalSoftwareId == request.SoftwareId).ToArray();
        int MappedId(ExternalMappingTypeEnum type) => int.TryParse(
            maps.SingleOrDefault(x => x.MapType == type)?.ExEntityId, out var result) ? result : 0;
        var needsOriginalChequeAmount = source.PayType == PayTypeEnum.چکی && source.Vosoled;
        return new { Type = (int)source.PayType, source.CustomerName, source.Beneficiary,
            source.BankAccountTxt, Amount = source.credit,
            NeedsOriginalChequeAmount = needsOriginalChequeAmount,
            DefaultChequeAmount = needsOriginalChequeAmount ? source.credit?.ToString(CultureInfo.InvariantCulture) : null,
            DueText = source.PayType == PayTypeEnum.چکی && !source.Vosoled ? BaseServices.GetDatePersian(source.VosolDateM) : null,
            CustomerId = MappedId(ExternalMappingTypeEnum.مشتری), BankAccountId = MappedId(ExternalMappingTypeEnum.حساب_بانکی),
            Options = await service.GetDepositOptionsAsync((int)source.PayType, ct) };
    }

    public async Task<object> PreviewAsync(DepositTransferRequest request, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        var draft = await PrepareDraftAsync(source, request, validateFiscalYear: false, ct);
        return new { draft.Amount, DateText = BaseServices.GetDatePersian(draft.Date),
            CrmCollectedAmount = source.PayType == PayTypeEnum.چکی && source.Vosoled ? source.credit : null,
            DueText = BaseServices.GetDatePersian(draft.Due),
            draft.CreditDetailTitle, draft.DebitTitle, draft.DebitAccountTitle, draft.CreditTitle, draft.AutoVoucher,
            PreviewToken = GetPreviewToken(draft) };
    }

    public async Task<int> SaveCustomerMappingAsync(DepositTransferRequest request, AppUser actor, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        var customerId = GetMappedCustomerChoice(source, request)
            ?? throw new SepidarTransferValidationException("برای این نوع بستانکار نیازی به تطبیق مشتری نیست.");
        await Destination(request.SoftwareId).GetDepositCustomerAsync(customerId, ct);
        await SaveChoiceAsync(source, source.CustomerId!.Value, request.SoftwareId, ExternalMappingTypeEnum.مشتری,
            "Customer", "Party", customerId, actor, ct);
        return customerId;
    }

    public async Task<int> SaveBankMappingAsync(DepositTransferRequest request, AppUser actor, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        if ((source.PayType != PayTypeEnum.نقدی && !UsesBankCollection(source))
            || source.BankAccountId is not Guid bankId || bankId == Guid.Empty)
            throw new SepidarTransferValidationException("ابتدا حساب بانکی این واریزی را در CRM مشخص کنید.");
        await Destination(request.SoftwareId).ValidateDepositBankAccountAsync(request.BankAccountId, ct);
        await SaveChoiceAsync(source, bankId, request.SoftwareId, ExternalMappingTypeEnum.حساب_بانکی,
            nameof(BankAccount), "BankAccount", request.BankAccountId, actor, ct);
        return request.BankAccountId;
    }

    private async Task SaveChoiceAsync(Accountancy source, Guid id, Guid softwareId, ExternalMappingTypeEnum type,
        string crmEntity, string externalEntity, int value, AppUser actor, CancellationToken ct)
    {
        await using var choiceLock = await repository.LockAsync(id, ct);
        var mappings = await repository.GetMappingsAsync(source.Id, source.CustomerId, source.BankAccountId, ct);
        var mapping = mappings.SingleOrDefault(x => x.MapType == type && x.ExternalSoftwareId == softwareId)
            ?? new ExternalMapping { Id = Guid.NewGuid(), EntityId = id, ExternalSoftwareId = softwareId, MapType = type,
                MapTypeTxt = type.ToString().Replace('_', ' '), EntityName = crmEntity, ExEntityName = externalEntity,
                UserCreatorID = actor.Id, UserCreator = actor.FullName };
        var externalId = value.ToString(CultureInfo.InvariantCulture);
        if (mapping.ExEntityId == externalId) return;
        mapping.ExEntityId = externalId;
        await repository.SaveMappingAsync(mapping, ct);
    }

    public async Task<SepidarService.DepositDocument> SubmitAsync(DepositTransferRequest request, AppUser actor, CancellationToken ct)
    {
        await using var guard = await repository.LockAsync(request.Id, ct);
        var source = await SourceAsync(request.Id, ct);
        var service = Destination(request.SoftwareId);
        var maps = (await repository.GetMappingsAsync(source.Id, source.CustomerId, source.BankAccountId, ct))
            .Where(x => x.ExternalSoftwareId == request.SoftwareId).ToArray();
        var note = source.PayType == PayTypeEnum.تهاتری;
        var mapping = maps.SingleOrDefault(x => x.MapType == DepositType);
        if (mapping != null)
        {
            var existing = await FindAsync(service, mapping, ct);
            if (existing != null)
            {
                if (mapping.HasPendingExternalId()) await CompleteAsync(mapping, ct);
                return existing;
            }
            if (!mapping.HasPendingExternalId())
                throw new SepidarTransferValidationException("سند مقصد پیدا نشد. ابتدا سابقه قبلی را بررسی کنید.");
        }
        bool Matches(ExternalMappingTypeEnum type, int value) => maps.Any(x => x.MapType == type
            && x.ExEntityId == value.ToString(CultureInfo.InvariantCulture));
        if ((GetMappedCustomerChoice(source, request) is int customerId && !Matches(ExternalMappingTypeEnum.مشتری, customerId))
            || (source.PayType == PayTypeEnum.نقدی && !Matches(ExternalMappingTypeEnum.حساب_بانکی, request.BankAccountId)))
            throw new SepidarTransferValidationException("تطبیق مشتری یا حساب بانکی تغییر کرده است. به مرحله تطبیق برگردید.");
        var draft = await PrepareDraftAsync(source, request, validateFiscalYear: true, ct);
        if (GetPreviewToken(draft) != request.PreviewToken)
            throw new SepidarTransferValidationException("اطلاعات یا تنظیمات مقصد تغییر کرده است. پیش‌نمایش را دوباره بررسی کنید.");
        var creator = await users.GetCurrentExternalUserIdAsync(request.SoftwareId, service, ct);
        if (mapping == null)
        {
            var reserved = await service.ReserveDepositIdAsync(note, ct);
            mapping = NewMapping(source.Id, DepositType, nameof(Accountancy), note ? "DebitCreditNote" : "ReceiptHeader",
                ExternalMapping.PendingIdPrefix + reserved.ToString(CultureInfo.InvariantCulture));
            await repository.SaveMappingAsync(mapping, ct);
        }
        var result = await service.AddDepositAsync(ReadId(mapping), creator, draft, ct);
        await CompleteAsync(mapping, ct);
        return result;

        ExternalMapping NewMapping(Guid id, ExternalMappingTypeEnum type, string entity, string external, string value) =>
            new() { Id = Guid.NewGuid(), EntityId = id, ExternalSoftwareId = request.SoftwareId,
                MapType = type, MapTypeTxt = type.ToString().Replace('_', ' '), EntityName = entity,
                ExEntityName = external, ExEntityId = value, UserCreatorID = actor.Id, UserCreator = actor.FullName };
    }

    public async Task DeleteMissingAsync(Guid id, Guid softwareId, CancellationToken ct)
    {
        await using var guard = await repository.LockAsync(id, ct);
        var mapping = (await repository.GetMappingsAsync(id, null, null, ct))
            .SingleOrDefault(x => x.ExternalSoftwareId == softwareId)
            ?? throw new SepidarTransferValidationException("سابقه واریزی پیدا نشد.");
        if (await FindAsync(Destination(softwareId), mapping, ct) != null)
            throw new SepidarTransferValidationException("سند در سپیدار وجود دارد و سابقه قابل حذف نیست.");
        await repository.DeleteMappingAsync(mapping, ct);
    }

    private async Task CompleteAsync(ExternalMapping mapping, CancellationToken ct)
    {
        mapping.ExEntityId = ReadId(mapping).ToString(CultureInfo.InvariantCulture);
        await repository.SaveMappingAsync(mapping, ct);
    }

    private static string GetPreviewToken(SepidarService.DepositDraft draft) =>
        SepidarService.GetPurchasePreviewToken(draft with
        {
            Year = 0,
            Input = draft.Input with { NoteDescription = null }
        });

    private static int? GetMappedCustomerChoice(Accountancy source, DepositTransferRequest request)
    {
        if (source.PayType != PayTypeEnum.تهاتری)
            return request.CustomerId;
        return request.CreditType == SepidarService.DepositPartyType.Customer && request.CreditDetailId > 0
            ? request.CreditDetailId : null;
    }

    private Task<SepidarService.DepositDraft> PrepareDraftAsync(Accountancy source, DepositTransferRequest request,
        bool validateFiscalYear, CancellationToken ct)
    {
        var note = source.PayType == PayTypeEnum.تهاتری;
        if (request.Description?.Length > 250)
            throw new SepidarTransferValidationException(note ? "شرح قلم حداکثر ۲۵۰ کاراکتر است." : "شرح سند حداکثر ۲۵۰ کاراکتر است.");
        if (note && request.NoteDescription?.Length > 4000)
            throw new SepidarTransferValidationException("شرح اعلامیه حداکثر ۴۰۰۰ کاراکتر است.");
        var input = new SepidarService.DepositInput((int)source.PayType, request.CustomerId,
            request.BankAccountId, request.CashId, request.BankId, Normalize(request.Reference), Normalize(request.Sayad),
            Normalize(request.AccountNo), request.Description?.Trim(),
            Normalize(request.SecondNumber), request.ChequeOwner?.Trim(), Normalize(request.BranchCode),
            request.BranchTitle?.Trim(), note ? request.NoteDescription?.Trim() : null,
            new(request.DebitType, request.DebitAccountId, request.DebitDetailId),
            new(request.CreditType, request.CreditAccountId, request.CreditDetailId));
        var date = (SourceDate(source) ?? source.CreateDate).Date;
        DateTime? due = source.PayType == PayTypeEnum.چکی
            ? ParseDate(request.DueText, source.Vosoled ? null : source.VosolDateM) : null;
        var amount = source.credit!.Value;
        if (source.PayType == PayTypeEnum.چکی && source.Vosoled
            && (!decimal.TryParse(Normalize(request.ChequeAmount), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount)
                || amount <= 0 || amount > 999999999999999.9999m || decimal.Round(amount, 4) != amount))
            throw new SepidarTransferValidationException("مبلغ اولیه چک را به ریال وارد کنید.");
        return Destination(request.SoftwareId).PrepareDepositAsync(input, amount, date, due, validateFiscalYear, ct);
    }

    private async Task<Accountancy> SourceAsync(Guid id, CancellationToken ct)
    {
        var source = await repository.GetDepositAsync(id, ct);
        if (source == null || source.AccType != AcEnum.واریزی || source.Status != AccStatusEnum.تایید_شده
            || source.PayType is not (PayTypeEnum.نقدی or PayTypeEnum.چکی or PayTypeEnum.تهاتری))
            throw new SepidarTransferValidationException("فقط واریزی نقدی، چکی یا تهاتری تأییدشده قابل انتقال است.");
        if (source.CurrencyType != CurrencyTypeEnum.ریال || !(source.credit > 0)
            || source.credit > 999999999999999.9999m || decimal.Round(source.credit.Value, 4) != source.credit
            || source.CustomerId == null)
            throw new SepidarTransferValidationException("واریزی باید مبلغ مثبت ریالی و مشتری مشخص داشته باشد.");
        return source;
    }

    private static DateTime? SourceDate(Accountancy source) => source.PayType == PayTypeEnum.چکی
        ? source.ReciveDateM : source.VosolDateM ?? source.date;

    private static string Normalize(string? value) => TextService.ToEnglishNumber(value?.Trim() ?? "");

    private static DateTime ParseDate(string? value, DateTime? fallback, string field = "تاریخ سررسید چک")
    {
        if (value == null && fallback.HasValue) return fallback.Value.Date;
        var text = Normalize(value);
        var parts = text.Split('/');
        if (text.Length == 10 && parts.Length == 3 && int.TryParse(parts[0], out var year)
            && int.TryParse(parts[1], out var month) && int.TryParse(parts[2], out var day))
        {
            try { return new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0); }
            catch (ArgumentOutOfRangeException) { }
        }
        throw new SepidarTransferValidationException($"{field} را به صورت سال/ماه/روز وارد کنید.");
    }

    private static int ReadId(ExternalMapping mapping) => int.TryParse(mapping.ExEntityId.AsSpan(
        mapping.HasPendingExternalId() ? ExternalMapping.PendingIdPrefix.Length : 0), out var id) && id > 0
        ? id : throw new SepidarTransferValidationException("شناسه سند مقصد معتبر نیست.");

    private static Task<SepidarService.DepositDocument?> FindAsync(SepidarService service, ExternalMapping mapping, CancellationToken ct) =>
        service.FindDepositDocumentAsync(mapping.ExEntityName == "DebitCreditNote", ReadId(mapping), ct);

    private SepidarService Destination(Guid id) => id == Sepidar1SoftwareId ? first : id == Sepidar2SoftwareId ? second
        : throw new SepidarTransferValidationException("نرم‌افزار مالی انتخاب‌شده پشتیبانی نمی‌شود.");
}
