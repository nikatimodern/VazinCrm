#nullable enable
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.Core.Domain.Sms.Enums;
using CRM.EndPoint.FrontEnd.IServices.Sms;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Services.LogSystem;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
namespace CRM.EndPoint.FrontEnd.Services.Accounts;

public sealed class AccountRequestContext(IHttpContextAccessor accessor, SignInManager<AppUser> signIn) : IAccountRequestContext
{
    private HttpContext Context => accessor.HttpContext ?? throw new InvalidOperationException("An HTTP request is required.");
    public string? UserId => signIn.UserManager.GetUserId(Context.User);
    public string? RemoteIp => Context.Connection.RemoteIpAddress?.ToString();
    public string UserAgent => Context.Request.Headers.UserAgent.ToString();
    public string? GetSession(string key) => Context.Session.GetString(key);
    public void SetSession(string key, string value) => Context.Session.SetString(key, value);
    public void ClearSession() => Context.Session.Clear();
}
public sealed class AccountAuthentication(SignInManager<AppUser> signIn) : IAccountAuthentication
{
    public async Task<AccountSignInResult> CheckPasswordAsync(AppUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Validate using Identity's security checks without signing in or touching Identity's failure counter.
        var result = await signIn.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        // Preserve the two-factor requirement enforced by PasswordSignInAsync.
        var requiresTwoFactor = result.Succeeded
            && await signIn.IsTwoFactorEnabledAsync(user)
            && !await signIn.IsTwoFactorClientRememberedAsync(user);
        return new(result.Succeeded, result.IsLockedOut, requiresTwoFactor);
    }
    public async Task SignInAsync(AppUser user, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await signIn.SignInAsync(user, new AuthenticationProperties { AllowRefresh = true, IsPersistent = true, ExpiresUtc = expiresAt });
    }
    public async Task SignOutAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await signIn.SignOutAsync();
    }
}
public sealed class AccountAudit(LogSystemService log) : IAccountAudit
{
    public async Task WriteAsync(AppUser user, AccountAuditAction action, string? details, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var logAction = action switch { AccountAuditAction.Login => LogActionEnum.ورود_کاربر, AccountAuditAction.Logout => LogActionEnum.خروج_کاربر, AccountAuditAction.Created => LogActionEnum.ثبت, AccountAuditAction.PasswordReset => LogActionEnum.ویرایش, _ => throw new ArgumentOutOfRangeException(nameof(action)) };
        await log.LogChange(user, null, logAction, user, null, details);
    }
}
public sealed class AccountOtpDelivery(ISmsQueue queue) : IAccountOtpDelivery
{
    public Task SendAsync(string mobile, string code, bool forgot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return queue.QueueAsync(new SmsJob { PatternType = forgot ? SmsPatternType.OtpForgot : SmsPatternType.OtpLogin, Mobile = mobile, Code = code });
    }
}
public sealed class AccountImages(FileSaver files) : IAccountImages
{
    public AccountImageResult Save(AccountUpload upload, string userId, string? oldUrl)
    {
        using var stream = new MemoryStream(upload.Contents, false);
        var file = new FormFile(stream, 0, stream.Length, "PersonImage", upload.FileName) { Headers = new HeaderDictionary(), ContentType = upload.ContentType };
        var compressed = files.CompressImage(file);
        var url = files.SaveImagePersonLogin(file, userId, oldUrl);
        return new(compressed, url);
    }
}
public sealed class AccountNotifications(IHubContext<TaskHub> hub) : IAccountNotifications
{
    public Task AccessDeniedAsync(string userId, CancellationToken cancellationToken) =>
        hub.Clients.User(userId).SendAsync("ShowAccessDeniedMessage", "شما به این صفحه دسترسی ندارید!", cancellationToken);
}
