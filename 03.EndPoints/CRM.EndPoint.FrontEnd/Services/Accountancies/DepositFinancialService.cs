using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.ASettings.Enums;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.Framework;
using static CRM.Core.Domain.ExternalSoftwares.ExternalSoftwareIds;

namespace CRM.EndPoint.FrontEnd.Services.Accountancies;

public sealed record DepositTransferRequest(Guid Id, Guid SoftwareId, int CustomerId, int VendorId, int BankAccountId,
    int CashId, int BankId, string? DateText, string? DueText, string? Reference, string? Sayad, string? AccountNo,
    string? Description, string? PreviewToken, string? CustomerAccount,
    string? SecondNumber, string? ChequeOwner, string? BranchCode, string? BranchTitle);

public sealed class DepositFinancialService(IDepositFinancialRepository repository, Sepidar1Service first,
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
                var document = mapping == null ? null : await FindAsync(Destination(software.Id), mapping, ct);
                return new { software.Id, software.Title, Document = document,
                    DateText = BaseServices.GetDatePersian(document?.Date), Missing = mapping != null && document == null && !mapping.HasPendingExternalId(),
                    Pending = mapping?.HasPendingExternalId() == true };
            }));
        return new { PayType = (int)source.PayType, source.CustomerName, Entries = entries };
    }

    public async Task<object> PrepareAsync(DepositTransferRequest request, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        var service = Destination(request.SoftwareId);
        var maps = (await repository.GetMappingsAsync(source.Id, source.CustomerId, source.BankAccountId, ct))
            .Where(x => x.ExternalSoftwareId == request.SoftwareId).ToArray();
        int MappedId(ExternalMappingTypeEnum type) => int.TryParse(
            maps.SingleOrDefault(x => x.MapType == type)?.ExEntityId, out var result) ? result : 0;
        return new { Type = (int)source.PayType, source.CustomerName, source.Beneficiary, source.ToName,
            source.BankAccountTxt, Amount = source.credit, DateText = BaseServices.GetDatePersian(SourceDate(source)),
            DueText = BaseServices.GetDatePersian(source.VosolDateM),
            CustomerId = MappedId(ExternalMappingTypeEnum.مشتری), BankAccountId = MappedId(ExternalMappingTypeEnum.حساب_بانکی),
            Options = await service.GetDepositOptionsAsync((int)source.PayType, ct) };
    }

    public async Task<object> PreviewAsync(DepositTransferRequest request, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        var draft = await PrepareDraftAsync(source, request, validateFiscalYear: false, ct);
        return new { draft.Amount, DateText = BaseServices.GetDatePersian(draft.Date),
            DueText = BaseServices.GetDatePersian(draft.Due),
            draft.Customer, draft.DebitTitle, draft.CreditTitle, draft.AutoVoucher,
            PreviewToken = GetPreviewToken(draft) };
    }

    public async Task<int> SaveCustomerMappingAsync(DepositTransferRequest request, string actorId, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        await Destination(request.SoftwareId).GetDepositCustomerAsync(request.CustomerId, ct);
        await SaveChoiceAsync(source, source.CustomerId!.Value, request.SoftwareId, ExternalMappingTypeEnum.مشتری,
            "Customer", "Party", request.CustomerId, actorId, ct);
        return request.CustomerId;
    }

    public async Task<int> SaveBankMappingAsync(DepositTransferRequest request, string actorId, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        if (source.PayType != PayTypeEnum.نقدی || source.BankAccountId is not Guid bankId || bankId == Guid.Empty)
            throw new InvalidOperationException("ابتدا حساب بانکی این واریزی را در CRM مشخص کنید.");
        await Destination(request.SoftwareId).ValidateDepositBankAccountAsync(request.BankAccountId, ct);
        await SaveChoiceAsync(source, bankId, request.SoftwareId, ExternalMappingTypeEnum.حساب_بانکی,
            nameof(BankAccount), "BankAccount", request.BankAccountId, actorId, ct);
        return request.BankAccountId;
    }

    private async Task SaveChoiceAsync(Accountancy source, Guid id, Guid softwareId, ExternalMappingTypeEnum type,
        string crmEntity, string externalEntity, int value, string actorId, CancellationToken ct)
    {
        await using var choiceLock = await repository.LockAsync(id, ct);
        var mappings = await repository.GetMappingsAsync(source.Id, source.CustomerId, source.BankAccountId, ct);
        var mapping = mappings.SingleOrDefault(x => x.MapType == type && x.ExternalSoftwareId == softwareId)
            ?? new ExternalMapping { Id = Guid.NewGuid(), EntityId = id, ExternalSoftwareId = softwareId, MapType = type,
                MapTypeTxt = type.ToString().Replace('_', ' '), EntityName = crmEntity, ExEntityName = externalEntity,
                UserCreatorID = actorId };
        var externalId = value.ToString(CultureInfo.InvariantCulture);
        if (mapping.ExEntityId == externalId) return;
        mapping.ExEntityId = externalId;
        await repository.SaveMappingAsync(mapping, ct);
    }

    public async Task<SepidarService.DepositDocument> SubmitAsync(DepositTransferRequest request, string actorId, CancellationToken ct)
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
                throw new InvalidOperationException("سند مقصد پیدا نشد. ابتدا سابقه قبلی را بررسی کنید.");
        }
        bool Matches(ExternalMappingTypeEnum type, int value) => maps.Any(x => x.MapType == type
            && x.ExEntityId == value.ToString(CultureInfo.InvariantCulture));
        if (!Matches(ExternalMappingTypeEnum.مشتری, request.CustomerId)
            || (source.PayType == PayTypeEnum.نقدی && !Matches(ExternalMappingTypeEnum.حساب_بانکی, request.BankAccountId)))
            throw new InvalidOperationException("تطبیق مشتری یا حساب بانکی تغییر کرده است. به مرحله تطبیق برگردید.");
        var draft = await PrepareDraftAsync(source, request, validateFiscalYear: true, ct);
        if (GetPreviewToken(draft) != request.PreviewToken)
            throw new InvalidOperationException("اطلاعات یا تنظیمات مقصد تغییر کرده است. پیش‌نمایش را دوباره بررسی کنید.");
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
                ExEntityName = external, ExEntityId = value, UserCreatorID = actorId };
    }

    public async Task DeleteMissingAsync(Guid id, Guid softwareId, CancellationToken ct)
    {
        await using var guard = await repository.LockAsync(id, ct);
        var mapping = (await repository.GetMappingsAsync(id, null, null, ct))
            .SingleOrDefault(x => x.ExternalSoftwareId == softwareId)
            ?? throw new InvalidOperationException("سابقه واریزی پیدا نشد.");
        if (await FindAsync(Destination(softwareId), mapping, ct) != null)
            throw new InvalidOperationException("سند در سپیدار وجود دارد و سابقه قابل حذف نیست.");
        await repository.DeleteMappingAsync(mapping, ct);
    }

    private async Task CompleteAsync(ExternalMapping mapping, CancellationToken ct)
    {
        mapping.ExEntityId = ReadId(mapping).ToString(CultureInfo.InvariantCulture);
        await repository.SaveMappingAsync(mapping, ct);
    }

    private static string GetPreviewToken(SepidarService.DepositDraft draft) =>
        SepidarService.GetPurchasePreviewToken(draft with { Date = default, Year = 0 });

    private Task<SepidarService.DepositDraft> PrepareDraftAsync(Accountancy source, DepositTransferRequest request,
        bool validateFiscalYear, CancellationToken ct)
    {
        if (source.PayType == PayTypeEnum.چکی && (source.Vosoled || source.AccountancyTransfer != null))
            throw new InvalidOperationException("در این گردش فقط دریافت اولیه چک ثبت می‌شود.");
        if (request.Description?.Length > 250)
            throw new InvalidOperationException("شرح سند حداکثر ۲۵۰ کاراکتر است.");
        var input = new SepidarService.DepositInput((int)source.PayType, request.CustomerId, request.VendorId,
            request.BankAccountId, request.CashId, request.BankId, Normalize(request.Reference), Normalize(request.Sayad),
            Normalize(request.AccountNo), request.Description?.Trim(), request.CustomerAccount,
            Normalize(request.SecondNumber), request.ChequeOwner?.Trim(), Normalize(request.BranchCode),
            request.BranchTitle?.Trim());
        var defaultDate = SourceDate(source) ?? source.CreateDate;
        var date = validateFiscalYear ? ParseDate(request.DateText, defaultDate) : defaultDate.Date;
        DateTime? due = source.PayType == PayTypeEnum.چکی
            ? ParseDate(request.DueText, source.VosolDateM) : null;
        return Destination(request.SoftwareId).PrepareDepositAsync(input, source.credit!.Value, date, due, validateFiscalYear, ct);
    }

    private async Task<Accountancy> SourceAsync(Guid id, CancellationToken ct)
    {
        var source = await repository.GetDepositAsync(id, ct);
        if (source == null || source.AccType != AcEnum.واریزی || source.Status != AccStatusEnum.تایید_شده
            || source.PayType is not (PayTypeEnum.نقدی or PayTypeEnum.چکی or PayTypeEnum.تهاتری))
            throw new InvalidOperationException("فقط واریزی نقدی، چکی یا تهاتری تأییدشده قابل انتقال است.");
        if (source.CurrencyType != CurrencyTypeEnum.ریال || !(source.credit > 0)
            || source.credit > 999999999999999.9999m || decimal.Round(source.credit.Value, 4) != source.credit
            || source.CustomerId == null)
            throw new InvalidOperationException("واریزی باید مبلغ مثبت ریالی و مشتری مشخص داشته باشد.");
        return source;
    }

    private static DateTime? SourceDate(Accountancy source) => source.PayType == PayTypeEnum.چکی
        ? source.ReciveDateM : source.VosolDateM ?? source.date;

    private static string Normalize(string? value) => TextService.ToEnglishNumber(value?.Trim() ?? "");

    private static DateTime ParseDate(string? value, DateTime? fallback)
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
        throw new InvalidOperationException("تاریخ پرداخت یا سررسید را به صورت سال/ماه/روز وارد کنید.");
    }

    private static int ReadId(ExternalMapping mapping) => int.TryParse(mapping.ExEntityId.AsSpan(
        mapping.HasPendingExternalId() ? ExternalMapping.PendingIdPrefix.Length : 0), out var id) && id > 0
        ? id : throw new InvalidOperationException("شناسه سند مقصد معتبر نیست.");

    private static Task<SepidarService.DepositDocument?> FindAsync(SepidarService service, ExternalMapping mapping, CancellationToken ct) =>
        service.FindDepositDocumentAsync(mapping.ExEntityName == "DebitCreditNote", ReadId(mapping), ct);

    private SepidarService Destination(Guid id) => id == Sepidar1SoftwareId ? first : id == Sepidar2SoftwareId ? second
        : throw new InvalidOperationException("نرم‌افزار مالی انتخاب‌شده پشتیبانی نمی‌شود.");
}
