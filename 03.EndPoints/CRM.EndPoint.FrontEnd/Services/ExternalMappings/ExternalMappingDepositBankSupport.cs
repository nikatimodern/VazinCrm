using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Entities;
using CRM.Core.Domain.Accountancies.Enum;
using CRM.Core.Domain.ExternalSoftwares.Entites;
using CRM.Core.Domain.ExternalSoftwares.Enums;
using CRM.Core.Domain.PreFactors.Enum;
using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Infrastructures.Sql;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.ExternalMappings;

public sealed partial class ExternalMappingService
{
    // Keep the same eligibility and currency as the deposit transfer workflow.
    private static IQueryable<Accountancy> EligibleDeposits(IQueryable<Accountancy> query) =>
        query.Where(x => !x.IsDeleted && x.AccType == AcEnum.واریزی && x.Status == AccStatusEnum.تایید_شده
            && (x.PayType == PayTypeEnum.نقدی || x.PayType == PayTypeEnum.چکی || x.PayType == PayTypeEnum.تهاتری)
            && x.CurrencyType == CurrencyTypeEnum.ریال && x.credit > 0
            && x.credit <= 999999999999999.9999m && x.CustomerId.HasValue);

    private static Task<int> MappingRialCurrencyAsync(SepidarContextDbBase context, CancellationToken ct) =>
        context.Currencies.AsNoTracking().Where(x => x.Title.Trim() == "ریال" || x.Title.Trim() == "ريال")
            .Select(x => x.CurrencyID).SingleAsync(ct);

    private static async Task<bool> BankMappingTargetExistsAsync(SepidarContextDbBase context, int id, CancellationToken ct)
    {
        var currency = await MappingRialCurrencyAsync(context, ct);
        return await context.SepidarBankAccounts.AsNoTracking()
            .AnyAsync(x => x.BankAccountId == id && x.CurrencyRef == currency, ct);
    }

    private static Task<bool> DepositDocumentExistsAsync(SepidarContextDbBase context, string entity, int id, CancellationToken ct) =>
        entity == nameof(DebitCreditNote)
            ? context.DebitCreditNotes.AsNoTracking().AnyAsync(x => x.DebitCreditNoteID == id, ct)
            : context.ReceiptHeaders.AsNoTracking().AnyAsync(x => x.ReceiptHeaderId == id, ct);

    private static async Task<bool> DepositMappingTargetExistsAsync(SepidarContextDbBase context, string entity,
        int id, CancellationToken ct)
    {
        var currency = await MappingRialCurrencyAsync(context, ct);
        return entity == nameof(DebitCreditNote)
            ? await context.DebitCreditNotes.AsNoTracking().AnyAsync(x => x.DebitCreditNoteID == id && x.CurrencyRef == currency, ct)
            : await context.ReceiptHeaders.AsNoTracking().AnyAsync(x => x.ReceiptHeaderId == id && x.CurrencyRef == currency
                && x.Type == 1 && (x.ItemType == 4 || x.ItemType == 8), ct);
    }

