using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.EndPoint.FrontEnd.Services.SepidarServices;
using CRM.Framework;
using static CRM.EndPoint.FrontEnd.Services.SepidarServices.SepidarService;

namespace CRM.EndPoint.FrontEnd.Services.Accountancies;

public sealed record DepositChequeOperationRequest(Guid Id, Guid SoftwareId, DepositChequeAction Action, int ReceiptId,
    string? DateText, int BankAccountId, int PaymentType, int PayeeId, int AccountId, int CashId,
    string? Description, string? PreviewToken);

public sealed partial class DepositFinancialService
{
    private static bool UsesBankCollection(Accountancy source) =>
        source.PayType == PayTypeEnum.چکی && source.Vosoled && ActiveChequeTransfer(source) == null;

    private static AccountancyTransfer? ActiveChequeTransfer(Accountancy source) =>
        source.AccountancyTransfer is { IsDeleted: false } transfer ? transfer : null;

    private static async Task<object> GetChequeHistoryAsync(SepidarService service, Accountancy source, int receiptId, CancellationToken ct)
    {
        var status = await service.FindDepositChequeStatusAsync(receiptId, ct);
        var transferred = ActiveChequeTransfer(source) != null;
        if (status == null)
            return new { NextAction = DepositChequeAction.None, Message = "چک این رسید در مقصد قابل تطبیق نیست." };
        var assignment = status.Assignment;
        var assignmentBank = assignment?.BankId is int bankId
            ? await service.GetChequeAssignmentBankTitleAsync(bankId, ct) : null;
        var collection = status.Collection;
        var collectionBank = collection?.BankId is int collectedBankId && collectedBankId != assignment?.BankId
            ? await service.GetChequeAssignmentBankTitleAsync(collectedBankId, ct) : assignmentBank;
        var payment = status.Payment;
        var next = status.Next(transferred, source.Vosoled);
        return new
        {
            NextAction = next,
            Message = status.Conflicts(transferred, source.Vosoled)
                ? "وضعیت چک در نرم‌افزار مقصد با گردش ثبت‌شده در CRM سازگار نیست." : null,
            Assignment = assignment == null ? null : new
            {
                assignment.Number, DateText = BaseServices.GetDatePersian(assignment.Date), BankAccount = assignmentBank
            },
            Collection = collection == null ? null : new
            {
                collection.Number, DateText = BaseServices.GetDatePersian(collection.Date), BankAccount = collectionBank, status.Amount
            },
            Payment = payment == null ? null : new
            {
                payment.Number, DateText = BaseServices.GetDatePersian(payment.Date),
                Payee = TextService.NormalizePersianText(payment.Payee).Trim(), status.Amount
            }
        };
    }

    public async Task<object> PrepareChequeOperationAsync(DepositChequeOperationRequest request, CancellationToken ct)
    {
        var (source, service, receiptId) = await GetChequeOperationSourceAsync(request, ct);
        var status = await service.FindDepositChequeStatusAsync(receiptId, ct);
        if (status == null || request.Action == DepositChequeAction.None
            || status.Next(ActiveChequeTransfer(source) != null, source.Vosoled) != request.Action)
            throw new SepidarTransferValidationException("این عملیات برای چک انتخاب‌شده قابل ثبت نیست. سوابق را بررسی کنید.");

        var transfer = ActiveChequeTransfer(source);
        var maps = await repository.GetMappingsAsync(source.Id, transfer?.CustomerId, source.BankAccountId, ct);
        int Mapped(ExternalMappingTypeEnum type) => int.TryParse(maps.SingleOrDefault(x =>
            x.MapType == type && x.ExternalSoftwareId == request.SoftwareId)?.ExEntityId, out var localId) ? localId : 0;
        return new
        {
            ReceiptId = receiptId,
            ChequeNumber = status.Number,
            Amount = status.Amount,
            CollectedAmount = source.Vosoled ? source.credit : null,
            DateText = request.Action == DepositChequeAction.Spend ? BaseServices.GetDatePersian(transfer?.CreateDate) : null,
            HasBankMappingSource = source.BankAccountId.HasValue && source.BankAccountId.Value != Guid.Empty,
            source.BankAccountTxt,
            BankId = Mapped(ExternalMappingTypeEnum.حساب_بانکی),
            PayeeId = Mapped(ExternalMappingTypeEnum.تامین_کننده),
            Beneficiary = transfer?.Beneficiary,
            PayeeName = transfer?.ToName,
            status.CashId,
            Options = await service.GetChequeOperationOptionsAsync(request.Action, ct)
        };
    }

