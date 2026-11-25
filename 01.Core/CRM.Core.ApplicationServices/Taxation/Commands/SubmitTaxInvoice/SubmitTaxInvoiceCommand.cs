using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Taxation.Dtos;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.Core.ApplicationServices.Taxation.Commands.SubmitTaxInvoice;

public sealed record SubmitTaxInvoiceCommand(Guid DispatchId, TaxFilingDetails? Filing) : ICommand<TaxDispatchState>;

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
            var filing = request.Filing ?? throw new ArgumentException("اطلاعات صدور صورتحساب اصلی لازم است.");
            var draft = await repository.ReadDraftAsync(request.DispatchId, ct);
            var profile = await bindings.GetAsync(draft.MemoryId, ct);
            var preview = JsonSerializer.Deserialize<InvoicePreview>(draft.Snapshot);
            if (preview is null || preview.FiscalId != draft.FiscalId || preview.MemoryId != draft.MemoryId)
                throw new CryptographicException("Frozen tax snapshot does not match the dispatch.");
            if (filing.IssuedAt > clock.GetUtcNow())
                throw new ArgumentException("زمان صدور صورتحساب نمی‌تواند در آینده باشد.");
            if (profile.Protocol == TaxAuthorityProtocol.V1 &&
                (preview.Pattern is not (null or TaxInvoicePattern.DomesticSale) || preview.InvoiceType is not (null or 1)))
                throw new ArgumentException("نسخه ۱ فقط برای فاکتور اصلی فروش داخلی نوع اول فعال است.");

            var (taxId, payload) = TaxInvoicePayload.Build(preview, profile, filing);
            await repository.PinAsync(request.DispatchId, profile, filing.Serial, taxId, payload, ct);
            submission = await repository.FindAsync(request.DispatchId, ct);
        }

        if (submission.Status != TaxDispatchStatus.Pending)
            return new(submission.DispatchId, submission.AttemptId, submission.Status, submission.TaxId, submission.ReferenceNumber);

        if (submission.Reference is { } reference)
        {
            if (reference.Status != TaxDispatchStatus.Accepted || reference.Kind == TaxDispatchKind.Cancellation)
                throw new ArgumentException("سند مرجع دیگر قابل استفاده نیست.");
            TaxReferenceInvoice.ValidateWorkFolder(reference.WorkFolderEvidence, reference.TaxId, reference.Kind, submission.Kind);
        }

        var attempt = Guid.NewGuid();
        // Decryption, validation, certificate access and nonce requests run before any send attempt is recorded.
        using var prepared = await client.PrepareAsync(submission, attempt, ct);
        ct.ThrowIfCancellationRequested();
        if (!await repository.TryStartAsync(request.DispatchId, attempt, clock.GetUtcNow(),
            submission.Reference?.EvidenceVersion, ct))
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
