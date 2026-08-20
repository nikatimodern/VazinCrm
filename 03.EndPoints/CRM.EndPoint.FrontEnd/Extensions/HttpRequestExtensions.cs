using System.Linq;
using Microsoft.AspNetCore.Http;

namespace CRM.EndPoint.FrontEnd.Extensions;

public static class HttpRequestExtensions {
    public static string GetSignalRConnectionId(this HttpRequest request)
    {
        return request?.Headers["X-SignalR-ConnectionId"].FirstOrDefault();
    }
}