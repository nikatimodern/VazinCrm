using System;
using System.Net.Http;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxProtocolClients(HttpClient legacy, HttpClient modern, ITaxV1SigningKeys keys,
    ITaxCertificateStore certs, TimeProvider clock) : IDisposable
{
    internal MoadianV1Client V1 { get; } = new(legacy, keys, clock);
    internal MoadianClient V2 { get; } = new(modern, certs, clock);
    internal HttpClient For(TaxAuthorityProtocol protocol) => protocol == TaxAuthorityProtocol.V1 ? legacy : modern;

    public void Dispose()
    {
        legacy.Dispose();
        modern.Dispose();
    }
}
