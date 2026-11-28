using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Accountancies.Repositories;
using CRM.Framework;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public partial class SepidarService
{
    internal sealed record DepositSyncSource(int Type, decimal Amount, DateTime Date, DateTime? Due,
        bool Collected, bool Transferred, DateTime? CollectionDate, int CustomerId, int BankAccountId, int PayeeId);
    internal sealed record DepositSyncDifference(string Title, string? Current, string? Proposed);
    internal sealed record DepositSyncSummary(IReadOnlyList<DepositSyncDifference> Changes, string? ReceiptRestriction,
        string? AssignmentRestriction, string? CollectionRestriction, string? PaymentRestriction, bool PaymentChanged);
    internal sealed record DepositSyncPreview(DepositMaintenanceImpact Impact, IReadOnlyList<DepositSyncDifference> Changes);

    private sealed record DepositSyncValues(decimal Amount, DateTime Date, DateTime? Due,
        int? CustomerDl, int BankId, int? PayeeDl, int? CurrentPayeeDl, int? CreditType,
        decimal CurrentAmount, DateTime CurrentDate, DateTime? CurrentDue, int? CurrentCustomerDl, int? CurrentBank);

    private async Task<int?> FindSyncPartyDetailAsync(int id, CancellationToken ct) => id <= 0 ? null
        : await sepidarContext.Parties.AsNoTracking().Where(x => x.PartyId == id && x.Dl.IsActive)
            .Select(x => (int?)x.DLRef).SingleOrDefaultAsync(ct);

    private async Task<DepositSyncValues> ReadDepositSyncValuesAsync(int id, DepositSyncSource source, CancellationToken ct)
    {
        var db = sepidarContext;
        var mappedCustomer = await FindSyncPartyDetailAsync(source.CustomerId, ct);
        if (source.Type == 4)
        {
            var note = await db.DebitCreditNotes.AsNoTracking().SingleOrDefaultAsync(x => x.DebitCreditNoteID == id, ct)
                ?? throw new SepidarTransferValidationException("اعلامیه بدهکار/بستانکار در مقصد پیدا نشد.");
            var credit = await db.DebitCreditNoteItems.AsNoTracking().Where(x => x.DebitCreditNoteRef == id)
                .OrderBy(x => x.RowID).Select(x => new { x.CreditType, x.CreditDLRef }).FirstOrDefaultAsync(ct);
            return new(source.Amount, source.Date, null, credit?.CreditDLRef != null &&
                credit.CreditType == (int)DepositPartyType.Customer ? mappedCustomer : null,
                0, null, null, credit?.CreditType, note.SumAmount, note.Date, null, credit?.CreditDLRef, null);
        }
        var receipt = await db.ReceiptHeaders.AsNoTracking().SingleOrDefaultAsync(x => x.ReceiptHeaderId == id, ct)
            ?? throw new SepidarTransferValidationException("رسید دریافت در مقصد پیدا نشد.");
        int? bank = null;
        DateTime? due = null;
        int? currentPayee = null;
        if (source.Type == 1)
            bank = await db.ReceiptDrafts.AsNoTracking().Where(x => x.ReceiptHeaderRef == id)
                .OrderBy(x => x.ReceiptDraftId).Select(x => x.BankAccountRef).FirstOrDefaultAsync(ct);
        else if (source.Type == 2)
        {
            var cheque = await db.ReceiptCheques.AsNoTracking().Where(x => x.ReceiptHeaderRef == id)
                .OrderBy(x => x.ReceiptChequeId).FirstOrDefaultAsync(ct);
            due = cheque?.Date;
            if (cheque != null && source.Transferred)
                currentPayee = await (from item in db.PaymentChequeOthers.AsNoTracking()
                    join payment in db.PaymentHeaders.AsNoTracking() on item.PaymentHeaderRef equals payment.PaymentHeaderId
                    where item.ReceiptChequeRef == cheque.ReceiptChequeId
                    orderby payment.PaymentHeaderId descending
                    select (int?)payment.DlRef).FirstOrDefaultAsync(ct);
        }
        return new(source.Type == 2 && source.Collected ? receipt.TotalAmount : source.Amount, source.Date,
            source.Type == 2 ? source.Due ?? due : null, mappedCustomer, source.Type == 1 ? source.BankAccountId : 0,
            currentPayee.HasValue ? await FindSyncPartyDetailAsync(source.PayeeId, ct) : null, currentPayee, null,
            receipt.TotalAmount, receipt.Date, due, receipt.DlRef, bank);
    }

    private async Task<IReadOnlyList<DepositSyncDifference>> GetDepositSyncDifferencesAsync(DepositSyncValues values, bool payment, CancellationToken ct)
    {
        var changes = new List<DepositSyncDifference>();
        async Task AddPartyAsync(string title, int? oldId, int? newId)
        {
            if (!newId.HasValue || oldId == newId) return;
            var labels = await sepidarContext.DLs.AsNoTracking().Where(x => x.DLId == oldId || x.DLId == newId)
                .Select(x => new { x.DLId, Label = x.Code + " - " + x.Title })
                .ToDictionaryAsync(x => x.DLId, x => x.Label, ct);
            changes.Add(new(title, oldId.HasValue ? labels.GetValueOrDefault(oldId.Value) : null, labels.GetValueOrDefault(newId.Value)));
        }
        if (payment)
        {
            await AddPartyAsync("طرف مقابل اعلامیه پرداخت", values.CurrentPayeeDl, values.PayeeDl);
            return changes;
        }
        if (values.Amount != values.CurrentAmount)
            changes.Add(new("مبلغ (ریال)", values.CurrentAmount.ToString("#,0.####"), values.Amount.ToString("#,0.####")));
        if (values.Date.Date != values.CurrentDate.Date)
            changes.Add(new("تاریخ سند", BaseServices.GetDatePersian(values.CurrentDate), BaseServices.GetDatePersian(values.Date)));
        if (values.Due?.Date != values.CurrentDue?.Date)
            changes.Add(new("سررسید چک", BaseServices.GetDatePersian(values.CurrentDue), BaseServices.GetDatePersian(values.Due)));
        await AddPartyAsync(values.CreditType == null ? "مشتری" : "تفصیلی بستانکار", values.CurrentCustomerDl, values.CustomerDl);
        if (values.BankId > 0 && values.BankId != values.CurrentBank)
            changes.Add(new("حساب بانکی مقصد", values.CurrentBank is int oldBank ? await GetChequeAssignmentBankTitleAsync(oldBank, ct) : null,
                await GetChequeAssignmentBankTitleAsync(values.BankId, ct)));
        return changes;
    }

    internal async Task<DepositSyncSummary> GetDepositSyncSummaryAsync(int id, DepositSyncSource source, CancellationToken ct)
    {
        var values = await ReadDepositSyncValuesAsync(id, source, ct);
        var changes = await GetDepositSyncDifferencesAsync(values, false, ct);
        if (source.Type != 2)
            return new(changes, null, null, null, null, false);
        var cheque = await FindDepositChequeStatusAsync(id, ct);
        if (cheque == null)
            return new(changes, "چک این رسید در مقصد قابل تطبیق نیست.", null, null, null, false);
        var assignment = cheque.Assignment;
        var collection = cheque.Collection;
        var payment = cheque.Payment;
        const string shared = "این سند شامل چند چک است و باید در سپیدار بررسی شود.";
        var paymentChanged = values.PayeeDl.HasValue && values.CurrentPayeeDl != values.PayeeDl;
        var receiptRestriction = changes.Count == 0 ? null : payment != null ? "ابتدا اعلامیه پرداختِ خرج کردن چک را حذف کنید."
            : collection != null ? "ابتدا وصول و سپس واگذاری چک را حذف کنید."
            : assignment != null ? "ابتدا واگذاری چک به بانک را حذف کنید." : null;
        var paymentRestriction = payment == null ? null : !source.Transferred
            ? "انتقال چک در ERP فعال نیست. اعلامیهٔ پرداخت را از سوابق حذف کنید."
            : changes.Count > 0 ? "برای همگام‌سازی رسید دریافت، ابتدا این اعلامیهٔ پرداخت را حذف کنید." : null;
        if ((paymentRestriction != null || paymentChanged) && payment?.ItemCount > 1)
            paymentRestriction = shared;
        var assignmentMismatch = assignment != null && (source.Transferred || !source.Collected ||
            (source.BankAccountId > 0 && assignment.BankId != source.BankAccountId) ||
            (source.CollectionDate.HasValue && assignment.Date.Date > source.CollectionDate.Value.Date));
        var assignmentRestriction = assignmentMismatch
            ? collection != null ? "ابتدا وصول و سپس واگذاری چک را حذف کنید." : "واگذاری با گردش ERP مغایرت دارد. ابتدا واگذاری را حذف کنید."
            : null;
        var amountMismatch = source.Collected && !source.Transferred && cheque.Amount != source.Amount
            ? $"مبلغ اولیه چک ({cheque.Amount:#,0.####} ریال) با مبلغ وصول ERP ({source.Amount:#,0.####} ریال) برابر نیست."
            : null;
        var collectionRestriction = collection == null ? null : amountMismatch != null
            ? amountMismatch + " ابتدا وصول ثبت‌شده در سپیدار را حذف کنید."
            : assignmentMismatch || !source.Collected || collection.Date.Date != source.CollectionDate
                ? "وصول ثبت‌شده با ERP مغایرت دارد. ابتدا وصول را حذف کنید." : null;
        if (assignmentRestriction != null && assignment?.ItemCount > 1) assignmentRestriction = shared;
        if (collectionRestriction != null && collection?.ItemCount > 1) collectionRestriction = shared;
        if (cheque.State is not (ChequeInCash or ChequeAssigned or ChequeCollected or ChequeSpent))
            receiptRestriction ??= "وضعیت فعلی چک باید در نرم‌افزار سپیدار بررسی شود.";
        if (collection == null && amountMismatch != null)
            receiptRestriction ??= amountMismatch + " پیش از ثبت وصول، اختلاف را بررسی کنید.";
        return new(changes, receiptRestriction, assignmentRestriction, collectionRestriction, paymentRestriction, paymentChanged);
    }

    internal async Task<DepositSyncPreview> GetDepositSyncPreviewAsync(int id, DepositSyncSource source,
        DepositChequeAction action, Guid software, IDepositFinancialRepository dependencies, CancellationToken ct)
    {
        var (preview, _, _) = await BuildDepositSyncPlanAsync(id, source, action, software, dependencies, ct);
        return preview;
    }

    private async Task<(DepositSyncPreview Preview, DepositMaintenancePlan Plan, DepositSyncValues Values)> BuildDepositSyncPlanAsync(
        int id, DepositSyncSource source, DepositChequeAction action, Guid software, IDepositFinancialRepository dependencies, CancellationToken ct)
    {
        var plan = await BuildDepositMaintenancePlanAsync(id, source.Type, action, true, software, dependencies, ct);
        var values = await ReadDepositSyncValuesAsync(id, source, ct);
        var changes = await GetDepositSyncDifferencesAsync(values, action == DepositChequeAction.Spend, ct);
        var blockers = plan.Impact.Blockers.ToList();
        if (changes.Count == 0)
            blockers.Add("سند با اطلاعات ERP همگام است.");
        if (action == DepositChequeAction.Spend && !source.Transferred)
            blockers.Add("انتقال چک در ERP فعال نیست. اعلامیهٔ پرداخت را از سوابق حذف کنید.");
        if (action == DepositChequeAction.Spend && (await GetDepositSyncDifferencesAsync(values, false, ct)).Count > 0)
            blockers.Add("برای همگام‌سازی رسید دریافت، ابتدا اعلامیهٔ پرداخت را حذف کنید.");
        if (action == DepositChequeAction.None && !await sepidarContext.FiscalYears.AnyAsync(x =>
            x.FiscalYearId == plan.Year && x.Status == 1 && x.StartDate <= values.Date && x.EndDate >= values.Date, ct))
            blockers.Add("تاریخ جدید باید در همان سال مالی باز سند سپیدار باشد.");
        var token = GetPurchasePreviewToken(new { plan.Impact.Token, plan.Payment, source, values, changes, blockers });
        return (new(plan.Impact with { Token = token, Blockers = blockers }, changes), plan, values);
    }

    internal async Task<DepositMaintenanceResult> SynchronizeDepositAsync(int id, DepositSyncSource source, DepositChequeAction action,
        string token, int modifier, Guid software, IDepositFinancialRepository dependencies, CancellationToken ct)
    {
        await using var transaction = await sepidarContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await AcquireSepidarInvoiceExportLockAsync(ct);
        var (preview, plan, values) = await BuildDepositSyncPlanAsync(id, source, action, software, dependencies, ct);
        if (preview.Impact.Blockers.Count > 0)
            throw new SepidarTransferValidationException(preview.Impact.Blockers[0]);
        if (preview.Impact.Token != token)
            throw new SepidarTransferValidationException("اطلاعات ERP یا سپیدار تغییر کرده است. پیش‌نمایش را دوباره باز کنید.");
        try
        {
            bool issued;
            if (action == DepositChequeAction.Spend)
            {
                var payment = plan.Payment!;
                issued = await EditDepositChequePaymentAsync(plan, source.PayeeId, payment.AccountSlRef ?? 0,
                    payment.Description, modifier, ct);
            }
            else
            {
                var input = await ReadDepositEditInputAsync(source.Type, plan, ct);
                if (values.CustomerDl.HasValue && values.CurrentCustomerDl != values.CustomerDl)
                    input = source.Type == 4 ? input with { CreditSide = input.CreditSide with { DetailId = source.CustomerId } }
                        : input with { CustomerId = source.CustomerId };
                if (values.BankId > 0)
                    input = input with { BankAccountId = values.BankId };
                issued = await EditDepositDocumentAsync(plan, input, values.Due, modifier, ct, values.Amount, values.Date);
            }
            if (plan.OldVoucher is { } old)
            {
                await DeleteDepositNotesAsync("SG.Accounting.VoucherManagement.Common.DsVoucher", old.VoucherId, ct);
                await sepidarContext.Vouchers.Where(x => x.VoucherId == old.VoucherId).ExecuteDeleteAsync(ct);
            }
            await transaction.CommitAsync(ct);
            return new(false, true, issued);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new SepidarTransferValidationException("سند وابسته‌ای در سپیدار مانع عملیات است. تغییرات اعمال نشد.");
        }
    }
}
