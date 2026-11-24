using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxInvoice;

public sealed record SubmitTaxInvoiceCommand(Guid DispatchId, TaxFilingDetails Filing) : ICommand<TaxDispatchState>;

public sealed class SubmitTaxInvoiceCommandHandler(ITaxSubmissionRepository repository, ITaxDispatchQueue queue,
    ITaxSigningBindings bindings, ITaxAuthorityClient client, TimeProvider clock)
    : ICommandHandler<SubmitTaxInvoiceCommand, TaxDispatchState>
{
    public async Task<TaxDispatchState> HandleAsync(SubmitTaxInvoiceCommand request, CancellationToken ct)
    {
        var state = await queue.GetStateAsync(request.DispatchId, ct) ?? throw new KeyNotFoundException();
        if (state.Status != TaxDispatchStatus.Pending)
            return state;

        var submission = await repository.FindAsync(request.DispatchId, ct);
        if (submission is null)
        {
            var draft = await repository.ReadDraftAsync(request.DispatchId, ct);
            var profile = bindings.Get(draft.MemoryId);
            var preview = JsonSerializer.Deserialize<InvoicePreview>(draft.Snapshot);
            if (preview is null || preview.FiscalId != draft.FiscalId || preview.MemoryId != draft.MemoryId)
                throw new CryptographicException("Frozen tax snapshot does not match the dispatch.");
            if (request.Filing.IssuedAt > clock.GetUtcNow())
                throw new ArgumentException("زمان صدور صورتحساب نمی‌تواند در آینده باشد.");
            var (taxId, payload) = TaxInvoicePayload.Build(preview, profile, request.Filing);
            await repository.PinAsync(request.DispatchId, profile, request.Filing.Serial, taxId, payload, ct);
            submission = await repository.FindAsync(request.DispatchId, ct);
        }

        if (submission.Status != TaxDispatchStatus.Pending)
            return new(submission.DispatchId, submission.AttemptId, submission.Status, submission.TaxId, submission.ReferenceNumber);

        var attempt = Guid.NewGuid();
        // Decryption, validation, certificate access and nonce requests run before any send attempt is recorded.
        using var prepared = await client.PrepareAsync(submission, attempt, ct);
        ct.ThrowIfCancellationRequested();
        if (!await repository.TryStartAsync(request.DispatchId, attempt, clock.GetUtcNow(), ct))
            return await queue.GetStateAsync(request.DispatchId, ct);

        TaxAcknowledgement acknowledgement;
        try
        {
            // Operator disconnects must not cancel a submission once its attempt is durable.
            using var send = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            acknowledgement = await client.SendAsync(prepared, send.Token);
        }
        catch (Exception ex) when (ex is TaxTransportException or OperationCanceledException)
        {
            return await RecoverByInquiryAsync(request.DispatchId, attempt);
        }

        // Preserve the attempt even when the response cannot be persisted.
        try
        {
            using var save = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await repository.RecordAcknowledgementAsync(request.DispatchId, attempt, acknowledgement.ReferenceNumber,
                acknowledgement.Evidence, save.Token);
        }
        catch (Exception ex) when (ex is DbException or TimeoutException or OperationCanceledException or CryptographicException)
        {
            return await RecoverByInquiryAsync(request.DispatchId, attempt);
        }

        using var read = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return await queue.GetStateAsync(request.DispatchId, read.Token);
    }

    private async Task<TaxDispatchState> RecoverByInquiryAsync(Guid id, Guid attempt)
    {
        // If SQL is unavailable, the committed reservation expires into Unknown.
        using var write = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await queue.MarkSubmissionUnknownAsync(id, attempt, write.Token);
        return await queue.GetStateAsync(id, write.Token);
    }
}
