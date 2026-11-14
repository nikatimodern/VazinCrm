using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.CheckOtpTimer;

public sealed record CheckOtpTimerCommand(string Type, string? Mobile) : ICommand<CheckOtpTimerResult?>;

public sealed class CheckOtpTimerCommandHandler(IOtpMobileRepository otp)
    : ICommandHandler<CheckOtpTimerCommand, CheckOtpTimerResult?>
{
    public async Task<CheckOtpTimerResult?> HandleAsync(CheckOtpTimerCommand request, CancellationToken cancellationToken)
    {
        var mobile = AccountOtpPhone.Normalize(request.Mobile);
        if (mobile is null || request.Type is not ("Login" or "Forgot")) return null;
        var state = await otp.GetStatusAsync(mobile, request.Type, cancellationToken);
        if (state is null) return null;

        var locked = state.Status == OtpActionStatus.Locked;
        var pending = state.Status == OtpActionStatus.Cooldown;
        return new(locked || pending, (locked || pending ? state.RetryAfterSeconds : 0).ToString(), locked);
    }
}

public sealed record CheckOtpTimerResult(bool Status, string ElsapsTime, bool IsLocked);
