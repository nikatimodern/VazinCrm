using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxPaymentTransport(TaxProtocolClients clients, TaxSandboxRouting routing) : ITaxPaymentTransport
{
    public async Task<PreparedTaxRequest> PreparePaymentAsync(TaxSubmission invoice, Guid attemptId, string payload, CancellationToken ct)
    {
        if (invoice.Environment != TaxAuthorityEnvironment.Sandbox || invoice.Protocol != TaxAuthorityProtocol.V2 ||
            !await IsAllowedAsync(invoice.MemoryId, ct))
            throw new ArgumentException("ثبت پرداخت فقط با روش فعال آزمایشی نسخه ۲ در دسترس است.");
        return await clients.V2.PreparePaymentAsync(invoice, attemptId, payload, ct);
    }

    public async Task<TaxPaymentReply> SubmitPaymentAsync(PreparedTaxRequest request, CancellationToken ct)
    {
        if (!await IsAllowedAsync(request.MemoryId, ct))
            throw new TaxTransportException("The selected tax payment transport is no longer available.");
        return await clients.V2.SubmitPaymentAsync(request, ct);
    }

    private async Task<bool> IsAllowedAsync(Guid? id, CancellationToken ct) =>
        await routing.GetActiveProtocolAsync(ct) == TaxAuthorityProtocol.V2 &&
        await routing.IsAllowedAsync(id, TaxAuthorityProtocol.V2, ct);
}
