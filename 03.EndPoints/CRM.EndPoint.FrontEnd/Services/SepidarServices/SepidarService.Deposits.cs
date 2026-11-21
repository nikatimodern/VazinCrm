using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using CRM.Framework;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public partial class SepidarService
{
    public sealed record DepositOption(int Id, string Title);
    private sealed record DepositAccountOption(string Id, int AccountId, string Code, string Title);
    private static readonly (string Id, string Setting)[] DepositCreditSettings =
        [("receivable", "SalesReceivableSL"), ("advance", "AdvanceReceivedSL")];
    public sealed record DepositDocument(int Number, DateTime Date);
    public sealed record DepositInput(int Type, int CustomerId, int VendorId, int BankAccountId,
        int CashId, int BankId, string Reference, string Sayad, string AccountNo, string Description,
        string? CustomerAccount, string SecondNumber, string? ChequeOwner, string BranchCode, string? BranchTitle);
    public sealed record DepositDraft(DepositInput Input, decimal Amount, DateTime Date,
        DateTime? Due, int Year, int Currency, int CustomerDl, int CreditAccount, int DebitAccount,
        int DebitDl, bool AutoVoucher, string Customer, string DebitTitle, string CreditTitle);

    private const string ReceiptIssuer = "SG.ReceiptAndPayment.ReceiptManagement.Common.DsReceipt, SG.ReceiptAndPayment.ReceiptManagement.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
    private const string NoteIssuer = "SG.General.DebitCreditNoteManagement.Common.DsDebitCreditNote, SG.General.DebitCreditNoteManagement.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";

    public Task<DepositDocument?> FindDepositDocumentAsync(bool note, int id, CancellationToken ct) =>
        (note
            ? sepidarContext.DebitCreditNotes.AsNoTracking().Where(x => x.DebitCreditNoteID == id)
                .Select(x => new DepositDocument(x.Number, x.Date))
            : sepidarContext.ReceiptHeaders.AsNoTracking().Where(x => x.ReceiptHeaderId == id)
                .Select(x => new DepositDocument(x.Number, x.Date)))
        .SingleOrDefaultAsync(ct);

    public async Task<int> ReserveDepositIdAsync(bool note, CancellationToken ct)
    {
        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var id = await GetLockedNextPurchaseIdAsync(note ? "GNR.DebitCreditNote" : "RPA.ReceiptHeader", 1, ct);
        await transaction.CommitAsync(ct);
        return id;
    }

    public async Task<object> GetDepositOptionsAsync(int type, CancellationToken ct)
    {
        var customers = await sepidarContext.Parties.AsNoTracking().Where(p => p.IsCustomer && p.Dl != null && p.Dl.IsActive)
            .OrderBy(p => p.Name).Select(p => new DepositOption(p.PartyId, p.Dl.Code + " - " + p.Dl.Title)).ToListAsync(ct);
        var vendors = type == 4 ? await GetPurchaseVendorsAsync(null, ct) : [];
        var bankCurrency = type == 1 ? await GetDepositCurrencyAsync(ct) : 0;
        var accounts = type == 1 ? await sepidarContext.SepidarBankAccounts.AsNoTracking()
            .Where(x => x.CurrencyRef == bankCurrency)
            .OrderBy(x => x.AccountNo)
            .Select(x => new {
                Value = x.BankAccountId,
                AccountName = x.Dl.Title,
                AccountOwner = x.Owner,
                AccountNumber = x.AccountNo,
                CartNumber = x.CreditCardNumber,
                x.ShebaNumber,
                BankName = x.SepidarBankBranch.SepidarBank.Title,
                BranchName = x.SepidarBankBranch.Title,
                DlCode = x.Dl.Code
            }).ToListAsync(ct) : [];
        var cash = type != 4 ? await (
            from cashBox in sepidarContext.SepidarCash.AsNoTracking()
            join dl in sepidarContext.DLs.AsNoTracking() on cashBox.DlRef equals dl.DLId
            join currency in sepidarContext.Currencies.AsNoTracking() on cashBox.CurrencyRef equals currency.CurrencyID
            where currency.Title.Trim() == "ریال" || currency.Title.Trim() == "ريال"
            orderby dl.Code
            select new DepositOption(cashBox.CashId, dl.Code + " - " + cashBox.Title)).ToListAsync(ct) : [];
        var banks = type == 2 ? await sepidarContext.SepidarBanks.AsNoTracking()
            .OrderBy(x => x.Title).Select(x => new DepositOption(x.BankId, x.Title)).ToListAsync(ct) : [];
        var creditAccounts = new List<DepositAccountOption>(DepositCreditSettings.Length);
        foreach (var (id, setting) in DepositCreditSettings)
            creditAccounts.Add(await GetDepositCreditAccountAsync(id, setting, ct));
        return new {
            Customers = customers.Select(NormalizeOption),
            Vendors = vendors.Select(x => x with { Title = TextService.NormalizePersianText(x.Title) }),
            Accounts = accounts.Select(x => new {
                x.Value, x.AccountNumber, x.CartNumber, x.ShebaNumber, x.DlCode,
                AccountName = TextService.NormalizePersianText(x.AccountName),
                AccountOwner = TextService.NormalizePersianText(x.AccountOwner),
                BankName = TextService.NormalizePersianText(x.BankName),
                BranchName = TextService.NormalizePersianText(x.BranchName)
            }),
            Cash = cash.Select(NormalizeOption),
            Banks = banks.Select(NormalizeOption),
            CreditAccounts = creditAccounts.Select(x => x with { Title = TextService.NormalizePersianText(x.Title) })
        };

        static DepositOption NormalizeOption(DepositOption option) =>
            option with { Title = TextService.NormalizePersianText(option.Title) };
    }

    private async Task<DepositAccountOption> GetDepositCreditAccountAsync(string id, string setting, CancellationToken ct)
    {
        var accountId = await GetConfiguredAccountIdAsync(setting, ct);
        var accounts = sepidarContext.Accounts.AsNoTracking();
        return await (from subsidiary in accounts
            join general in accounts on subsidiary.ParentAccountRef equals (int?)general.AccountId
            join accountGroup in accounts on general.ParentAccountRef equals (int?)accountGroup.AccountId
            where subsidiary.AccountId == accountId
            select new DepositAccountOption(id, subsidiary.AccountId,
                accountGroup.Code.Trim() + general.Code.Trim() + subsidiary.Code.Trim(), subsidiary.Title))
            .SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("کد کامل معین دریافت در سپیدار پیدا نشد.");
    }

    internal async Task<DepositOption> GetDepositCustomerAsync(int partyId, CancellationToken ct) =>
        await sepidarContext.Parties.AsNoTracking()
            .Where(x => x.PartyId == partyId && x.IsCustomer && x.Dl != null && x.Dl.IsActive)
            .Select(x => new DepositOption(x.DLRef, x.Dl.Title)).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("مشتری معتبر در سپیدار انتخاب کنید.");

    internal async Task ValidateDepositBankAccountAsync(int bankId, CancellationToken ct) =>
        _ = await GetDepositBankAccountAsync(bankId, await GetDepositCurrencyAsync(ct), ct);

    private async Task<DepositOption> GetDepositBankAccountAsync(int bankId, int currency, CancellationToken ct) =>
        await sepidarContext.SepidarBankAccounts.AsNoTracking()
            .Where(x => x.BankAccountId == bankId && x.CurrencyRef == currency)
            .Select(x => new DepositOption(x.DlRef, x.AccountNo + " - " + x.ClearFormatName)).SingleOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("حساب بانکی ریالی مقصد را انتخاب کنید.");

    private Task<int> GetDepositCurrencyAsync(CancellationToken ct) =>
        sepidarContext.Currencies.AsNoTracking().Where(x => x.Title.Trim() == "ریال" || x.Title.Trim() == "ريال")
            .Select(x => x.CurrencyID).SingleAsync(ct);

    public async Task<DepositDraft> PrepareDepositAsync(DepositInput input, decimal amount, DateTime date,
        DateTime? due, bool validateFiscalYear, CancellationToken ct)
    {
        var note = input.Type == 4;
        var cheque = input.Type == 2;
        var year = validateFiscalYear ? await GetFiscalYearIdAsync(date, ct) : 0;
        var currency = await GetDepositCurrencyAsync(ct);
        var customer = await GetDepositCustomerAsync(input.CustomerId, ct);
        var (creditChoice, creditSetting) = DepositCreditSettings.SingleOrDefault(option => option.Id == input.CustomerAccount);
        if (creditSetting == null)
            throw new InvalidOperationException("معین سمت مشتری را انتخاب کنید.");
        var credit = await GetDepositCreditAccountAsync(creditChoice, creditSetting, ct);
        int debitDl;
        string debitTitle;
        if (note)
        {
            var vendor = await sepidarContext.Parties.AsNoTracking()
                .Where(x => x.PartyId == input.VendorId && x.IsVendor && x.Dl != null && x.Dl.IsActive)
                .Select(x => new DepositOption(x.DLRef, x.Dl.Title)).SingleOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("تأمین‌کننده بدهکار را انتخاب کنید.");
            (debitDl, debitTitle) = (vendor.Id, vendor.Title);
        }
        else
        {
            if (!await sepidarContext.SepidarCash.AsNoTracking()
                .AnyAsync(x => x.CashId == input.CashId && x.CurrencyRef == currency, ct))
                throw new InvalidOperationException("صندوق ریالی معتبر در سپیدار انتخاب کنید.");
            if (cheque)
            {
                if (!due.HasValue || string.IsNullOrWhiteSpace(input.Reference)
                    || input.Reference.Length > 50 || string.IsNullOrWhiteSpace(input.AccountNo) || input.AccountNo.Length > 50
                    || input.SecondNumber.Length > 50 || input.ChequeOwner?.Length > 250
                    || input.BranchCode.Length > 250 || input.BranchTitle?.Length > 250
                    || !await sepidarContext.SepidarBanks.AnyAsync(x => x.BankId == input.BankId, ct))
                    throw new InvalidOperationException("شماره چک، بانک، حساب صادرکننده و سررسید را وارد کنید و طول اطلاعات تکمیلی را بررسی کنید.");
                if (input.Sayad.Length > 0 && (input.Sayad.Length != 16 || !input.Sayad.All(ch => ch is >= '0' and <= '9')))
                    throw new InvalidOperationException("شناسه صیادی باید ۱۶ رقم باشد.");
                if (input.Sayad.Length > 0 && await sepidarContext.ReceiptCheques.AsNoTracking().AnyAsync(x => x.SayadCode == input.Sayad, ct))
                    throw new InvalidOperationException("چک با این شناسه صیادی قبلاً در سپیدار ثبت شده است.");
                (debitDl, debitTitle) = (customer.Id, customer.Title);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(input.Reference) || input.Reference.Length > 100)
                    throw new InvalidOperationException("شماره پیگیری حواله را وارد کنید.");
                var bank = await GetDepositBankAccountAsync(input.BankAccountId, currency, ct);
                (debitDl, debitTitle) = (bank.Id, bank.Title);
            }
        }
        var debit = await GetConfiguredAccountIdAsync(note ? "InventoryPayableSL" : cheque ? "CashDocAccount" : "BankAmountAccount", ct);
        var autoVoucher = await GetRequiredSepidarBooleanSettingAsync(note ? "DebitCreditNoteAutomaticVoucher" : "ReceiptAutoVoucher", ct);
        return new DepositDraft(input, amount, date.Date, due?.Date, year, currency, customer.Id, credit.AccountId, debit,
            debitDl, autoVoucher, customer.Title, debitTitle, credit.Code + " - " + credit.Title);
    }

    public async Task<DepositDocument> AddDepositAsync(int id, int creator, DepositDraft draft, CancellationToken ct)
    {
        var input = draft.Input;
        var note = input.Type == 4;
        var cheque = input.Type == 2;
        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireSepidarInvoiceExportLockAsync(ct);
        var existing = await FindDepositDocumentAsync(note, id, ct);
        if (existing != null) return existing;
        if (cheque && input.Sayad.Length > 0 && await sepidarContext.Database.SqlQuery<int>(
            $"SELECT ReceiptChequeId AS [Value] FROM RPA.ReceiptCheque WITH (UPDLOCK,HOLDLOCK) WHERE SayadCode={input.Sayad}").AnyAsync(ct))
            throw new InvalidOperationException("چک با این شناسه صیادی قبلاً در سپیدار ثبت شده است.");
        if (input.Type == 1 && await sepidarContext.Database.SqlQuery<int>(
            $"SELECT ReceiptDraftId AS [Value] FROM RPA.ReceiptDraft WITH (UPDLOCK,HOLDLOCK) WHERE BankAccountRef={input.BankAccountId} AND Number={input.Reference} AND Date={draft.Date} AND Amount={draft.Amount}").AnyAsync(ct))
            throw new InvalidOperationException("حواله با همین شماره پیگیری، حساب، تاریخ و مبلغ قبلاً در سپیدار ثبت شده است.");

        var year = draft.Year;
        var latest = await (note
            ? sepidarContext.Database.SqlQuery<int?>($"SELECT MAX(Number) AS [Value] FROM GNR.DebitCreditNote WITH (UPDLOCK,HOLDLOCK) WHERE FiscalYearRef={year}")
            : sepidarContext.Database.SqlQuery<int?>($"SELECT MAX(Number) AS [Value] FROM RPA.ReceiptHeader WITH (UPDLOCK,HOLDLOCK) WHERE FiscalYearRef={year}"))
            .SingleAsync(ct);
        var numberedEntity = note ? "SG.General.DebitCreditNoteManagement.Common.DebitCreditNoteRow"
            : "SG.ReceiptAndPayment.ReceiptManagement.Common.ReceiptHeaderRow";
        var numbering = await sepidarContext.Database.SqlQuery<PurchaseNumbering>(
            $"SELECT Method,StartValue,FinishValue,ApplyProperty1,ApplyProperty2,ApplyProperty3,ApplyPorperty4 FROM FMK.NumberedEntity WHERE EntityFullName={numberedEntity}")
            .SingleOrDefaultAsync(ct);
        if (numbering == null || numbering.Method != 2 || !numbering.ApplyProperty1)
            throw new InvalidOperationException("روش شماره‌گذاری سند در سپیدار برای این گردش پشتیبانی نمی‌شود.");
        var next = Math.Max((long)latest.GetValueOrDefault() + 1, numbering.StartValue); //فعلا
        if (next < 1 || next > int.MaxValue || next > numbering.FinishValue)
            throw new InvalidOperationException("شماره بعدی سند خارج از محدوده تنظیم‌شده در سپیدار است.");
        var number = (int)next;
        var autoVoucher = await GetRequiredSepidarBooleanSettingAsync(note ? "DebitCreditNoteAutomaticVoucher" : "ReceiptAutoVoucher", ct);
        if (autoVoucher != draft.AutoVoucher)
            throw new InvalidOperationException("تنظیم صدور سند حسابداری تغییر کرده است. پیش‌نمایش را دوباره بررسی کنید.");
        int? voucher = autoVoucher ? await AddDepositVoucherAsync(id, number, creator, draft, ct) : null;
        if (note)
            await AddDebitCreditNoteAsync(id, number, creator, draft, voucher, ct);
        else
            await AddReceiptAsync(id, number, creator, draft, voucher, ct);
        await sepidarContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new DepositDocument(number, draft.Date);
    }

    private async Task AddDebitCreditNoteAsync(int id, int number, int creator, DepositDraft draft,
        int? voucher, CancellationToken ct)
    {
        var now = DateTime.Now;
        var description = draft.Input.Description ?? "";
        var note = new DebitCreditNote
        {
            DebitCreditNoteID = id,
            Number = number,
            Date = draft.Date,
            CurrencyRef = draft.Currency,
            Rate = 1,
            Version = voucher.HasValue ? 2 : 1,
            Creator = creator,
            CreationDate = now,
            LastModifier = creator,
            LastModificationDate = now,
            VoucherRef = voucher,
            FiscalYearRef = draft.Year,
            SumAmount = draft.Amount,
            SumAmountInBaseCurrency = draft.Amount,
            Description = description,
            Description_En = description
        };
        var item = new DebitCreditNoteItem
        {
            DebitCreditNoteItemID = await GetLockedNextPurchaseIdAsync("GNR.DebitCreditNoteItem", 1, ct),
            DebitCreditNoteRef = id,
            RowID = 1,
            DebitSLRef = draft.DebitAccount,
            CreditSLRef = draft.CreditAccount,
            DebitDLRef = draft.DebitDl,
            CreditDLRef = draft.CustomerDl,
            DebitType = 1,
            CreditType = 2,
            Amount = draft.Amount,
            Rate = 1,
            AmountInBaseCurrency = draft.Amount,
            Description = description,
            Description_En = description,
            Version = 1
        };
        sepidarContext.DebitCreditNotes.Add(note);
        sepidarContext.DebitCreditNoteItems.Add(item);
    }

    private async Task AddReceiptAsync(int id, int number, int creator, DepositDraft draft,
        int? voucher, CancellationToken ct)
    {
        var input = draft.Input;
        var cheque = input.Type == 2;
        var description = input.Description ?? "";
        var now = DateTime.Now;
        var receipt = new ReceiptHeader
        {
            ReceiptHeaderId = id,
            Type = 1,
            AccountSlRef = draft.CreditAccount,
            DlRef = draft.CustomerDl,
            Number = number,
            Date = draft.Date,
            CurrencyRef = draft.Currency,
            Description = description,
            Description_En = description,
            Discount = 0,
            TotalAmount = draft.Amount,
            ItemType = cheque ? 4 : 8,
            State = voucher.HasValue ? 2 : 1, //فعلا
            CreatorForm = "0",
            Creator = creator,
            CreationDate = now,
            LastModifier = creator,
            LastModificationDate = now,
            Version = voucher.HasValue ? 2 : 1,
            Rate = 1,
            CashRef = input.CashId,
            Amount = 0,
            AmountInBaseCurrency = 0,
            FiscalYearRef = draft.Year,
            VoucherRef = voucher,
            DiscountRate = 1,
            DiscountInBaseCurrency = 0,
            TotalAmountInBaseCurrency = draft.Amount
        };
        sepidarContext.ReceiptHeaders.Add(receipt);
        if (cheque)
            await AddReceiptChequeAsync(receipt, draft, ct);
        else
            await AddReceiptDraftAsync(receipt, draft, ct);
    }

    private async Task AddReceiptDraftAsync(ReceiptHeader receipt, DepositDraft draft, CancellationToken ct)
    {
        var row = new ReceiptDraft
        {
            ReceiptDraftId = await GetLockedNextPurchaseIdAsync("RPA.ReceiptDraft", 1, ct),
            Number = draft.Input.Reference,
            Date = receipt.Date,
            Amount = draft.Amount,
            BankAccountRef = draft.Input.BankAccountId,
            Description = receipt.Description,
            Description_En = receipt.Description_En,
            Version = 1,
            ReceiptHeaderRef = receipt.ReceiptHeaderId,
            HeaderNumber = receipt.Number,
            HeaderDate = receipt.Date,
            CurrencyRef = receipt.CurrencyRef,
            Rate = 1,
            AmountInBaseCurrency = draft.Amount
        };
        sepidarContext.ReceiptDrafts.Add(row);
    }

    private async Task AddReceiptChequeAsync(ReceiptHeader receipt, DepositDraft draft, CancellationToken ct)
    {
        var input = draft.Input;
        var cheque = new ReceiptCheque
        {
            ReceiptChequeId = await GetLockedNextPurchaseIdAsync("RPA.ReceiptCheque", 1, ct),
            Number = input.Reference,
            IsGuarantee = false,
            AccountNo = input.AccountNo,
            Amount = draft.Amount,
            Date = draft.Due,
            Description = receipt.Description,
            Description_En = receipt.Description_En,
            Version = 2,
            State = 1,
            CashRef = receipt.CashRef,
            ReceiptHeaderRef = receipt.ReceiptHeaderId,
            BankRef = input.BankId,
            HeaderNumber = receipt.Number,
            HeaderDate = receipt.Date,
            CurrencyRef = receipt.CurrencyRef,
            Rate = 1,
            AmountInBaseCurrency = draft.Amount,
            DlRef = receipt.DlRef,
            Type = 1,
            InitState = 1,
            SayadCode = EmptyToNull(input.Sayad),
            SecondNumber = EmptyToNull(input.SecondNumber),
            ChequeOwner = EmptyToNull(input.ChequeOwner),
            BranchCode = EmptyToNull(input.BranchCode),
            BranchTitle = EmptyToNull(input.BranchTitle)
        };
        var history = new ReceiptChequeHistory
        {
            ReceiptChequeHistoryId = await GetLockedNextPurchaseIdAsync("RPA.ReceiptChequeHistory", 1, ct),
            State = 1,
            Type = 11,
            Date = receipt.Date,
            ReceiptChequeRef = cheque.ReceiptChequeId
        };
        sepidarContext.ReceiptCheques.Add(cheque);
        sepidarContext.ReceiptChequeHistory.Add(history);
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private async Task<int> AddDepositVoucherAsync(int sourceId, int number, int creator, DepositDraft draft, CancellationToken ct)
    {
        var note = draft.Input.Type == 4;
        var id = await GetLockedNextPurchaseIdAsync("ACC.Voucher", 1, ct);
        var numbers = await GetNextVoucherNumbersAsync(draft.Year, ct);
        var dateText = BaseServices.GetDatePersian(draft.Date);
        var text = note ? $"بابت اعلامیه بدهکار/بستانکار شماره {number} تاریخ {dateText}"
            : $"بابت دریافت طی رسید دریافت شماره {number} تاریخ {dateText}";
        var lineText = string.IsNullOrWhiteSpace(draft.Input.Description) ? text : $"{text} [{draft.Input.Description}]";
        lineText = lineText[..Math.Min(lineText.Length, 250)];
        var now = DateTime.Now;
        sepidarContext.Vouchers.Add(new Voucher {
            VoucherId = id, Number = numbers.Number, ReferenceNumber = numbers.ReferenceNumber, Date = draft.Date,
            FiscalYearRef = draft.Year, State = 1, Type = 2, IssuerSystem = note ? 5 : 2, Version = 1,
            Creator = creator, LastModifier = creator, CreationDate = now, LastModificationDate = now,
            DailyNumber = await GetNextVoucherDailyNumberAsync(draft.Date, draft.Year, ct),
            Description = text, Description_En = text
        });
        var lastItem = await GetLockedNextPurchaseIdAsync("ACC.VoucherItem", 2, ct);
        var debitDl = await GetVoucherItemDlRefAsync(draft.DebitAccount, draft.DebitDl, ct);
        var creditDl = await GetVoucherItemDlRefAsync(draft.CreditAccount, draft.CustomerDl, ct);
        sepidarContext.VoucherItems.AddRange(
            Line(lastItem - 1, 1, draft.DebitAccount, debitDl, draft.Amount, 0),
            Line(lastItem, 2, draft.CreditAccount, creditDl, 0, draft.Amount));
        await sepidarContext.SaveChangesAsync(ct);
        return id;

        VoucherItem Line(int itemId, int row, int account, int? dl, decimal debit, decimal credit) => new() {
            VoucherItemId = itemId, VoucherRef = id, RowNumber = row, AccountSLRef = account, DLRef = dl,
            Debit = debit, Credit = credit, IssuerEntityName = note ? NoteIssuer : ReceiptIssuer,
            IssuerEntityRef = sourceId, Description = lineText, Description_En = lineText, Version = 1
        };
    }
}
