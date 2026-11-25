using System.Globalization;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.SelectAccount;

public sealed record SelectAccountCommand(string UserId, string Type = "Login") : ICommand<AccountOutcome>;

public sealed class SelectAccountCommandHandler(
    IAccountRequestContext context, IAccountUserRepository users, AccountSignInFlow login)
    : ICommandHandler<SelectAccountCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(SelectAccountCommand request, CancellationToken cancellationToken)
    {
        var mobile = context.GetSession("OtpVerifiedMobile");
        var until = context.GetSession("OtpVerifiedUntilUtc");
        if (string.IsNullOrWhiteSpace(mobile)
            || request.Type != context.GetSession("OtpVerifiedType")
            || !DateTime.TryParse(until, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expires)
            || expires <= DateTime.UtcNow)
            return new(403, "اعتبار تأیید پیامکی پایان یافته است. دوباره کد دریافت کنید.");

        var accounts = await users.FindByPhoneAsync(mobile, cancellationToken);
        if (accounts.Count == 0) accounts = await users.FindByUserNameAsync(mobile, cancellationToken);
        var user = accounts.FirstOrDefault(u => u.Id == request.UserId);
        if (user is null) return new(403, "این حساب به شماره موبایل تأییدشده تعلق ندارد.");

        context.SetSession("OtpVerifiedMobile", "");
        context.SetSession("OtpVerifiedType", "");
        context.SetSession("OtpVerifiedUntilUtc", "");
        return new(200, "موفق", await login.CompleteOtpAsync(user, request.Type, mobile, cancellationToken));
    }
}