    public async Task<object> PreviewChequeOperationAsync(DepositChequeOperationRequest request, CancellationToken ct)
    {
        var (source, service, receiptId) = await GetChequeOperationSourceAsync(request, ct);
        var input = BuildChequeOperationInput(source, request, preview: true);
        var draft = await service.PrepareChequeOperationAsync(receiptId, input, ActiveChequeTransfer(source) != null,
            source.Vosoled, source.credit!.Value, ct);
        return new
        {
            draft.Status.Number,
            draft.Status.Amount,
            CollectedAmount = source.Vosoled ? source.credit : null,
            draft.AutoVoucher,
            TargetTitle = TextService.NormalizePersianText($"{draft.TargetCode} - {draft.TargetTitle}"),
            draft.DebitTitle,
            draft.CreditTitle,
            DateText = BaseServices.GetDatePersian(input.Date),
            PreviewToken = GetChequeOperationPreviewToken(draft)
        };
    }

    public async Task<DepositDocument> SubmitChequeOperationAsync(DepositChequeOperationRequest request, CancellationToken ct)
    {
        await using var guard = await repository.LockAsync(request.Id, ct);
        var (source, service, receiptId) = await GetChequeOperationSourceAsync(request, ct);
        var input = BuildChequeOperationInput(source, request);
        if (input.Action == DepositChequeAction.Assign && source.BankAccountId is Guid bankId && bankId != Guid.Empty)
        {
            var maps = await repository.GetMappingsAsync(source.Id, null, bankId, ct);
            if (!maps.Any(x => x.MapType == ExternalMappingTypeEnum.حساب_بانکی && x.ExternalSoftwareId == request.SoftwareId
                && int.TryParse(x.ExEntityId, out var mappedId) && mappedId == input.BankId))
                throw new SepidarTransferValidationException("تطبیق حساب بانکی تغییر کرده است. به مرحله انتخاب حساب برگردید.");
        }
        var creator = await users.GetCurrentExternalUserIdAsync(request.SoftwareId, service, ct);
        return await service.AddChequeOperationAsync(receiptId, input, ActiveChequeTransfer(source) != null, source.Vosoled,
            source.credit!.Value, request.PreviewToken, creator, ct);
    }

    private async Task<(Accountancy Source, SepidarService Service, int ReceiptId)> GetChequeOperationSourceAsync(
        DepositChequeOperationRequest request, CancellationToken ct)
    {
        var source = await SourceAsync(request.Id, ct);
        if (source.PayType != PayTypeEnum.چکی)
            throw new SepidarTransferValidationException("این عملیات فقط برای واریزی چکی قابل انجام است.");
        var service = Destination(request.SoftwareId);
        var mappings = await repository.GetMappingsAsync(source.Id, null, null, ct);
        var receipt = mappings.SingleOrDefault(x => x.ExternalSoftwareId == request.SoftwareId && x.MapType == DepositType);
        if (receipt == null || receipt.ExEntityName != "ReceiptHeader" || await FindAsync(service, receipt, ct) == null)
            throw new SepidarTransferValidationException("ابتدا رسید دریافت چک را در همین نرم‌افزار مالی ثبت کنید.");
        var receiptId = ReadId(receipt);
        if (request.ReceiptId != 0 && request.ReceiptId != receiptId)
            throw new SepidarTransferValidationException("رسید دریافت مقصد تغییر کرده است. سوابق را دوباره باز کنید.");
        return (source, service, receiptId);
    }

    private static ChequeOperationInput BuildChequeOperationInput(Accountancy source, DepositChequeOperationRequest request, bool preview = false)
    {
        if (request.Description?.Length > 250)
            throw new SepidarTransferValidationException("شرح اعلامیه پرداخت حداکثر ۲۵۰ کاراکتر است.");
        DateTime? date = request.Action switch
        {
            DepositChequeAction.Collect => source.VosolDateM?.Date ??
                throw new SepidarTransferValidationException("تاریخ وصول چک در CRM ثبت نشده است."),
            DepositChequeAction.Assign when preview => null,
            DepositChequeAction.Assign => ParseDate(request.DateText, null, "تاریخ واگذاری به بانک"),
            DepositChequeAction.Spend => ParseDate(request.DateText, null, "تاریخ اعلامیه پرداخت"),
            _ => throw new SepidarTransferValidationException("نوع عملیات چک معتبر نیست.")
        };
        if (request.Action == DepositChequeAction.Assign && source.VosolDateM is { } collectedDate && date > collectedDate.Date)
            throw new SepidarTransferValidationException("تاریخ واگذاری نمی‌تواند بعد از تاریخ وصول ثبت‌شده در CRM باشد.");
        return new(request.Action, date, request.BankAccountId, request.PaymentType, request.PayeeId, request.AccountId,
            request.CashId, request.Description?.Trim() ?? "");
    }
}
