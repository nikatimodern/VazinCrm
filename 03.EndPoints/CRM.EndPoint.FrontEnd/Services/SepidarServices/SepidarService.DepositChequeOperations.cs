using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.Framework;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public partial class SepidarService
{
    public enum DepositChequeAction { None, Assign, Collect, Spend }

    private const int ChequeInCash = 1;
    private const int ChequeAssigned = 2;
    private const int ChequeCollected = 4;
    private const int ChequeSpent = 32;
    private const int AssignmentType = 1;
    private const int CollectionType = 2;
    private const int PaySupplier = 256;
    private const int PayOther = 2;
    private const int PayCustomer = 1;
    private const int PayBroker = 128;
    private const int PayPettyCash = 2048;
    private const string BankingIssuer = "SG.ReceiptAndPayment.ReceiptManagement.Common.dsReceiptChequeBanking, SG.ReceiptAndPayment.ReceiptManagement.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
    private const string PaymentIssuer = "SG.ReceiptAndPayment.PaymentManagement.Common.DsPayment, SG.ReceiptAndPayment.PaymentManagement.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
    private const string BankingNumberedEntity = "SG.ReceiptAndPayment.ReceiptManagement.Common.ReceiptChequeBankingRow";
    private const string PaymentNumberedEntity = "SG.ReceiptAndPayment.PaymentManagement.Common.PaymentHeaderRow";

    public sealed record ChequeBankingDocument(int ItemId, int Number, DateTime Date, int? BankId, DateTime? CreationDate);
    public sealed record ChequePaymentDocument(int Number, DateTime Date, string? Payee, DateTime? CreationDate);
    public sealed record DepositChequeStatus(int Id, int State, decimal Amount, string Number, DateTime ReceivedDate,
        DateTime? DueDate, int? CashId, ChequeBankingDocument? Assignment, ChequeBankingDocument? Collection,
        ChequePaymentDocument? Payment)
    {
        public DepositChequeAction Next(bool transferred, bool collected) => (transferred, collected, State) switch
        {
            (true, _, ChequeInCash) when Payment == null => DepositChequeAction.Spend,
            (false, true, ChequeInCash) when Payment == null => DepositChequeAction.Assign,
            (false, true, ChequeAssigned) when Assignment?.BankId != null && Payment == null => DepositChequeAction.Collect,
            _ => DepositChequeAction.None
        };

        public bool Conflicts(bool transferred, bool collected) =>
            transferred ? State is not (ChequeInCash or ChequeSpent) || (State == ChequeSpent) != (Payment != null)
            : collected && (State is not (ChequeInCash or ChequeAssigned or ChequeCollected)
                || (State == ChequeAssigned && Assignment?.BankId == null)
                || (State == ChequeCollected && Collection == null));
    }

    public sealed record ChequeOperationInput(DepositChequeAction Action, DateTime? Date, int BankId, int PaymentType,
        int PayeeId, int AccountId, int CashId, string Description);
    public sealed record ChequeOperationDraft(ChequeOperationInput Input, DepositChequeStatus Status, int Version, int HistoryId,
        int Year, int DebitAccount, int CreditAccount, int DebitDl, int CreditDl, int EffectiveBankId,
        string TargetTitle, string TargetCode, string? DebitTitle, string? CreditTitle, bool AutoVoucher);

    public async Task<DepositChequeStatus?> FindDepositChequeStatusAsync(int receiptId, CancellationToken ct)
    {
        var cheque = await sepidarContext.ReceiptCheques.AsNoTracking()
            .Where(x => x.ReceiptHeaderRef == receiptId).Take(2).ToListAsync(ct);
        return cheque.Count == 1 ? await ReadChequeStatusAsync(cheque[0], ct) : null;
    }

    private async Task<DepositChequeStatus> ReadChequeStatusAsync(ReceiptCheque cheque, CancellationToken ct)
    {
        var banking = await (from item in sepidarContext.ReceiptChequeBankingItems.AsNoTracking()
            join header in sepidarContext.ReceiptChequeBankings.AsNoTracking()
                on item.ReceiptChequeBankingRef equals header.ReceiptChequeBankingId
            where item.ReceiptChequeRef == cheque.ReceiptChequeId &&
                ((header.Type == AssignmentType && item.State == ChequeAssigned) ||
                 (header.Type == CollectionType && item.State == ChequeCollected))
            orderby header.Date descending, item.ReceiptChequeBankingItemId descending
            select new { header.Type, item.ReceiptChequeBankingItemId, header.Number, header.Date, header.CreationDate,
                BankId = item.BankAccountRef ?? header.BankAccountRef, item.ReceiptChequeBankingItemRef }).ToListAsync(ct);
        var collection = banking.FirstOrDefault(x => x.Type == CollectionType);
        var assignment = collection != null && cheque.State == ChequeCollected
            ? banking.FirstOrDefault(x => x.ReceiptChequeBankingItemId == collection.ReceiptChequeBankingItemRef)
            : banking.FirstOrDefault(x => x.Type == AssignmentType);
        var payment = await (from item in sepidarContext.PaymentChequeOthers.AsNoTracking()
            join header in sepidarContext.PaymentHeaders.AsNoTracking() on item.PaymentHeaderRef equals header.PaymentHeaderId
            join detail in sepidarContext.DLs.AsNoTracking() on header.DlRef equals detail.DLId into details
            from detail in details.DefaultIfEmpty()
            where item.ReceiptChequeRef == cheque.ReceiptChequeId
            orderby header.Date descending, header.PaymentHeaderId descending
            select new ChequePaymentDocument(header.Number, header.Date, detail == null ? null : detail.Title,
                header.CreationDate)).FirstOrDefaultAsync(ct);
        return new(cheque.ReceiptChequeId, cheque.State, cheque.Amount, cheque.Number, cheque.HeaderDate,
            cheque.Date, cheque.CashRef,
            assignment == null ? null : new(assignment.ReceiptChequeBankingItemId, assignment.Number, assignment.Date,
                assignment.BankId, assignment.CreationDate),
            collection == null ? null : new(collection.ReceiptChequeBankingItemId, collection.Number, collection.Date,
                collection.BankId, collection.CreationDate), payment);
    }

    internal async Task<string?> GetChequeAssignmentBankTitleAsync(int bankId, CancellationToken ct)
    {
        var account = await sepidarContext.SepidarBankAccounts.AsNoTracking()
            .Where(x => x.BankAccountId == bankId)
            .Select(x => new { Title = x.Dl.Title, x.AccountNo })
            .SingleOrDefaultAsync(ct);
        if (account == null)
            return null;

        var title = TextService.NormalizePersianText(account.Title).Trim();
        var number = account.AccountNo.Trim();
        if (title.Length == 0)
            return number;
        return number.Length == 0 || TextService.ToEnglishNumber(title).Contains(TextService.ToEnglishNumber(number), StringComparison.Ordinal)
            ? title : $"{title} ({number})";
    }

    public async Task<object> GetChequeOperationOptionsAsync(DepositChequeAction action, CancellationToken ct)
    {
        if (action == DepositChequeAction.Collect)
            return new { };
        if (action == DepositChequeAction.Assign)
        {
            var currency = await GetDepositCurrencyAsync(ct);
            var bankAccounts = await sepidarContext.SepidarBankAccounts.AsNoTracking()
                .Where(x => x.CurrencyRef == currency && x.Dl.IsActive).OrderBy(x => x.AccountNo)
                .Select(x => new
                {
                    Value = x.BankAccountId, AccountName = x.Dl.Title, AccountOwner = x.Owner,
                    AccountNumber = x.AccountNo, CartNumber = x.CreditCardNumber, x.ShebaNumber,
                    BankName = x.SepidarBankBranch.SepidarBank.Title, BranchName = x.SepidarBankBranch.Title, DlCode = x.Dl.Code
                }).ToListAsync(ct);
            return new { BankAccounts = bankAccounts.Select(x => new
            {
                x.Value, x.AccountNumber, x.CartNumber, x.ShebaNumber, x.DlCode,
                AccountName = TextService.NormalizePersianText(x.AccountName), AccountOwner = TextService.NormalizePersianText(x.AccountOwner),
                BankName = TextService.NormalizePersianText(x.BankName), BranchName = TextService.NormalizePersianText(x.BranchName)
            }) };
        }
        var parties = await sepidarContext.Parties.AsNoTracking().Where(x => x.Dl.IsActive)
            .OrderBy(x => x.Dl.Code).Select(x => new { Id = x.PartyId, Title = x.Dl.Code + " - " + x.Dl.Title,
                x.IsVendor, x.IsCustomer, x.IsBroker }).ToListAsync(ct);
        var accounts = await DepositAccounts().OrderBy(x => x.Code).ThenBy(x => x.Id).ToListAsync(ct);
        var rial = await GetDepositCurrencyAsync(ct);
        var cash = await (from box in sepidarContext.SepidarCash.AsNoTracking()
            join dl in sepidarContext.DLs.AsNoTracking() on box.DlRef equals dl.DLId
            where box.CurrencyRef == rial && dl.IsActive
            orderby dl.Code
            select new DepositOption(box.CashId, dl.Code + " - " + box.Title)).ToListAsync(ct);
        var defaultPayable = await GetConfiguredAccountIdAsync("InventoryPayableSL", ct);
        var paymentTypes = await sepidarContext.Database.SqlQuery<DepositPaymentType>(
            $"SELECT Code AS Id, Title, DisplayOrder FROM FMK.Lookup WHERE Type = N'PaymentType'")
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var pettyCashParties = (await ActivePettyCashParties(rial).ToListAsync(ct)).ToHashSet();
        return new
        {
            PaymentTypes = paymentTypes.Select(x => new { x.Id, Title = TextService.NormalizePersianText(x.Title).Trim(),
                CanSpendCheque = IsChequePaymentType(x.Id) }),
            Payees = parties.Select(x => new { x.Id, Title = TextService.NormalizePersianText(x.Title),
                PaymentTypes = GetChequePayeeTypes(x.IsVendor, x.IsCustomer, x.IsBroker, pettyCashParties.Contains(x.Id)) }),
            Accounts = accounts.Select(x => new DepositOption(x.Id, x.Label)),
            Cash = cash.Select(x => x with { Title = TextService.NormalizePersianText(x.Title) }),
            DefaultPayable = defaultPayable,
            DefaultPaymentType = PaySupplier
        };
    }

    private sealed class DepositPaymentType
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int? DisplayOrder { get; set; }
    }

    private static bool IsChequePaymentType(int type) => type is PaySupplier or PayCustomer or PayBroker or PayOther or PayPettyCash;

    private IQueryable<int> ActivePettyCashParties(int currency) => sepidarContext.Database.SqlQuery<int>(
        $"SELECT DISTINCT PartyRef AS [Value] FROM RPA.PettyCash WHERE IsActive=1 AND CurrencyRef={currency}");

    private static int[] GetChequePayeeTypes(bool vendor, bool customer, bool broker, bool pettyCash)
    {
        var result = new System.Collections.Generic.List<int> { PayOther };
        if (vendor) result.Add(PaySupplier);
        if (customer) result.Add(PayCustomer);
        if (broker) result.Add(PayBroker);
        if (pettyCash) result.Add(PayPettyCash);
        return result.ToArray();
    }

    public async Task<ChequeOperationDraft> PrepareChequeOperationAsync(int receiptId, ChequeOperationInput input,
        bool transferred, bool collected, decimal crmAmount, CancellationToken ct)
    {
        var cheque = await sepidarContext.ReceiptCheques.AsNoTracking().Where(x => x.ReceiptHeaderRef == receiptId)
            .Take(2).ToListAsync(ct);
        if (cheque.Count != 1)
            throw new SepidarTransferValidationException("رسید مقصد باید دقیقاً یک چک قابل تطبیق داشته باشد.");
        return await BuildChequeOperationAsync(cheque[0], input, transferred, collected, crmAmount, ct);
    }

    private async Task<ChequeOperationDraft> BuildChequeOperationAsync(ReceiptCheque cheque, ChequeOperationInput input,
        bool transferred, bool collected, decimal crmAmount, CancellationToken ct)
    {
        var status = await ReadChequeStatusAsync(cheque, ct);
        if (input.Action == DepositChequeAction.None || status.Next(transferred, collected) != input.Action)
            throw new SepidarTransferValidationException("عملیات با وضعیت فعلی چک در سپیدار سازگار نیست. سوابق را دوباره بررسی کنید.");
        if (input.Date is { } operationDate && operationDate.Date < status.ReceivedDate.Date)
            throw new SepidarTransferValidationException("تاریخ عملیات نمی‌تواند قبل از تاریخ رسید دریافت باشد.");
        var isPayment = input.Action == DepositChequeAction.Spend;
        var isAssignment = input.Action == DepositChequeAction.Assign;
        if (input.Action == DepositChequeAction.Collect)
        {
            var collectionDate = input.Date ?? throw new SepidarTransferValidationException("تاریخ وصول چک مشخص نیست.");
            var assignedAt = status.Assignment!.Date;
            if (collectionDate.Date < assignedAt.Date)
                throw new SepidarTransferValidationException("تاریخ وصول نمی‌تواند قبل از تاریخ واگذاری به بانک باشد.");
            if (collectionDate.Date == assignedAt.Date)
            {
                var collectedAt = assignedAt.AddSeconds(1);
                if (collectedAt.Date != collectionDate.Date)
                    throw new SepidarTransferValidationException("امکان ثبت وصول بعد از این واگذاری در همان روز وجود ندارد.");
                input = input with { Date = collectedAt };
            }
            if (crmAmount != status.Amount)
                throw new SepidarTransferValidationException("مبلغ وصول CRM با مبلغ چک در سپیدار برابر نیست. اختلاف را قبل از ثبت وصول برطرف کنید.");
        }
        if (cheque.CurrencyRef != await GetDepositCurrencyAsync(ct) || cheque.IsGuarantee || status.Amount <= 0)
            throw new SepidarTransferValidationException("این عملیات فقط برای چک دریافتنی ریالی عادی قابل ثبت است.");
        var history = await sepidarContext.ReceiptChequeHistory.AsNoTracking().Where(x => x.ReceiptChequeRef == cheque.ReceiptChequeId)
            .OrderByDescending(x => x.ReceiptChequeHistoryId).FirstOrDefaultAsync(ct);
        if (history == null || history.State != status.State ||
            (input.Action == DepositChequeAction.Collect && history.ReceiptChequeBankingItemRef != status.Assignment?.ItemId))
            throw new SepidarTransferValidationException("آخرین گردش چک در سپیدار با وضعیت فعلی آن مطابقت ندارد.");
        var year = input.Date is { } dated ? await GetFiscalYearIdAsync(dated.Date, ct) : 0;
        var autoVoucher = await GetRequiredSepidarBooleanSettingAsync(isPayment ? "PaymentAutoVoucher" : "ReceiptBankingAutoVoucher", ct);
        var credit = autoVoucher
            ? await GetDepositAccountAsync(await GetConfiguredAccountIdAsync(
                input.Action == DepositChequeAction.Collect ? "SubmitChequeAccount" : "CashDocAccount", ct), "بستانکار", ct)
            : null;
        int effectiveBank = 0;
        int debitId;
        int debitDl;
        string targetTitle;
        string targetCode;
        if (isPayment)
        {
            if (!IsChequePaymentType(input.PaymentType))
                throw new SepidarTransferValidationException("ثبت خرج کردن چک برای این نوع اعلامیه در این گردش پشتیبانی نمی‌شود.");
            if (string.IsNullOrWhiteSpace(input.Description))
                throw new SepidarTransferValidationException("شرح اعلامیه پرداخت را وارد کنید.");
            var payee = await sepidarContext.Parties.AsNoTracking()
                .Where(x => x.PartyId == input.PayeeId && x.Dl.IsActive)
                .Select(x => new { Id = x.DLRef, x.Dl.Title, x.Dl.Code, x.IsVendor, x.IsCustomer, x.IsBroker }).SingleOrDefaultAsync(ct)
                ?? throw new SepidarTransferValidationException("طرف مقابل معتبر در سپیدار انتخاب کنید.");
            var pettyCash = input.PaymentType == PayPettyCash
                && await ActivePettyCashParties(cheque.CurrencyRef).AnyAsync(id => id == input.PayeeId, ct);
            if (!GetChequePayeeTypes(payee.IsVendor, payee.IsCustomer, payee.IsBroker, pettyCash).Contains(input.PaymentType))
                throw new SepidarTransferValidationException("طرف مقابل با نوع اعلامیه پرداخت انتخاب‌شده سازگار نیست.");
            if (!await (from box in sepidarContext.SepidarCash.AsNoTracking()
                    join dl in sepidarContext.DLs.AsNoTracking() on box.DlRef equals dl.DLId
                    where box.CashId == input.CashId && box.CurrencyRef == cheque.CurrencyRef && dl.IsActive
                    select box.CashId).AnyAsync(ct))
                throw new SepidarTransferValidationException("صندوق معتبر در سپیدار انتخاب کنید.");
            if (status.CashId.HasValue && input.CashId != status.CashId.Value)
                throw new SepidarTransferValidationException("صندوق اعلامیه باید همان صندوق چک در رسید دریافت باشد.");
            (debitId, debitDl, targetTitle, targetCode) = (input.AccountId, payee.Id, payee.Title, payee.Code);
        }
        else
        {
            effectiveBank = isAssignment ? input.BankId : status.Assignment!.BankId!.Value;
            var bank = await sepidarContext.SepidarBankAccounts.AsNoTracking()
                .Where(x => x.BankAccountId == effectiveBank && x.CurrencyRef == cheque.CurrencyRef && x.Dl.IsActive)
                .Select(x => new { Id = x.DlRef, x.Dl.Title, x.Dl.Code }).SingleOrDefaultAsync(ct)
                ?? throw new SepidarTransferValidationException("حساب بانکی ریالی معتبر در سپیدار انتخاب کنید.");
            debitId = autoVoucher
                ? await GetConfiguredAccountIdAsync(isAssignment ? "SubmitChequeAccount" : "BankAmountAccount", ct) : 0;
            debitDl = isAssignment ? cheque.DlRef : bank.Id;
            targetTitle = bank.Title;
            targetCode = bank.Code;
        }
        var debit = isPayment || autoVoucher ? await GetDepositAccountAsync(debitId, "بدهکار", ct) : null;
        return new(input, status, cheque.Version, history.ReceiptChequeHistoryId, year, debit?.Id ?? 0, credit?.Id ?? 0,
            debitDl, cheque.DlRef, effectiveBank, targetTitle, targetCode, debit?.Label, credit?.Label, autoVoucher);
    }

    public async Task<DepositDocument> AddChequeOperationAsync(int receiptId, ChequeOperationInput input,
        bool transferred, bool collected, decimal crmAmount, string? expectedPreview, int creator, CancellationToken ct)
    {
        if (!input.Date.HasValue)
            throw new SepidarTransferValidationException("تاریخ عملیات چک را وارد کنید.");

        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireSepidarInvoiceExportLockAsync(ct);
        var cheques = await sepidarContext.ReceiptCheques.Where(x => x.ReceiptHeaderRef == receiptId).Take(2).ToListAsync(ct);
        if (cheques.Count != 1)
            throw new SepidarTransferValidationException("چک رسید مقصد پیدا نشد یا قابل تطبیق نیست.");
        var cheque = cheques[0];
        var draft = await BuildChequeOperationAsync(cheque, input, transferred, collected, crmAmount, ct);
        if (GetChequeOperationPreviewToken(draft) != expectedPreview)
            throw new SepidarTransferValidationException("اطلاعات عملیات تغییر کرده است. پیش‌نمایش را دوباره بررسی کنید.");
        var document = input.Action == DepositChequeAction.Spend
            ? await AddChequePaymentAsync(cheque, draft, creator, ct)
            : await AddChequeBankingAsync(cheque, draft, creator, ct);
        await sepidarContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return document;
    }

    internal static string GetChequeOperationPreviewToken(ChequeOperationDraft draft) =>
        GetPurchasePreviewToken(draft.Input.Action == DepositChequeAction.Assign
            ? draft with { Input = draft.Input with { Date = null }, Year = 0 }
            : draft);

    private async Task<int> GetChequeOperationNumberAsync(ChequeOperationDraft draft, bool payment, CancellationToken ct)
    {
        var entity = payment ? PaymentNumberedEntity : BankingNumberedEntity;
        var config = await sepidarContext.Database.SqlQuery<PurchaseNumbering>(
            $"SELECT Method,StartValue,FinishValue,ApplyProperty1,ApplyProperty2,ApplyProperty3,ApplyPorperty4 FROM FMK.NumberedEntity WHERE EntityFullName={entity}")
            .SingleOrDefaultAsync(ct);
        if (config == null || config.Method != 2 || !config.ApplyProperty1 || config.ApplyProperty2 != payment)
            throw new SepidarTransferValidationException("روش شماره‌گذاری این عملیات در سپیدار پشتیبانی نمی‌شود.");
        var year = draft.Year;
        var paymentType = draft.Input.PaymentType;
        var last = await (payment
            ? sepidarContext.Database.SqlQuery<int?>($"SELECT MAX(Number) AS [Value] FROM RPA.PaymentHeader WITH (UPDLOCK,HOLDLOCK) WHERE FiscalYearRef={year} AND Type={paymentType}")
            : sepidarContext.Database.SqlQuery<int?>($"SELECT MAX(Number) AS [Value] FROM RPA.ReceiptChequeBanking WITH (UPDLOCK,HOLDLOCK) WHERE FiscalYearRef={year}"))
            .SingleAsync(ct);
        var next = Math.Max((long)last.GetValueOrDefault() + 1, config.StartValue);
        if (next < 1 || next > int.MaxValue || next > config.FinishValue)
            throw new SepidarTransferValidationException("شماره بعدی عملیات خارج از محدوده تنظیم‌شده در سپیدار است.");
        return (int)next;
    }

    private async Task<DepositDocument> AddChequeBankingAsync(ReceiptCheque cheque, ChequeOperationDraft draft, int creator, CancellationToken ct)
    {
        var assigning = draft.Input.Action == DepositChequeAction.Assign;
        var state = assigning ? ChequeAssigned : ChequeCollected;
        var date = draft.Input.Date!.Value;
        var now = DateTime.Now;
        var id = await GetLockedNextPurchaseIdAsync("RPA.ReceiptChequeBanking", 1, ct);
        var itemId = await GetLockedNextPurchaseIdAsync("RPA.ReceiptChequeBankingItem", 1, ct);
        var number = await GetChequeOperationNumberAsync(draft, false, ct);
        var operation = assigning ? "واگذار به بانك" : "وصول و واخواست";
        var headerText = $"بابت {operation} شماره {number} تاريخ {BaseServices.GetDatePersian(date)}";
        var lineText = $"بابت {(assigning ? "واگذار به بانك" : "وصول")} شماره {cheque.Number} تاريخ {BaseServices.GetDatePersian(cheque.Date)}";
        var voucherId = draft.AutoVoucher ? await AddChequeOperationVoucherAsync(
            draft, id, creator, BankingIssuer, headerText, lineText, lineText, ct) : (int?)null;
        var header = new ReceiptChequeBanking
        {
            ReceiptChequeBankingId = id, Date = date, Type = assigning ? AssignmentType : CollectionType,
            BankAccountRef = assigning ? draft.EffectiveBankId : null, Number = number, Creator = creator,
            Version = voucherId.HasValue ? 2 : 1, State = voucherId.HasValue ? 2 : 4, //فعلا برای وصول بدون سند
            FiscalYearRef = draft.Year,
            CreationDate = now, LastModifier = creator, LastModificationDate = now, VoucherRef = voucherId
        };
        var item = new ReceiptChequeBankingItem
        {
            ReceiptChequeBankingItemId = itemId, ReceiptChequeBankingRef = id, ReceiptChequeRef = cheque.ReceiptChequeId,
            ReceiptChequeBankingItemRef = assigning ? null : draft.Status.Assignment!.ItemId,
            ForcastDate = cheque.Date, State = state, HeaderDate = date, HeaderNumber = number,
            BankAccountRef = draft.EffectiveBankId, HeaderState = voucherId.HasValue ? 1 : 4 //فعلا
        };
        sepidarContext.ReceiptChequeBankings.Add(header);
        sepidarContext.ReceiptChequeBankingItems.Add(item);
        await SetChequeOperationHistoryAsync(cheque, draft, state, assigning ? 31 : 32, itemId, null, ct);
        return new(number, date);
    }

    private async Task<DepositDocument> AddChequePaymentAsync(ReceiptCheque cheque, ChequeOperationDraft draft, int creator, CancellationToken ct)
    {
        var input = draft.Input;
        var date = input.Date!.Value;
        var now = DateTime.Now;
        var id = await GetLockedNextPurchaseIdAsync("RPA.PaymentHeader", 1, ct);
        var itemId = await GetLockedNextPurchaseIdAsync("RPA.PaymentChequeOther", 1, ct);
        var number = await GetChequeOperationNumberAsync(draft, true, ct);
        var headerText = $"بابت پرداخت طي اعلاميه شماره {number} تاريخ {BaseServices.GetDatePersian(date)}";
        var debitText = $"واگذاري چك شماره {cheque.Number} تاريخ {BaseServices.GetDatePersian(cheque.Date)}";
        var creditText = $"{debitText} به {draft.TargetTitle} بابت {input.Description}";
        var voucherId = draft.AutoVoucher ? await AddChequeOperationVoucherAsync(
            draft, id, creator, PaymentIssuer, headerText, debitText, creditText, ct) : (int?)null;
        var payment = new PaymentHeader
        {
            PaymentHeaderId = id, Type = input.PaymentType, AccountSlRef = draft.DebitAccount, DlRef = draft.DebitDl,
            Number = number, Date = date, CurrencyRef = cheque.CurrencyRef, Description = input.Description,
            Description_En = input.Description, Discount = 0, TotalAmount = cheque.Amount, ItemType = 8,
            State = voucherId.HasValue ? 2 : 1, //فعلا برای خرج کردن بدون سند
            CreatorForm = "0", Creator = creator, LastModifier = creator,
            CreationDate = now, LastModificationDate = now, Version = voucherId.HasValue ? 2 : 1,
            Rate = cheque.Rate, CashRef = input.CashId, Amount = 0, AmountInBaseCurrency = 0, FiscalYearRef = draft.Year,
            VoucherRef = voucherId, DiscountRate = 1, DiscountInBaseCurrency = 0, TotalAmountInBaseCurrency = cheque.AmountInBaseCurrency
        };
        sepidarContext.PaymentHeaders.Add(payment);
        sepidarContext.PaymentChequeOthers.Add(new PaymentChequeOther
        {
            PaymentChequeOtherId = itemId, PaymentHeaderRef = id, ReceiptChequeRef = cheque.ReceiptChequeId,
            HeaderDate = date, HeaderNumber = number
        });
        await SetChequeOperationHistoryAsync(cheque, draft, ChequeSpent, 23, null, itemId, ct);
        return new(number, date);
    }

    private async Task SetChequeOperationHistoryAsync(ReceiptCheque cheque, ChequeOperationDraft draft, int state,
        int historyType, int? bankingItemId, int? paymentItemId, CancellationToken ct)
    {
        cheque.State = state;
        cheque.Version++;
        sepidarContext.ReceiptChequeHistory.Add(new ReceiptChequeHistory
        {
            ReceiptChequeHistoryId = await GetLockedNextPurchaseIdAsync("RPA.ReceiptChequeHistory", 1, ct),
            ReceiptChequeRef = cheque.ReceiptChequeId, ReceiptChequeHistoryRef = draft.HistoryId,
            ReceiptChequeBankingItemRef = bankingItemId, PaymentChequeOtherRef = paymentItemId,
            State = state, Type = historyType, Date = draft.Input.Date!.Value
        });
    }

    private async Task<int> AddChequeOperationVoucherAsync(ChequeOperationDraft draft, int sourceId, int creator,
        string issuer, string headerText, string debitText, string creditText, CancellationToken ct)
    {
        var id = await GetLockedNextPurchaseIdAsync("ACC.Voucher", 1, ct);
        var next = await GetNextVoucherNumbersAsync(draft.Year, ct);
        var date = draft.Input.Date!.Value;
        var now = DateTime.Now;
        var debitDl = await GetVoucherItemDlRefAsync(draft.DebitAccount, draft.DebitDl, ct);
        var creditDl = await GetVoucherItemDlRefAsync(draft.CreditAccount, draft.CreditDl, ct);
        sepidarContext.Vouchers.Add(new Voucher
        {
            VoucherId = id, Number = next.Number, ReferenceNumber = next.ReferenceNumber, Date = date,
            FiscalYearRef = draft.Year, State = 1, Type = 2, IssuerSystem = 2, Version = 1,
            Creator = creator, LastModifier = creator, CreationDate = now, LastModificationDate = now,
            DailyNumber = await GetNextVoucherDailyNumberAsync(date, draft.Year, ct),
            Description = headerText, Description_En = headerText
        });
        var last = await GetLockedNextPurchaseIdAsync("ACC.VoucherItem", 2, ct);
        var amount = draft.Status.Amount;
        sepidarContext.VoucherItems.AddRange(
            Line(last - 1, 1, draft.DebitAccount, debitDl, amount, 0, debitText),
            Line(last, 2, draft.CreditAccount, creditDl, 0, amount, creditText));
        await sepidarContext.SaveChangesAsync(ct);
        return id;

        VoucherItem Line(int itemId, int row, int account, int? dl, decimal debit, decimal credit, string text)
        {
            text = text[..Math.Min(text.Length, 250)];
            return new VoucherItem
            {
                VoucherItemId = itemId, VoucherRef = id, RowNumber = row, AccountSLRef = account, DLRef = dl,
                Debit = debit, Credit = credit, IssuerEntityName = issuer, IssuerEntityRef = sourceId,
                Description = text, Description_En = text, Version = 1
            };
        }
    }
}
