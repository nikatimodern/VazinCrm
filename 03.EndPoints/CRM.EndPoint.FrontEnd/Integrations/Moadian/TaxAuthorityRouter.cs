using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxAuthorityRouter(MoadianV1Client v1, MoadianClient v2, TaxConnectionSettings settings) : ITaxAuthorityClient
{
    public Task<PreparedTaxRequest> PrepareAsync(TaxSubmission submission, Guid attemptId, CancellationToken ct)
    {
        if (submission.Protocol != settings.ActiveProtocol)
            throw new ArgumentException("روش ثبت‌شده برای این درخواست با روش ارسال فعال شرکت یکسان نیست.");
        return For(submission.Protocol).PrepareAsync(submission, attemptId, ct);
    }

    public Task<TaxAcknowledgement> SendAsync(PreparedTaxRequest request, CancellationToken ct) =>
        settings.WithActiveSendAsync(request.Protocol, () => For(request.Protocol).SendAsync(request, ct), ct);

    public Task<TaxProcessingResult> InquireAsync(TaxSubmission submission, CancellationToken ct) =>
        For(submission.Protocol).InquireAsync(submission, ct);

    public Task<TaxWorkFolderResult> GetWorkFolderAsync(TaxSubmission submission, CancellationToken ct) =>
        For(submission.Protocol).GetWorkFolderAsync(submission, ct);

    private ITaxAuthorityClient For(TaxAuthorityProtocol protocol) => protocol switch
    {
        TaxAuthorityProtocol.V1 when v1 is not null => v1,
        TaxAuthorityProtocol.V2 when v2 is not null => v2,
        _ => throw new ArgumentException("مسیر آزمایشی روش انتخاب‌شده تنظیم نشده است.")
    };
}
