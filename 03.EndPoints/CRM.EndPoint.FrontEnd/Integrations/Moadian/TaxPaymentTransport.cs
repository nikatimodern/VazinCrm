using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxPaymentTransport(MoadianClient v2, TaxConnectionSettings settings) : ITaxPaymentTransport
{
    public Task<PreparedTaxRequest> PreparePaymentAsync(TaxSubmission invoice, Guid attemptId, string payload, CancellationToken ct)
    {
        if (v2 is null || settings.ActiveProtocol != TaxAuthorityProtocol.V2 || invoice.Protocol != TaxAuthorityProtocol.V2)
            throw new ArgumentException("ثبت پرداخت فقط با روش فعال نسخه ۲ در دسترس است.");
        return v2.PreparePaymentAsync(invoice, attemptId, payload, ct);
    }

    public Task<TaxPaymentReply> SubmitPaymentAsync(PreparedTaxRequest request, CancellationToken ct) =>
        settings.WithActiveSendAsync(TaxAuthorityProtocol.V2, () => v2.SubmitPaymentAsync(request, ct), ct);
}