    private async Task ValidateDepositMappingAsync(SepidarContextDbBase context, Guid sourceId, string entity, int id,
        IReadOnlyCollection<ExternalMapping> existing, CancellationToken ct)
    {
        var source = await EligibleDeposits(db.Accountancies.AsNoTracking())
            .SingleOrDefaultAsync(x => x.Id == sourceId, ct)
            ?? throw new ArgumentException("فقط واریزی ریالی نقدی، چکی یا تهاتری تأییدشده قابل تطبیق است.");
        if (source.PayType == PayTypeEnum.تهاتری != (entity == nameof(DebitCreditNote)))
            throw new ArgumentException("نوع سند مقصد با نوع واریزی سازگار نیست.");
        var currency = await MappingRialCurrencyAsync(context, ct);
        var customerMaps = existing.Where(x => x.MapType == ExternalMappingTypeEnum.مشتری &&
            x.EntityId == source.CustomerId && x.ExEntityName == "Party").ToArray();
        int? customerDl = null;
        if (customerMaps.Length > 0)
        {
            if (customerMaps.Length != 1 || !int.TryParse(customerMaps[0].ExEntityId, out var party))
                throw new MappingConflictException("ارتباط مشتری را بررسی کنید.");
            customerDl = await context.Parties.AsNoTracking().Where(x => x.PartyId == party && x.IsCustomer && x.Dl != null && x.Dl.IsActive)
                .Select(x => (int?)x.DLRef).SingleOrDefaultAsync(ct);
            if (!customerDl.HasValue) throw new MappingConflictException("مشتری متناظر در سپیدار معتبر نیست.");
        }
        if (entity == nameof(DebitCreditNote))
        {
            var note = await context.DebitCreditNotes.AsNoTracking()
                .Where(x => x.DebitCreditNoteID == id && x.CurrencyRef == currency)
                .Select(x => new { x.SumAmount }).SingleOrDefaultAsync(ct);
            if (note == null || note.SumAmount != source.credit)
                throw new ArgumentException("اعلامیه بدهکار بستانکار یا مبلغ آن با واریزی سازگار نیست.");
            var items = await context.DebitCreditNoteItems.AsNoTracking()
                .Where(x => x.DebitCreditNoteRef == id).Select(x => new { x.CreditType, x.CreditDLRef, x.Amount }).ToListAsync(ct);
            if (items.Count != 1 || items[0].Amount != source.credit
                || (customerDl.HasValue && items[0].CreditType == 2 && items[0].CreditDLRef != customerDl))
                throw new MappingConflictException("قلم بستانکار اعلامیه با اطلاعات واریزی سازگار نیست.");
            return;
        }
        var cheque = source.PayType == PayTypeEnum.چکی;
        var receipt = await context.ReceiptHeaders.AsNoTracking()
            .Where(x => x.ReceiptHeaderId == id && x.Type == 1 && x.CurrencyRef == currency && x.ItemType == (cheque ? 4 : 8))
            .Select(x => new { x.DlRef, x.TotalAmount }).SingleOrDefaultAsync(ct);
        if (receipt == null || (!(cheque && source.Vosoled) && receipt.TotalAmount != source.credit)
            || (customerDl.HasValue && receipt.DlRef != customerDl))
            throw new ArgumentException("نوع، مبلغ یا مشتری رسید دریافت با واریزی سازگار نیست.");
        if (cheque)
        {
            var rows = await context.ReceiptCheques.AsNoTracking()
                .Where(x => x.ReceiptHeaderRef == id).Select(x => new { x.IsGuarantee, x.Type, x.Amount, x.Date }).ToListAsync(ct);
            if (rows.Count != 1 || rows[0].IsGuarantee || rows[0].Type != 1 || rows[0].Amount != receipt.TotalAmount
                || (!source.Vosoled && source.VosolDateM.HasValue && rows[0].Date?.Date != source.VosolDateM.Value.Date))
                throw new ArgumentException("چک مرتبط با رسید دریافت معتبر نیست.");
        }
        else
        {
            var rows = await context.ReceiptDrafts.AsNoTracking()
                .Where(x => x.ReceiptHeaderRef == id).Select(x => new { x.BankAccountRef, x.Amount }).ToListAsync(ct);
            if (rows.Count != 1 || !rows[0].BankAccountRef.HasValue || rows[0].Amount != source.credit)
                throw new ArgumentException("حواله بانکی مرتبط با رسید دریافت معتبر نیست.");
            var bankMaps = existing.Where(x => x.MapType == ExternalMappingTypeEnum.حساب_بانکی &&
                x.EntityId == source.BankAccountId && x.ExEntityName == "BankAccount").ToArray();
            if (bankMaps.Length > 0 && (bankMaps.Length != 1 || bankMaps[0].ExEntityId != rows[0].BankAccountRef.Value.ToString()))
                throw new MappingConflictException("حساب بانکی رسید با ارتباط ثبت‌شده حساب CRM یکسان نیست.");
        }
    }

