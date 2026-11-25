using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.Domain.Sepidar.Deposits.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Services.SepidarServices;

public partial class SepidarService
{
    private async Task<int> ReadDepositPartyIdAsync(int dl, CancellationToken ct) =>
        await sepidarContext.Parties.AsNoTracking().Where(x => x.DLRef == dl)
            .OrderBy(x => x.PartyId).Select(x => (int?)x.PartyId).FirstOrDefaultAsync(ct) ?? 0;

    private async Task<DepositInput> ReadDepositEditInputAsync(int type, DepositMaintenancePlan plan, CancellationToken ct)
    {
        async Task<DepositNoteSide> Side(int? side, int account, int? dl)
        {
            var partyType = (DepositPartyType)(side ?? 0);
            var detail = dl is not int id ? 0 : partyType == DepositPartyType.Other ? id : await ReadDepositPartyIdAsync(id, ct);
            return new(partyType, account, detail);
        }
        var receipt = plan.Receipt;
        var cheque = plan.Cheque;
        var noteItem = plan.NoteItem;
        var credit = noteItem == null
            ? new DepositNoteSide(DepositPartyType.Customer, receipt?.AccountSlRef ?? 0, 0)
            : await Side(noteItem.CreditType, noteItem.CreditSLRef, noteItem.CreditDLRef);
        var debit = noteItem == null
            ? new DepositNoteSide(DepositPartyType.Other, 0, 0)
            : await Side(noteItem.DebitType, noteItem.DebitSLRef, noteItem.DebitDLRef);
        return new(type, receipt == null ? 0 : await ReadDepositPartyIdAsync(receipt.DlRef, ct),
            plan.Draft?.BankAccountRef ?? 0, receipt?.CashRef ?? 0, cheque?.BankRef ?? 0,
            cheque?.Number ?? plan.Draft?.Number ?? "", cheque?.SayadCode ?? "", cheque?.AccountNo ?? "",
            receipt?.Description ?? noteItem?.Description ?? "", cheque?.SecondNumber ?? "", cheque?.ChequeOwner,
            cheque?.BranchCode ?? "", cheque?.BranchTitle, plan.Note?.Description, debit, credit);
    }

