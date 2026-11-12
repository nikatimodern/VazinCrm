using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Commands.ResetAccountPassword;

public sealed record ResetAccountPasswordCommand(string NewPassword) : ICommand<AccountOutcome>
{
    public override string ToString() => nameof(ResetAccountPasswordCommand);
}

public sealed class ResetAccountPasswordCommandHandler(IAccountRequestContext context, IAccountUserRepository users, IAccountRepository repository, IAccountAudit audit, IOtpMobileRepository otp, AccountSignInFlow login) : ICommandHandler<ResetAccountPasswordCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(ResetAccountPasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = context.GetSession("UserId");
        var phone = context.GetSession("Phone");
        if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(userId)) return new(404, "کاربر یافت نشد");
        var user = await users.FindByIdAsync(userId, cancellationToken);
        if (user is null) return new(404, "کاربری با این شماره پیدا نشد", new("OtpLogin"));
        var result = await users.ResetPasswordAsync(user, request.NewPassword, cancellationToken);
        if (!result.Succeeded) return new(405, "", Errors: result.Errors.Select(e => e.Description).ToList());
        await audit.WriteAsync(user, AccountAuditAction.PasswordReset, "بازیابی رمز عبور", cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        context.ClearSession();
        var redirect = await login.CompleteAsync(user, null, cancellationToken);
        await otp.ResetLockoutAsync(phone, cancellationToken);
        return new(200, "رمز عبور با موفقیت تغییر یافت", redirect);
    }
}