    // Acquire the same source lock used by the deposit transfer and bank-choice services.
    private async Task<IAsyncDisposable> AcquireMappingLocksAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var locks = new MappingLockSet();
        try
        {
            foreach (var id in ids.Distinct().OrderBy(x => x))
                locks.Items.Add(await depositRepository.LockAsync(id, ct));
            return locks;
        }
        catch (InvalidOperationException)
        {
            await locks.DisposeAsync();
            throw new MappingConflictException("عملیات مالی این رکورد در حال انجام است؛ دوباره تلاش کنید.");
        }
        catch
        {
            await locks.DisposeAsync();
            throw;
        }
    }

    private sealed class MappingLockSet : IAsyncDisposable
    {
        public List<IAsyncDisposable> Items { get; } = new();
        public async ValueTask DisposeAsync()
        {
            for (var i = Items.Count - 1; i >= 0; i--)
                await Items[i].DisposeAsync();
        }
    }

    private async Task LoadCrmDepositBankSidesAsync(List<MappingReportEntry> entries,
        Dictionary<(string, Guid), MappingReportSide> sides, CancellationToken ct)
    {
        foreach (var batch in entries.Where(x => x.Kind == nameof(Accountancy)).Select(x => x.EntityId).Distinct().Chunk(350))
        {
            var rows = await db.Accountancies.AsNoTracking().Where(x => batch.Contains(x.Id))
                .Select(x => new { x.Id, x.CustomerName, x.credit, x.PayType, x.PayTypeTxt, x.ReciveDateM, x.VosolDateM,
                    x.date, x.CreateDate, x.BankAccountTxt, x.CustomerCode }).ToListAsync(ct);
            foreach (var x in rows)
                sides[(nameof(Accountancy), x.Id)] = new MappingReportSide("واریزی CRM", x.Id.ToString(),
                    x.PayTypeTxt ?? x.PayType.ToString(), FormatLegacyPersianDate(
                        (x.PayType == PayTypeEnum.چکی ? x.ReciveDateM : x.VosolDateM ?? x.date) ?? x.CreateDate),
                    x.credit, TextService.NormalizePersianText(x.CustomerName ?? ""),
                    string.IsNullOrWhiteSpace(x.BankAccountTxt) ? "" : "حساب: " + x.BankAccountTxt);
        }
        foreach (var batch in entries.Where(x => x.Kind == nameof(BankAccount)).Select(x => x.EntityId).Distinct().Chunk(350))
        {
            var rows = await db.BankAccounts.AsNoTracking().Where(x => batch.Contains(x.Id))
                .Select(x => new { x.Id, x.AccountName, x.AccountNumber, x.BankName, x.ShebaNumber, x.Active }).ToListAsync(ct);
            foreach (var x in rows)
                sides[(nameof(BankAccount), x.Id)] = new MappingReportSide("حساب بانکی CRM", x.Id.ToString(),
                    x.AccountNumber ?? "", "", null, TextService.NormalizePersianText($"{x.BankName} | {x.AccountName}"),
                    "شبا: " + (x.ShebaNumber ?? "ثبت نشده") + (x.Active ? "" : " | غیرفعال"));
        }
    }

    private static async Task LoadSepidarDepositBankSidesAsync(List<MappingReportEntry> entries,
        SepidarContextDbBase context, Dictionary<(string, int), MappingReportSide> sides, CancellationToken ct)
    {
        foreach (var batch in ExternalTargetBatches(entries, nameof(ReceiptHeader)))
        {
            var rows = await context.ReceiptHeaders.AsNoTracking().Where(x => batch.Contains(x.ReceiptHeaderId))
                .Select(x => new { x.ReceiptHeaderId, x.Number, x.Date, x.TotalAmount, x.ItemType,
                    Name = context.DLs.Where(d => d.DLId == x.DlRef).Select(d => d.Title).FirstOrDefault() }).ToListAsync(ct);
            foreach (var x in rows)
                sides[(nameof(ReceiptHeader), x.ReceiptHeaderId)] = new MappingReportSide("رسید دریافت",
                    x.ReceiptHeaderId.ToString(), x.Number.ToString(), FormatLegacyPersianDate(x.Date),
                    x.TotalAmount, TextService.NormalizePersianText(x.Name ?? ""), x.ItemType == 4 ? "چکی" : "نقدی");
        }
        foreach (var batch in ExternalTargetBatches(entries, nameof(DebitCreditNote)))
        {
            var rows = await context.DebitCreditNotes.AsNoTracking().Where(x => batch.Contains(x.DebitCreditNoteID))
                .Select(x => new { x.DebitCreditNoteID, x.Number, x.Date, x.SumAmount, x.Description }).ToListAsync(ct);
            foreach (var x in rows)
                sides[(nameof(DebitCreditNote), x.DebitCreditNoteID)] = new MappingReportSide("اعلامیه بدهکار بستانکار",
                    x.DebitCreditNoteID.ToString(), x.Number.ToString(), FormatLegacyPersianDate(x.Date),
                    x.SumAmount, "تهاتری", TextService.NormalizePersianText(x.Description ?? ""));
        }
        foreach (var batch in ExternalTargetBatches(entries, "BankAccount"))
        {
            var rows = await context.SepidarBankAccounts.AsNoTracking().Where(x => batch.Contains(x.BankAccountId))
                .Select(x => new { x.BankAccountId, x.AccountNo, x.ShebaNumber, x.Owner,
                    Name = x.Dl.Title, Bank = x.SepidarBankBranch.SepidarBank.Title }).ToListAsync(ct);
            foreach (var x in rows)
                sides[("BankAccount", x.BankAccountId)] = new MappingReportSide("حساب بانکی سپیدار",
                    x.BankAccountId.ToString(), x.AccountNo, "", null,
                    TextService.NormalizePersianText($"{x.Bank} | {x.Name}"), "شبا: " + (x.ShebaNumber ?? "ثبت نشده"));
        }
    }

    private async Task<(string[] Receipts, string[] Notes, string[] Banks)> FindFinancialMappingSearchAsync(
        SepidarContextDbBase context, string search, int number, bool isNumber, HashSet<Guid> matched, CancellationToken ct)
    {
        var hasAmount = decimal.TryParse(search, out var amount);
        matched.UnionWith(await db.Accountancies.AsNoTracking()
            .Where(x => x.AccType == AcEnum.واریزی && ((x.CustomerName ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                || (x.BankAccountTxt ?? "").Contains(search) || (x.dateSh ?? "").Contains(search)
                || (x.ReciveDate ?? "").Contains(search) || (x.VosolDate ?? "").Contains(search)
                || (hasAmount && x.credit == amount) || (isNumber && x.CustomerCode == number)))
            .Select(x => x.Id).Take(500).ToListAsync(ct));
        matched.UnionWith(await db.BankAccounts.AsNoTracking()
            .Where(x => (x.AccountNumber ?? "").Contains(search) || (x.ShebaNumber ?? "").Contains(search)
                || (x.CartNumber ?? "").Contains(search)
                || (x.BankName ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                || (x.AccountName ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search))
            .Select(x => x.Id).Take(500).ToListAsync(ct));
        if (context == null) return (Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        var receipts = await context.ReceiptHeaders.AsNoTracking().Where(x =>
                (isNumber && (x.Number == number || x.ReceiptHeaderId == number)) || (hasAmount && x.TotalAmount == amount)
                || (x.Description ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                || context.DLs.Any(d => d.DLId == x.DlRef && (d.Title ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)))
            .Select(x => x.ReceiptHeaderId).Take(500).ToListAsync(ct);
        var notes = await context.DebitCreditNotes.AsNoTracking().Where(x =>
                (isNumber && (x.Number == number || x.DebitCreditNoteID == number)) || (hasAmount && x.SumAmount == amount)
                || (x.Description ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search))
            .Select(x => x.DebitCreditNoteID).Take(500).ToListAsync(ct);
        var banks = await context.SepidarBankAccounts.AsNoTracking().Where(x =>
                x.AccountNo.Contains(search) || (x.ShebaNumber ?? "").Contains(search) || (x.CreditCardNumber ?? "").Contains(search)
                || (x.Dl.Title ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search)
                || (x.SepidarBankBranch.SepidarBank.Title ?? "").Replace("ي", "ی").Replace("ى", "ی").Replace("ك", "ک").Contains(search))
            .Select(x => x.BankAccountId).Take(500).ToListAsync(ct);
        return (receipts.Select(x => x.ToString()).ToArray(), notes.Select(x => x.ToString()).ToArray(), banks.Select(x => x.ToString()).ToArray());
    }
}
