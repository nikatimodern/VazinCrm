using System.Security.Cryptography;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.RequestOtp;

public sealed record RequestOtpCommand(string Mobile, string Type = "Login") : ICommand<AccountOutcome>
{
    public override string ToString() => nameof(RequestOtpCommand);
}

public sealed class RequestOtpCommandHandler(IOtpMobileRepository otp, IAccountOtpDelivery delivery)
    : ICommandHandler<RequestOtpCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var mobile = AccountOtpPhone.Normalize(request.Mobile);
        if (mobile is null || request.Type is not ("Login" or "Forgot"))
            return new(400, "شماره موبایل یا نوع درخواست معتبر نیست");

        var code = RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4");
        var result = await otp.IssueAsync(mobile, request.Type, code, cancellationToken);
        if (result.Status == OtpActionStatus.Locked)
            return new(429, $"تلاش‌های ناموفق بیش از حد مجاز است. {result.RetryAfterSeconds} ثانیه دیگر تلاش کنید.",
                RetryAfterSeconds: result.RetryAfterSeconds);
        if (result.Status == OtpActionStatus.Cooldown)
            return new(429, $"برای دریافت کد جدید {result.RetryAfterSeconds} ثانیه صبر کنید.",
                RetryAfterSeconds: result.RetryAfterSeconds);

        try
        {
            await delivery.SendAsync(mobile, code, request.Type == "Forgot", cancellationToken);
        }
        catch
        {
            await otp.CancelAsync(mobile, request.Type, code, CancellationToken.None);
            return new(503, "ارسال پیامک ناموفق بود. دوباره تلاش کنید.");
        }

        return new(200, "کد یکبار مصرف ارسال شد");
    }
}
