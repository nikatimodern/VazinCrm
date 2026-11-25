using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CRM.EndPoint.FrontEnd.Composition;

internal sealed partial class TaxationAuditFilter(ILogger<TaxationAuditFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var http = context.HttpContext;
        var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var action = context.ActionDescriptor.RouteValues["action"];
        var controller = context.ActionDescriptor.RouteValues["controller"];
        var routingValue = context.RouteData.Values.TryGetValue("id", out var value) ? value?.ToString() : null;
        Guid? target = Guid.TryParse(routingValue, out var id) ? id : null;
        var status = StatusCodes.Status500InternalServerError;
        try
        {
            var result = await next();
            if (result.Exception is null || result.ExceptionHandled)
                status = (result.Result as IStatusCodeActionResult)?.StatusCode ?? http.Response.StatusCode;
        }
        finally
        {
            Record(logger, controller, action, actor, target, status);
        }
    }

    [LoggerMessage(EventId = 6201, EventName = "TaxationAudit", Level = LogLevel.Information,
        Message = "Taxation audit {Controller}.{Action} actor {ActorId} target {TargetId} HTTP {StatusCode}")]
    private static partial void Record(ILogger logger, string controller, string action, string actorId, Guid? targetId, int statusCode);
}