    private async Task<bool> EditDepositDocumentAsync(DepositMaintenancePlan plan, DepositInput input, DateTime? due,
        int modifier, CancellationToken ct)
    {
        var db = sepidarContext;
        var amount = plan.Note?.SumAmount ?? plan.Receipt!.TotalAmount;
        var prepared = await PrepareDepositAsync(input, amount, plan.Impact.Date, due, true, ct, plan.Cheque?.ReceiptChequeId);
        if (prepared.Year != plan.Year || prepared.AutoVoucher != plan.Impact.AutoVoucher ||
            prepared.Currency != (plan.Receipt?.CurrencyRef ?? plan.Note!.CurrencyRef))
            throw new SepidarTransferValidationException("سال مالی، ارز یا تنظیم صدور سند تغییر کرده است.");
        if (plan.Draft is { } transfer && await db.ReceiptDrafts.AnyAsync(x => x.ReceiptDraftId != transfer.ReceiptDraftId &&
            x.BankAccountRef == input.BankAccountId && x.Number == input.Reference &&
            x.Date == prepared.Date && x.Amount == prepared.Amount, ct))
            throw new SepidarTransferValidationException("حواله با همین شماره پیگیری، حساب، تاریخ و مبلغ قبلاً در سپیدار ثبت شده است.");

        var voucher = prepared.AutoVoucher
            ? await AddDepositVoucherAsync(plan.TargetId, plan.Impact.Number, modifier, prepared, ct) : (int?)null;
        var now = DateTime.Now;
        if (plan.Note is { } note && plan.NoteItem is { } row)
        {
            db.Attach(note);
            db.Attach(row);
            note.Description = note.Description_En = input.NoteDescription ?? "";
            note.VoucherRef = voucher;
            note.LastModifier = modifier;
            note.LastModificationDate = now;
            note.Version++;
            row.DebitSLRef = prepared.DebitAccount;
            row.CreditSLRef = prepared.CreditAccount;
            row.DebitDLRef = prepared.DebitDl;
            row.CreditDLRef = prepared.CreditDl;
            row.DebitType = (int)input.DebitSide.Type;
            row.CreditType = (int)input.CreditSide.Type;
            row.Description = row.Description_En = input.Description ?? "";
            row.Version++;
        }
        else if (plan.Receipt is { } receipt)
        {
            db.Attach(receipt);
            receipt.VoucherRef = voucher;
            receipt.State = voucher.HasValue ? 2 : 1;
            receipt.AccountSlRef = prepared.CreditAccount;
            receipt.DlRef = prepared.CreditDl!.Value;
            receipt.CashRef = input.CashId;
            receipt.Description = receipt.Description_En = input.Description;
            receipt.LastModifier = modifier;
            receipt.LastModificationDate = now;
            receipt.Version++;
            if (plan.Draft is { } transferRow)
            {
                db.Attach(transferRow);
                transferRow.BankAccountRef = input.BankAccountId;
                transferRow.Number = input.Reference;
                transferRow.Description = transferRow.Description_En = input.Description;
                transferRow.Version++;
            }
            if (plan.Cheque is { } cheque)
            {
                db.Attach(cheque);
                cheque.Number = input.Reference;
                cheque.Date = prepared.Due;
                cheque.BankRef = input.BankId;
                cheque.CashRef = input.CashId;
                cheque.DlRef = receipt.DlRef;
                cheque.AccountNo = input.AccountNo;
                cheque.SayadCode = EmptyToNull(input.Sayad);
                cheque.SecondNumber = EmptyToNull(input.SecondNumber);
                cheque.ChequeOwner = EmptyToNull(input.ChequeOwner);
                cheque.BranchCode = EmptyToNull(input.BranchCode);
                cheque.BranchTitle = EmptyToNull(input.BranchTitle);
                cheque.Description = cheque.Description_En = input.Description;
                cheque.Version++;
            }
        }
        await db.SaveChangesAsync(ct);
        return voucher.HasValue;
    }

    private async Task<bool> EditDepositChequePaymentAsync(DepositMaintenancePlan plan, int payeeId, int accountId,
        string? description, int modifier, CancellationToken ct)
    {
        var payment = plan.Payment!;
        var cheque = plan.Cheque!;
        if (description?.Length > 250)
            throw new SepidarTransferValidationException("شرح اعلامیه پرداخت حداکثر ۲۵۰ کاراکتر است.");
        var edit = new ChequeOperationInput(DepositChequeAction.Spend, payment.Date, 0,
            payment.Type, payeeId, accountId, payment.CashRef ?? 0, description?.Trim() ?? "");
        var prepared = await BuildChequeOperationAsync(cheque, edit, true, false, cheque.Amount, ct, editingPayment: true);
        if (prepared.Year != plan.Year || prepared.AutoVoucher != plan.Impact.AutoVoucher)
            throw new SepidarTransferValidationException("سال مالی یا تنظیم صدور سند تغییر کرده است.");

        var voucher = await PostChequePaymentVoucherAsync(cheque, prepared, payment.PaymentHeaderId, payment.Number, modifier, ct);
        sepidarContext.Attach(payment);
        payment.DlRef = prepared.DebitDl;
        payment.AccountSlRef = prepared.DebitAccount;
        payment.Description = payment.Description_En = edit.Description;
        payment.VoucherRef = voucher;
        payment.State = voucher.HasValue ? 2 : 1;
        payment.LastModifier = modifier;
        payment.LastModificationDate = DateTime.Now;
        payment.Version++;
        await sepidarContext.SaveChangesAsync(ct);
        return voucher.HasValue;
    }
}
