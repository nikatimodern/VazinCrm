using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.CheckOtpTimer;

public sealed record CheckOtpTimerCommand(string Type) : ICommand<CheckOtpTimerResult?>;

public sealed class CheckOtpTimerCommandHandler(IAccountRequestContext context) : ICommandHandler<CheckOtpTimerCommand, CheckOtpTimerResult?>
{
    public Task<CheckOtpTimerResult?> HandleAsync(CheckOtpTimerCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var time = context.GetSession($"{request.Type}OtpStartTime");
        if (time is null) return Task.FromResult<CheckOtpTimerResult?>(null);
        var elapsed = DateTime.Now - DateTime.Parse(time);
        var status = elapsed.TotalMinutes <= 1;
        var seconds = status ? Math.Floor((1 - elapsed.TotalMinutes) * 60).ToString() : "0";
        if (seconds == "0") context.SetSession($"{request.Type}IsExpireOtp", "true");
        return Task.FromResult<CheckOtpTimerResult?>(new CheckOtpTimerResult(status, seconds));
    }
}
public sealed record CheckOtpTimerResult(bool Status, string ElsapsTime);
