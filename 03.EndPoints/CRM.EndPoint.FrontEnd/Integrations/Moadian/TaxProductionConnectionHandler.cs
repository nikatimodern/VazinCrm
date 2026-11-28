using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Taxation.Submission;
using CRM.Core.Domain.Taxation;

namespace CRM.EndPoint.FrontEnd.Integrations.Moadian;

internal sealed class TaxProductionConnectionHandler(TaxAuthorityProtocol protocol, HttpMessageHandler inner)
    : DelegatingHandler(inner)
{
    internal const string ClientKey = "TaxProductionConnection";

    internal static Uri Address(TaxAuthorityProtocol protocol) =>
        new(protocol == TaxAuthorityProtocol.V1 ? "https://tp.tax.gov.ir/req/" :
            "https://tp.tax.gov.ir/requestsmanager/api/v2/");

    internal static string ReachabilityPath(TaxAuthorityProtocol protocol) =>
        protocol == TaxAuthorityProtocol.V1 ? "api/self-tsp/sync/GET_TOKEN" : "nonce";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri;
        if (uri is not { IsAbsoluteUri: true, Scheme: "https", Host: "tp.tax.gov.ir", IsDefaultPort: true, UserInfo: "", Fragment: "" } ||
            !Allows(request.Method, uri))
            throw new TaxTransportException("Request outside the production connection check.");
        return base.SendAsync(request, ct);
    }

    private bool Allows(HttpMethod method, Uri uri)
    {
        if (method == HttpMethod.Head) return uri == new Uri(Address(protocol), ReachabilityPath(protocol));
        if (protocol == TaxAuthorityProtocol.V1)
            return method == HttpMethod.Post && uri.Query.Length == 0 && uri.AbsolutePath is
                "/req/api/self-tsp/sync/GET_TOKEN" or "/req/api/self-tsp/sync/GET_FISCAL_INFORMATION" or
                "/req/api/self-tsp/sync/GET_SERVER_INFORMATION";
        return method == HttpMethod.Get &&
            (uri.PathAndQuery == "/requestsmanager/api/v2/nonce?timeToLive=30" ||
             uri.PathAndQuery == "/requestsmanager/api/v2/server-information");
    }
}
