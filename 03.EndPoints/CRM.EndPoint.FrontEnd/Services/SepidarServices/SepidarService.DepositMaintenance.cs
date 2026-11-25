using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Core.Domain.Sepidar.Deposits.Entities;
using CRM.Core.Domain.Sepidar.Payments.Entites;
using CRM.Core.Domain.Sepidar.Vouchers.Entites;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static CRM.Core.Domain.Accountancies.Repositories.DepositDependencyTarget;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public partial class SepidarService
{
    internal sealed record DepositMaintenanceImpact(int Number, DateTime Date, string Token, int? VoucherNumber,
        bool? AutoVoucher, IReadOnlyList<string> Blockers);
    internal sealed record DepositMaintenanceDetails(DepositMaintenanceImpact Impact, DepositInput? Input,
        decimal Amount, DateTime? Due, ChequeOperationInput? PaymentInput);
    internal sealed record DepositMaintenanceResult(bool Deleted, bool Saved, bool? VoucherIssued);

    private sealed record DepositMaintenancePlan(DepositMaintenanceImpact Impact, int TargetId, int Year,
        DepositDependencyTarget Target, Voucher? OldVoucher, ReceiptHeader? Receipt = null, ReceiptDraft? Draft = null,
        ReceiptCheque? Cheque = null, DebitCreditNote? Note = null, DebitCreditNoteItem? NoteItem = null,
        PaymentHeader? Payment = null, int? BankingItemId = null, int? PaymentItemId = null,
        ReceiptChequeHistory? LastHistory = null, int? PreviousChequeState = null);

    internal async Task<DepositMaintenanceDetails> GetDepositMaintenanceAsync(int id, int type, DepositChequeAction action,
        bool edit, Guid software, IDepositFinancialRepository dependencies, CancellationToken ct)
    {
        var plan = await BuildDepositMaintenancePlanAsync(id, type, action, edit, software, dependencies, ct);
        var input = edit && action == DepositChequeAction.None && plan.Impact.Blockers.Count == 0
            ? await ReadDepositEditInputAsync(type, plan, ct) : null;
        var paymentInput = edit && plan.Payment is { } payment && plan.Impact.Blockers.Count == 0
            ? new ChequeOperationInput(DepositChequeAction.Spend, payment.Date, 0, payment.Type,
                await ReadDepositPartyIdAsync(payment.DlRef, ct), payment.AccountSlRef ?? 0,
                payment.CashRef ?? 0, payment.Description)
            : null;
        return new(plan.Impact, input, plan.Note?.SumAmount ?? plan.Receipt?.TotalAmount ?? 0m,
            plan.Cheque?.Date, paymentInput);
    }

    private async Task<DepositMaintenancePlan> BuildDepositMaintenancePlanAsync(int id, int type, DepositChequeAction action,
        bool edit, Guid software, IDepositFinancialRepository dependencies, CancellationToken ct)
    {
        var db = sepidarContext;
        var blockers = new List<string>();
        async Task Check(DepositDependencyTarget target, int key, string message)
        {
            if (await dependencies.HasDestinationDependenciesAsync(software, target, key, ct))
                blockers.Add(message);
        }
        const string dependent = "این سند در گردش دیگری در سپیدار استفاده شده است.";
        const string multiple = "این سند فقط به چک و رسید دریافت این واریزی اختصاص ندارد.";
        const string mismatch = "ساختار سند مقصد با واریزی ثبت‌شده مطابقت ندارد.";
        var target = Receipt;
        var targetId = id;
        int number, year, version;
        int? voucher;
        DateTime date;
        string issuer, autoKey;
        int issuerSystem;
        ReceiptHeader? receipt = null;
        ReceiptDraft? draft = null;
        ReceiptCheque? cheque = null;
        DebitCreditNote? note = null;
        DebitCreditNoteItem? noteItem = null;
        PaymentHeader? payment = null;
        ReceiptChequeHistory? latest = null;
        int? bankingItemId = null, paymentItemId = null, previousState = null;
        if (type == 4 && action == DepositChequeAction.None)
        {
            target = Note;
            note = await db.DebitCreditNotes.AsNoTracking().SingleOrDefaultAsync(x => x.DebitCreditNoteID == id, ct)
                ?? throw new SepidarTransferValidationException("اعلامیه بدهکار/بستانکار در سپیدار پیدا نشد.");
            var rows = await db.DebitCreditNoteItems.AsNoTracking().Where(x => x.DebitCreditNoteRef == id).Take(2).ToListAsync(ct);
            if (rows.Count != 1 || rows[0].Amount != note.SumAmount || note.Rate != 1m)
                blockers.Add(mismatch);
            else
            {
                noteItem = rows[0];
                await Check(NoteItem, noteItem.DebitCreditNoteItemID, dependent);
            }
            (number, year, version, voucher, date, issuer, issuerSystem, autoKey) =
                (note.Number, note.FiscalYearRef, note.Version, note.VoucherRef, note.Date, NoteIssuer, 5, "DebitCreditNoteAutomaticVoucher");
        }
        else
        {
            receipt = await db.ReceiptHeaders.AsNoTracking().SingleOrDefaultAsync(x => x.ReceiptHeaderId == id, ct)
                ?? throw new SepidarTransferValidationException("رسید دریافت در سپیدار پیدا نشد.");
            var cheques = await db.ReceiptCheques.AsNoTracking().Where(x => x.ReceiptHeaderRef == id).Take(2).ToListAsync(ct);
            var drafts = await db.ReceiptDrafts.AsNoTracking().Where(x => x.ReceiptHeaderRef == id).Take(2).ToListAsync(ct);
            if (receipt.Type != 1 || receipt.Rate != 1m || receipt.Discount != 0m || receipt.Amount.GetValueOrDefault() != 0m)
                blockers.Add(mismatch);
            if (type == 2 && cheques.Count == 1 && drafts.Count == 0 && receipt.ItemType == 4)
            {
                cheque = cheques[0];
                if (cheque.IsGuarantee || cheque.InitState != ChequeInCash || cheque.Amount != receipt.TotalAmount || cheque.Rate != 1m)
                    blockers.Add(mismatch);
            }
            else if (type == 1 && drafts.Count == 1 && cheques.Count == 0 && receipt.ItemType == 8)
            {
                draft = drafts[0];
                if (draft.Amount != receipt.TotalAmount || draft.Rate != 1m)
                    blockers.Add(mismatch);
                await Check(Draft, draft.ReceiptDraftId, "حواله این رسید در مغایرت‌گیری بانکی استفاده شده است.");
            }
            else blockers.Add(multiple);

            (number, year, version, voucher, date, issuer, issuerSystem, autoKey) =
                (receipt.Number, receipt.FiscalYearRef, receipt.Version, receipt.VoucherRef, receipt.Date, ReceiptIssuer, 2, "ReceiptAutoVoucher");
            if (action != DepositChequeAction.None)
            {
                if (cheque == null)
                    throw new SepidarTransferValidationException(multiple);
                var current = await ReadChequeStatusAsync(cheque, ct);
                if (action == DepositChequeAction.Spend)
                {
                    var items = await db.PaymentChequeOthers.AsNoTracking().Where(x => x.ReceiptChequeRef == cheque.ReceiptChequeId)
                        .Take(2).ToListAsync(ct);
                    if (items.Count != 1)
                        throw new SepidarTransferValidationException("اعلامیه پرداخت این چک در سپیدار قابل تطبیق نیست.");
                    var item = items[0];
                    payment = await db.PaymentHeaders.AsNoTracking().SingleAsync(x => x.PaymentHeaderId == item.PaymentHeaderRef, ct);
                    paymentItemId = item.PaymentChequeOtherId;
                    target = Payment;
                    targetId = payment.PaymentHeaderId;
                    if (payment.ItemType != 8 || payment.TotalAmount != cheque.Amount || payment.Rate != 1m ||
                        (payment.ReceiptHeaderRef.HasValue && payment.ReceiptHeaderRef != receipt.ReceiptHeaderId) ||
                        payment.Amount.GetValueOrDefault() != 0m || payment.Discount.GetValueOrDefault() != 0m ||
                        await db.PaymentChequeOthers.CountAsync(x => x.PaymentHeaderRef == targetId, ct) != 1)
                        blockers.Add(multiple);
                    if (cheque.State != ChequeSpent)
                        blockers.Add("آخرین وضعیت چک، خرج‌شده نیست.");
                    await Check(PaymentItem, paymentItemId.Value, dependent);
                    (number, year, version, voucher, date, issuer, issuerSystem, autoKey) =
                        (payment.Number, payment.FiscalYearRef, payment.Version, payment.VoucherRef, payment.Date, PaymentIssuer, 2, "PaymentAutoVoucher");
                    previousState = ChequeInCash;
                }
                else if (action is DepositChequeAction.Assign or DepositChequeAction.Collect)
                {
                    var assigning = action == DepositChequeAction.Assign;
                    var related = assigning ? current.Assignment : current.Collection;
                    if (related == null)
                        throw new SepidarTransferValidationException("عملیات بانکی در سپیدار پیدا نشد.");
                    var item = await db.ReceiptChequeBankingItems.AsNoTracking()
                        .SingleAsync(x => x.ReceiptChequeBankingItemId == related.ItemId, ct);
                    var header = await db.ReceiptChequeBankings.AsNoTracking()
                        .SingleAsync(x => x.ReceiptChequeBankingId == item.ReceiptChequeBankingRef, ct);
                    bankingItemId = item.ReceiptChequeBankingItemId;
                    target = Banking;
                    targetId = header.ReceiptChequeBankingId;
                    if (await db.ReceiptChequeBankingItems.CountAsync(x => x.ReceiptChequeBankingRef == targetId, ct) != 1)
                        blockers.Add(multiple);
                    if (assigning && current.Collection != null)
                        blockers.Add("ابتدا وصول چک را از سوابق حذف کنید.");
                    if (cheque.State != (assigning ? ChequeAssigned : ChequeCollected))
                        blockers.Add("فقط آخرین عملیات بانکی چک قابل حذف است.");
                    await Check(BankingItem, bankingItemId.Value, "این عملیات در گردش بانکی یا مغایرت‌گیری دیگری استفاده شده است.");
                    (number, year, version, voucher, date, issuer, issuerSystem, autoKey) =
                        (header.Number, header.FiscalYearRef, header.Version, header.VoucherRef, header.Date, BankingIssuer, 2, "ReceiptBankingAutoVoucher");
                    previousState = assigning ? ChequeInCash : ChequeAssigned;
                    if (edit)
                        blockers.Add("عملیات بانکی چک فقط قابل حذف است.");
                }
                else throw new SepidarTransferValidationException("نوع عملیات چک معتبر نیست.");

                latest = await db.ReceiptChequeHistory.AsNoTracking().Where(x => x.ReceiptChequeRef == cheque.ReceiptChequeId)
                    .OrderByDescending(x => x.ReceiptChequeHistoryId).FirstOrDefaultAsync(ct);
                var previous = latest?.ReceiptChequeHistoryRef is int previousId
                    ? await db.ReceiptChequeHistory.AsNoTracking().SingleOrDefaultAsync(x =>
                        x.ReceiptChequeHistoryId == previousId && x.ReceiptChequeRef == cheque.ReceiptChequeId, ct) : null;
                var operationHistory = db.ReceiptChequeHistory.AsNoTracking();
                var operationHistoryCount = bankingItemId.HasValue
                    ? await operationHistory.CountAsync(x => x.ReceiptChequeBankingItemRef == bankingItemId, ct)
                    : await operationHistory.CountAsync(x => x.PaymentChequeOtherRef == paymentItemId, ct);
                if (operationHistoryCount != 1 || latest == null || latest.State != cheque.State ||
                    latest.ReceiptChequeBankingItemRef != bankingItemId || latest.PaymentChequeOtherRef != paymentItemId ||
                    previous?.State != previousState ||
                    (action == DepositChequeAction.Collect && previous?.ReceiptChequeBankingItemRef != current.Assignment?.ItemId))
                    blockers.Add("زنجیره آخرین گردش چک در سپیدار قابل برگشت نیست.");
                else
                    await Check(History, latest.ReceiptChequeHistoryId, "گردش دیگری به آخرین وضعیت چک وابسته است.");
            }
            else if (cheque != null)
            {
                var status = await ReadChequeStatusAsync(cheque, ct);
                if (status.Payment != null)
                    blockers.Add("ابتدا اعلامیه پرداختِ خرج کردن چک را حذف کنید.");
                else if (status.Collection != null)
                    blockers.Add("ابتدا وصول، سپس واگذاری چک را حذف کنید.");
                else if (status.Assignment != null)
                    blockers.Add("ابتدا واگذاری چک به بانک را حذف کنید.");
                else if (cheque.State != ChequeInCash)
                    blockers.Add("چک در وضعیت نزد صندوق نیست.");
                var history = await db.ReceiptChequeHistory.AsNoTracking().Where(x => x.ReceiptChequeRef == cheque.ReceiptChequeId)
                    .Take(2).ToListAsync(ct);
                if (history.Count != 1 || history[0].State != ChequeInCash || history[0].Type != 11 ||
                    history[0].ReceiptChequeHistoryRef.HasValue)
                    blockers.Add("رسید دارای گردش‌های وابسته چک است.");
                await Check(Cheque, cheque.ReceiptChequeId, dependent);
            }
        }

        await Check(target, targetId, dependent);
        if (!await db.FiscalYears.AnyAsync(x => x.FiscalYearId == year && x.Status == 1, ct))
            blockers.Add("سال مالی سند در سپیدار باز نیست.");
        Voucher? old = null;
        if (voucher is int voucherId)
        {
            old = await db.Vouchers.AsNoTracking().SingleOrDefaultAsync(x => x.VoucherId == voucherId, ct);
            var ownerType = issuer.Split(',')[0];
            var items = db.VoucherItems.AsNoTracking().Where(x => x.VoucherRef == voucherId);
            if (old == null || old.State != 1 || old.Type != 2 || old.IsMerged ||
                old.IssuerSystem != issuerSystem || old.FiscalYearRef != year || !await items.AnyAsync(ct) ||
                await items.AnyAsync(x => x.IssuerEntityRef != targetId || x.IssuerEntityName == null ||
                    !(x.IssuerEntityName == ownerType || x.IssuerEntityName.StartsWith(ownerType + ",")), ct) ||
                await dependencies.HasDestinationDependenciesAsync(software, DepositDependencyTarget.Voucher, voucherId, ct, target, targetId))
                blockers.Add("سند حسابداری مرتبط، موقت و مختص این سند نیست.");
        }
        bool? auto = edit && blockers.Count == 0 ? await GetRequiredSepidarBooleanSettingAsync(autoKey, ct) : null;
        var token = GetPurchasePreviewToken(new {
            id, type, action, edit, targetId, version, number, date, voucher, VoucherVersion = old?.Version,
            receipt, draft, cheque, note, noteItem, latest, bankingItemId, paymentItemId, previousState, auto,
            blockers
        });
        return new(new(number, date, token, old?.Number, auto, blockers), targetId, year, target, old,
            receipt, draft, cheque, note, noteItem, payment, bankingItemId, paymentItemId, latest, previousState);
    }

    internal async Task<DepositMaintenanceResult> MaintainDepositAsync(int id, int type, DepositChequeAction action,
        bool edit, string token, int modifier, DepositInput? input, DateTime? due, int payeeId, int accountId, string? description,
        Guid software, IDepositFinancialRepository dependencies, CancellationToken ct)
    {
        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireSepidarInvoiceExportLockAsync(ct);
        var plan = await BuildDepositMaintenancePlanAsync(id, type, action, edit, software, dependencies, ct);
        if (plan.Impact.Blockers.Count > 0)
            throw new SepidarTransferValidationException(plan.Impact.Blockers[0]);
        if (plan.Impact.Token != token)
            throw new SepidarTransferValidationException("اطلاعات سند تغییر کرده است. فرم را دوباره باز کنید.");
        try
        {
            bool? issued = null;
            if (!edit)
                await DeleteDepositDestinationAsync(plan, ct);
            else if (plan.Payment != null)
                issued = await EditDepositChequePaymentAsync(plan, payeeId, accountId, description, modifier, ct);
            else if (input != null)
                issued = await EditDepositDocumentAsync(plan, input, due, modifier, ct);
            else throw new SepidarTransferValidationException("اطلاعات ویرایش سند تکمیل نشده است.");

            if (plan.OldVoucher is { } old)
            {
                await DeleteDepositNotesAsync("SG.Accounting.VoucherManagement.Common.DsVoucher", old.VoucherId, ct);
                await sepidarContext.Vouchers.Where(x => x.VoucherId == old.VoucherId).ExecuteDeleteAsync(ct);
            }
            await transaction.CommitAsync(ct);
            return new(!edit, edit, issued);
        }
        catch (SqlException error) when (error.Number == 547)
        {
            throw new SepidarTransferValidationException("سند وابسته‌ای در سپیدار مانع عملیات است. تغییرات اعمال نشد.");
        }
    }

    private async Task DeleteDepositDestinationAsync(DepositMaintenancePlan plan, CancellationToken ct)
    {
        var db = sepidarContext;
        var type = plan.Target switch { Receipt => ReceiptIssuer, Note => NoteIssuer, Payment => PaymentIssuer, _ => BankingIssuer };
        await DeleteDepositNotesAsync(type.Split(',')[0], plan.TargetId, ct);
        if (plan.LastHistory != null && plan.Cheque != null && plan.PreviousChequeState is int state)
        {
            var latestId = plan.LastHistory.ReceiptChequeHistoryId;
            await db.ReceiptChequeHistory.Where(x => x.ReceiptChequeHistoryId == latestId).ExecuteDeleteAsync(ct);
            var chequeId = plan.Cheque.ReceiptChequeId;
            await db.ReceiptCheques.Where(x => x.ReceiptChequeId == chequeId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.State, state).SetProperty(x => x.Version, x => x.Version + 1), ct);
        }
        switch (plan.Target)
        {
            case Receipt: await db.ReceiptHeaders.Where(x => x.ReceiptHeaderId == plan.TargetId).ExecuteDeleteAsync(ct); break;
            case Note: await db.DebitCreditNotes.Where(x => x.DebitCreditNoteID == plan.TargetId).ExecuteDeleteAsync(ct); break;
            case Payment: await db.PaymentHeaders.Where(x => x.PaymentHeaderId == plan.TargetId).ExecuteDeleteAsync(ct); break;
            case Banking: await db.ReceiptChequeBankings.Where(x => x.ReceiptChequeBankingId == plan.TargetId).ExecuteDeleteAsync(ct); break;
        }
    }

    private async Task DeleteDepositNotesAsync(string type, int id, CancellationToken ct)
    {
        await sepidarContext.ExtraData.Where(x => x.EntityRef == id && x.EntityTypeName == type).ExecuteDeleteAsync(ct);
        await sepidarContext.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM FMK.Note WHERE EntityRef={id} AND EntityType={type}", ct);
    }
}
