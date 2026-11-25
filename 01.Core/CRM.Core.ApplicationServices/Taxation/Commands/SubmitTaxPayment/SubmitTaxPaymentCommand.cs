using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;
using System.Data.Common;
using System.Security.Cryptography;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxPayment;

public sealed record SubmitTaxPaymentCommand(Guid PaymentId) : ICommand<TaxPaymentState>;

public sealed class SubmitTaxPaymentCommandHandler(ITaxPaymentRepository repository, ITaxSubmissionRepository invoices,
    ITaxPaymentTransport transport, TimeProvider clock) : ICommandHandler<SubmitTaxPaymentCommand, TaxPaymentState>
{
    public async Task<TaxPaymentState> HandleAsync(SubmitTaxPaymentCommand request, CancellationToken ct)
    {
        var state = await repository.GetStateAsync(request.PaymentId, ct);
        if (state.Status != TaxPaymentStatus.Pending)
            return state;
        var payment = await repository.ReadPendingAsync(request.PaymentId, ct);
        if (payment is null)
            return await repository.GetStateAsync(request.PaymentId, ct);
        var invoice = await invoices.FindAsync(payment.InvoiceDispatchId, ct);
        if (invoice is not { Status: TaxDispatchStatus.Accepted, Protocol: TaxAuthorityProtocol.V2 })
            throw new ArgumentException("صورتحساب مرجع برای ثبت پرداخت پذیرفته‌شده نیست.");

        var attempt = Guid.NewGuid();
        using var prepared = await transport.PreparePaymentAsync(invoice, attempt, payment.Payload, ct);
        ct.ThrowIfCancellationRequested();
        if (!await repository.TryStartAsync(payment.Id, attempt, clock.GetUtcNow(), ct))
            return await repository.GetStateAsync(payment.Id, ct);

        try
        {
            using var send = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var reply = await transport.SubmitPaymentAsync(prepared, send.Token);
            using var save = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await repository.RecordOutcomeAsync(payment.Id, attempt, reply.Status, reply.Evidence, clock.GetUtcNow(), save.Token);
        }
        catch (Exception ex) when (ex is TaxTransportException or OperationCanceledException or DbException or TimeoutException or CryptographicException)
        {
            using var save = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await repository.RecordOutcomeAsync(payment.Id, attempt, TaxPaymentStatus.Unknown, null, clock.GetUtcNow(), save.Token);
        }

        using var read = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return await repository.GetStateAsync(payment.Id, read.Token);
    }
}
