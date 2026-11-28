using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxAuthorityRouter(TaxProtocolClients clients, TaxSandboxRouting routing) : ITaxAuthorityClient
{
    public async Task<PreparedTaxRequest> PrepareAsync(TaxSubmission submission, Guid attemptId, CancellationToken ct)
    {
        if (submission.Protocol != await routing.GetActiveProtocolAsync(ct))
            throw new ArgumentException("روش ثبت‌شده برای این درخواست با روش ارسال فعال شرکت یکسان نیست.");
        await EnsureAllowedAsync(submission, ct);
        return await For(submission.Protocol).PrepareAsync(submission, attemptId, ct);
    }

    public async Task<TaxAcknowledgement> SendAsync(PreparedTaxRequest request, CancellationToken ct)
    {
        if (request.Protocol != await routing.GetActiveProtocolAsync(ct) ||
            !await routing.IsAllowedAsync(request.MemoryId, request.Protocol, ct))
            throw new TaxTransportException("The selected tax transport is no longer available.");
        return await For(request.Protocol).SendAsync(request, ct);
    }

    public async Task<TaxProcessingResult> InquireAsync(TaxSubmission submission, CancellationToken ct)
    {
        await EnsureAllowedAsync(submission, ct);
        return await For(submission.Protocol).InquireAsync(submission, ct);
    }

    public async Task<TaxWorkFolderResult> GetWorkFolderAsync(TaxSubmission submission, CancellationToken ct)
    {
        await EnsureAllowedAsync(submission, ct);
        return await For(submission.Protocol).GetWorkFolderAsync(submission, ct);
    }

    private async Task EnsureAllowedAsync(TaxSubmission submission, CancellationToken ct)
    {
        if (submission.Environment != TaxAuthorityEnvironment.Sandbox ||
            !await routing.IsAllowedAsync(submission.MemoryId, submission.Protocol, ct))
            throw new ArgumentException("مسیر آزمایشی این حافظه فعال نیست.");
    }

    private ITaxAuthorityClient For(TaxAuthorityProtocol protocol) => protocol switch
    {
        TaxAuthorityProtocol.V1 => clients.V1,
        TaxAuthorityProtocol.V2 => clients.V2,
        _ => throw new ArgumentException("روش ارسال نامعتبر است.")
    };
}
