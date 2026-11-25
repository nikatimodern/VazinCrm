using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.VerifyOtp;

public sealed record VerifyOtpCommand(string? OtpCode, string Mobile, string Type = "Login") : ICommand<AccountOutcome>
{
    public override string ToString() => nameof(VerifyOtpCommand);
}

public sealed class VerifyOtpCommandHandler(
    IAccountRequestContext context,
    IAccountUserRepository users,
    IAccountRepository repository,
    IOtpMobileRepository otp,
    IAccountAudit audit,
    AccountSignInFlow login) : ICommandHandler<VerifyOtpCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var mobile = AccountOtpPhone.Normalize(request.Mobile);
        if (mobile is null || request.Type is not ("Login" or "Forgot"))
            return new(400, "شماره موبایل یا نوع درخواست معتبر نیست");

        var check = await otp.VerifyAsync(mobile, request.Type, request.OtpCode, cancellationToken);
        switch (check.Status)
        {
            case OtpActionStatus.Locked:
                return new(429, $"کد قبلی باطل شد. {check.RetryAfterSeconds} ثانیه دیگر کد جدید دریافت کنید.",
                    RetryAfterSeconds: check.RetryAfterSeconds);
            case OtpActionStatus.Invalid:
                return new(403, $"کد نادرست است. {check.RemainingAttempts} تلاش باقی مانده است.",
                    RemainingAttempts: check.RemainingAttempts);
            case OtpActionStatus.Expired:
                return new(401, "کد منقضی شده یا استفاده شده است. کد جدید دریافت کنید.");
            case OtpActionStatus.Accepted:
                break;
            default:
                return new(401, "کد معتبر نیست.");
        }

        var accounts = await users.FindByPhoneAsync(mobile, cancellationToken);
        if (accounts.Count == 0) accounts = await users.FindByUserNameAsync(mobile, cancellationToken);
        if (accounts.Count == 0)
        {
            var customer = await repository.FindCustomerByMobileAsync(mobile, cancellationToken);
            var user = new AppUser
            {
                UserName = mobile,
                PhoneNumber = mobile,
                FirstName = customer?.CompanyName,
                LastName = customer?.AgentName ?? mobile,
                PersonId = customer?.Id
            };
            var result = await users.CreateAsync(user, cancellationToken);
            if (!result.Succeeded)
                return new(405, "", Errors: result.Errors.Select(e => e.Code == "DuplicateUserName"
                    ? $"نام کاربری '{user.UserName}' قبلاً ثبت شده است."
                    : e.Description).ToList());
            await repository.ReassignChatsAsync(mobile, user.Id, cancellationToken);
            foreach (var role in customer?.RoleTypeTxt?.Split(" ", StringSplitOptions.RemoveEmptyEntries) ?? [])
                await users.AddToRoleAsync(user, role, cancellationToken);
            await audit.WriteAsync(user, AccountAuditAction.Created, customer?.RoleTypeTxt, cancellationToken);
            accounts.Add(user);
        }

        if (accounts.Count > 1)
        {
            context.SetSession("OtpVerifiedMobile", mobile);
            context.SetSession("OtpVerifiedType", request.Type);
            context.SetSession("OtpVerifiedUntilUtc", DateTime.UtcNow.AddMinutes(2).ToString("O"));
            return new(300, "چند حساب یافت شد. لطفا یکی را انتخاب کنید.",
                Accounts: accounts.Select(u => new AccountChoice(u.Id, u.UserName, u.FullName)).ToList());
        }

        return new(200, "موفق",
            await login.CompleteOtpAsync(accounts[0], request.Type, mobile, cancellationToken));
    }
}
