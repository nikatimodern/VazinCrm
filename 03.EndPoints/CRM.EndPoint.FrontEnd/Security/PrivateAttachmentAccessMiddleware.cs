using System;
using System.Linq;
using System.Threading.Tasks;
using CRM.Core.Domain.Attachments.Enum;
using CRM.Infrastructures.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRM.EndPoint.FrontEnd.Security;

// Keep existing URLs for private chat/contract files, but enforce ownership before
// the static-file middleware runs. Unregistered and deleted files fail closed.
public sealed class PrivateAttachmentAccessMiddleware(RequestDelegate next)
{
    public static bool Matches(PathString path) =>
        path.StartsWithSegments("/Uploaded/Chat", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Uploaded/ContractAttach", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/Uploaded/ContractAtt", StringComparison.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context, CRMContextDb db, DocumentAccessPolicy access)
    {
        context.Response.Headers["Cache-Control"] = "private, no-store";
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var path = context.Request.Path.Value ?? "";
        var isChat = context.Request.Path.StartsWithSegments("/Uploaded/Chat", StringComparison.OrdinalIgnoreCase);
        var type = isChat ? _AttachType.Chat : _AttachType.ContractAttach;
        var ct = context.RequestAborted;
        var candidates = await db.Attachments.AsNoTracking()
            .Where(x => !x.IsDeleted && x._AttachType == type && x.EntityId.HasValue && x.Url != null &&
                (x.Url.Replace("\\", "/") == path || x.Url.Replace("\\", "/") == path.Substring(1)))
            .Select(x => x.EntityId.Value).Distinct().Take(2).ToListAsync(ct);
        var permitted = candidates.Count == 1 && (isChat
            ? await access.CanChatAsync(candidates[0], ct)
            : !context.User.IsInRole("مشتری") && await access.CanAsync("Contract", "View", candidates[0], ct)
                && await db.Contracts.AsNoTracking().AnyAsync(c => c.Id == candidates[0] && !c.IsDeleted, ct));
        if (!permitted)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        await next(context);
    }
}
