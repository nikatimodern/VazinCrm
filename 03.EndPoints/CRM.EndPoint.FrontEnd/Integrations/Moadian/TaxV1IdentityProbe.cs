using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Queries.CheckTaxV1Connection;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxV1IdentityProbe(MoadianV1Client client) : ITaxV1IdentityProbe
{
    public Task CheckAsync(string fiscalId, TaxSigningBinding binding, CancellationToken ct) =>
        client is null || binding.Environment != TaxAuthorityEnvironment.Sandbox
            ? throw new ArgumentException("مسیر آزمایشی نسخه ۱ تنظیم نشده است.")
            : client.VerifyIdentityAsync(fiscalId, binding, ct);
}
