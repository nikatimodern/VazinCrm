using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;
namespace CRM.Core.ApplicationServices.Accounts.Services;

public sealed class AccountSignInFlow(IAccountAuthentication authentication, IAccountRequestContext context, IAccountAudit audit, IAccountRepository repository)
{
    public async Task<AccountRedirect> CompleteAsync(AppUser user, string? returnUrl, CancellationToken cancellationToken)
    {
        await authentication.SignInAsync(user, DateTimeOffset.Now.AddDays(2), cancellationToken);
        context.SetSession("ReleaseNotesSession", Guid.NewGuid().ToString("N"));
        var ua = context.UserAgent.ToLower();
        var device = ua.Contains("mobi") || ua.Contains("iphone") || ua.Contains("android") ? "Mobile" : ua.Contains("ipad") || ua.Contains("tablet") ? "Tablet" : "Desktop";
        var os = ua.Contains("windows") ? "Windows" : ua.Contains("android") ? "Android" : ua.Contains("iphone") ? "iOS" : ua.Contains("mac") ? "macOS" : ua.Contains("linux") ? "Linux" : "Unknown";
        var browser = ua.Contains("edg") ? "Edge" : ua.Contains("samsungbrowser") ? "SamsungBrowser" : ua.Contains("chrome") ? "Chrome" : ua.Contains("safari") ? "Safari" : ua.Contains("firefox") ? "Firefox" : ua.Contains("opr") ? "Opera" : "Unknown";
        await audit.WriteAsync(user, AccountAuditAction.Login, $"login: {user.UserName}, {device}, {os}, {browser}, {context.RemoteIp}", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return user.IsEmployee ? new("Index", "Dashboard", string.IsNullOrEmpty(returnUrl) ? null : returnUrl) : new("Home");
    }
    public Task<AccountRedirect> CompleteOtpAsync(AppUser user, string type, CancellationToken cancellationToken)
    {
        if (type == "Forgot")
        {
            context.SetSession("VerifiedOtp", "true");
            context.SetSession("UserId", user.Id);
            return Task.FromResult(new AccountRedirect("ResetPassword"));
        }
        context.ClearSession();
        return CompleteAsync(user, null, cancellationToken);
    }
}
