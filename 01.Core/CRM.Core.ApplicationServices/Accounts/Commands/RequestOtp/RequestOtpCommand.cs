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

public sealed class RequestOtpCommandHandler(IAccountRequestContext context, IAccountOtpDelivery delivery) : ICommandHandler<RequestOtpCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 9999).ToString();
        await delivery.SendAsync(request.Mobile, otp, request.Type == "Forgot", cancellationToken);
        context.SetSession($"{request.Type}IsExpireOtp", "false");
        context.SetSession($"{request.Type}OtpCode", otp);
        context.SetSession($"{request.Type}OtpStartTime", DateTime.Now.ToString("O"));
        context.SetSession("Phone", request.Mobile);
        return new(200, "کد یکبار مصرف ارسال شد");
    }
}
