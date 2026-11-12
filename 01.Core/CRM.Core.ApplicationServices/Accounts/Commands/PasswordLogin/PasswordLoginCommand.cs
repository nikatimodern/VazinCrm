using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.PasswordLogin;

public sealed record PasswordLoginCommand(string UserName, string Password, string? ReturnUrl) : ICommand<AccountOutcome>
{
    public override string ToString() => nameof(PasswordLoginCommand);
}

public sealed class PasswordLoginCommandHandler(
    IAccountUserRepository users,
    IPasswordLockoutRepository lockouts,
    IAccountAuthentication authentication,
    AccountSignInFlow login) : ICommandHandler<PasswordLoginCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(
        PasswordLoginCommand request, CancellationToken cancellationToken)
    {
        const string invalidCredentials = "نام کاربری یا رمز عبور اشتباه است!";
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrEmpty(request.Password))
            return new(404, invalidCredentials);

        var user = await users.FindByNameAsync(request.UserName, cancellationToken);
        if (user is null) return new(404, invalidCredentials);

        // The account state is persisted by UserId, not by username, phone or browser session.
        var current = await lockouts.GetStatusAsync(user.Id, cancellationToken);
        if (current?.Status == PasswordAttemptStatus.Locked)
            return Locked(current.RetryAfterSeconds);

        var check = await authentication.CheckPasswordAsync(user, request.Password, cancellationToken);
        if (check.IsLockedOut)
            return new(403, "حساب کاربری شما غیرفعال شده است.");

        // Repeat the lock check under a SQL transaction; parallel requests cannot bypass it.
        var attempt = await lockouts.RegisterAttemptAsync(user.Id, check.Succeeded, cancellationToken);
        if (attempt.Status == PasswordAttemptStatus.Locked)
            return Locked(attempt.RetryAfterSeconds);

        if (!check.Succeeded)
            return new(404, $"نام کاربری یا رمز عبور اشتباه است! {attempt.RemainingAttempts} تلاش باقی مانده است.",
                RemainingAttempts: attempt.RemainingAttempts);

        await authentication.SignOutAsync(cancellationToken);
        return new(200, "موفق", await login.CompleteAsync(user, request.ReturnUrl, cancellationToken));
    }

    private static AccountOutcome Locked(int retryAfterSeconds) =>
        new(429, $"ورود با رمز عبور موقتاً مسدود شده است. {retryAfterSeconds} ثانیه دیگر تلاش کنید.",
            RetryAfterSeconds: retryAfterSeconds);
}
