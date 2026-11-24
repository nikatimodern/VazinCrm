using CRM.Core.Domain.Taxation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CRM.Infrastructures.Sql.Taxation;

public sealed class TaxPaymentRepository(CRMContextDb db, ITaxPreviewProtector protection) : ITaxPaymentRepository
{
    public async Task<Guid> EnqueueAsync(Guid invoiceDispatchId, Guid accountancyId, string actorId,
        Func<TaxPaymentSource, TaxPaymentRegistration> prepare, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await TaxationRepository.LockAsync(db, $"tax-payment-invoice:{invoiceDispatchId:D}", ct);
        await TaxationRepository.LockAsync(db, $"tax-payment-accountancy:{accountancyId:D}", ct);
        var existing = await db.TaxPaymentDispatches.AsNoTracking().SingleOrDefaultAsync(x => x.AccountancyId == accountancyId, ct);
        if (existing is not null)
        {
            if (existing.InvoiceDispatchId != invoiceDispatchId)
                throw new ArgumentException("دریافت قبلاً به سند مالیاتی دیگری متصل شده است.");
            await transaction.CommitAsync(ct);
            return existing.Id;
        }

        var invoice = await db.TaxDispatches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == invoiceDispatchId, ct)
            ?? throw new KeyNotFoundException();
        var receipt = await db.Accountancies.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountancyId, ct)
            ?? throw new KeyNotFoundException();
        if (invoice.Status != TaxDispatchStatus.Accepted || string.IsNullOrEmpty(invoice.ProtectedSubmission))
            throw new ArgumentException("صورتحساب پذیرفته‌شده با اطلاعات ارسال لازم است.");

        var total = await db.TaxPaymentDispatches.Where(x => x.InvoiceDispatchId == invoiceDispatchId &&
            x.Status != TaxPaymentStatus.Rejected).SumAsync(x => x.Amount, ct);
        var request = prepare(new(invoice, receipt, protection.Unprotect(invoice.ProtectedSubmission), total));
        var payment = new TaxPaymentDispatch
        {
            Id = Guid.NewGuid(), InvoiceDispatchId = invoiceDispatchId, AccountancyId = accountancyId,
            Amount = request.Amount, ReferenceHash = request.ReferenceHash,
            ProtectedRequest = protection.Protect(request.Content), CreatedBy = actorId,
            CreatedAt = DateTimeOffset.UtcNow, Status = TaxPaymentStatus.Pending
        };
        db.TaxPaymentDispatches.Add(payment);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new ArgumentException("این دریافت یا شماره مرجع پرداخت قبلاً ثبت شده است.");
        }
        return payment.Id;
    }

    public async Task<PendingTaxPayment> ReadPendingAsync(Guid id, CancellationToken ct)
    {
        var request = await db.TaxPaymentDispatches.Where(x => x.Id == id && x.Status == TaxPaymentStatus.Pending)
            .Select(x => new { x.InvoiceDispatchId, x.ProtectedRequest }).SingleOrDefaultAsync(ct);
        return request is null ? null : new(id, request.InvoiceDispatchId, protection.Unprotect(request.ProtectedRequest));
    }

    public async Task<bool> TryStartAsync(Guid id, Guid attempt, DateTimeOffset started, CancellationToken ct) =>
        await db.TaxPaymentDispatches.Where(x => x.Id == id && x.Status == TaxPaymentStatus.Pending &&
            db.TaxDispatches.Any(invoice => invoice.Id == x.InvoiceDispatchId && invoice.Status == TaxDispatchStatus.Accepted))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, TaxPaymentStatus.InFlight)
                .SetProperty(x => x.AttemptId, attempt).SetProperty(x => x.AttemptStartedAt, started), ct) == 1;

    public async Task RecordOutcomeAsync(Guid id, Guid attempt, TaxPaymentStatus status, string evidence, DateTimeOffset now,
        CancellationToken ct)
    {
        if (status is not (TaxPaymentStatus.Accepted or TaxPaymentStatus.Rejected or TaxPaymentStatus.Unknown))
            throw new ArgumentOutOfRangeException(nameof(status));
        var protectedResponse = evidence is null ? null : protection.Protect(evidence);
        await db.TaxPaymentDispatches.Where(x => x.Id == id && x.AttemptId == attempt && x.Status == TaxPaymentStatus.InFlight)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, status)
                .SetProperty(x => x.ProtectedResponse, protectedResponse)
                .SetProperty(x => x.ResolvedAt, status == TaxPaymentStatus.Unknown ? (DateTimeOffset?)null : now), ct);
    }

    public async Task<TaxPaymentState> GetStateAsync(Guid id, CancellationToken ct)
    {
        var abandonedBefore = DateTimeOffset.UtcNow.AddMinutes(-2);
        return await db.TaxPaymentDispatches.Where(x => x.Id == id)
            .Select(x => new TaxPaymentState(x.Id, x.InvoiceDispatchId, x.AccountancyId, x.Amount,
                x.Status == TaxPaymentStatus.InFlight && x.AttemptStartedAt <= abandonedBefore ? TaxPaymentStatus.Unknown : x.Status,
                x.AttemptId, x.CreatedAt, x.AttemptStartedAt, x.ResolvedAt))
            .SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException();
    }
}
