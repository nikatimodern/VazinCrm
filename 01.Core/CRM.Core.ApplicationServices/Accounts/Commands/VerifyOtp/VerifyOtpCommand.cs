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

public sealed class VerifyOtpCommandHandler(IAccountRequestContext context, IAccountUserRepository users, IAccountRepository repository, IAccountAudit audit, AccountSignInFlow login) : ICommandHandler<VerifyOtpCommand, AccountOutcome>
{
    public async Task<AccountOutcome> HandleAsync(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var correctOtp = context.GetSession($"{request.Type}OtpCode");
        if (context.GetSession($"{request.Type}IsExpireOtp") == "true") return new(401, "کد منقضی شده است");
        if (request.OtpCode != correctOtp) return new(403, "کد نادرست است");
        var accounts = await users.FindByPhoneAsync(request.Mobile, cancellationToken);
        if (accounts.Count == 0) accounts = await users.FindByUserNameAsync(request.Mobile, cancellationToken);
        if (accounts.Count == 0)
        {
            var customer = await repository.FindCustomerByMobileAsync(request.Mobile, cancellationToken);
            var user = new AppUser { UserName = request.Mobile, PhoneNumber = request.Mobile, FirstName = customer?.CompanyName, LastName = customer?.AgentName ?? request.Mobile, PersonId = customer?.Id };
            var result = await users.CreateAsync(user, cancellationToken);
            if (!result.Succeeded)
                return new(405, "", Errors: result.Errors.Select(e => e.Code == "DuplicateUserName" ? $"نام کاربری '{user.UserName}' قبلاً ثبت شده است." : e.Description).ToList());
            await repository.ReassignChatsAsync(request.Mobile, user.Id, cancellationToken);
            foreach (var role in customer?.RoleTypeTxt?.Split(" ", StringSplitOptions.RemoveEmptyEntries) ?? [])
                await users.AddToRoleAsync(user, role, cancellationToken);
            await audit.WriteAsync(user, AccountAuditAction.Created, customer?.RoleTypeTxt, cancellationToken);
            accounts.Add(user);
        }
        if (accounts.Count > 1)
            return new(300, "چند حساب یافت شد. لطفا یکی را انتخاب کنید.", Accounts: accounts.Select(u => new AccountChoice(u.Id, u.UserName, u.FullName)).ToList());
        return new(200, "موفق", await login.CompleteOtpAsync(accounts[0], request.Type, cancellationToken));
    }
}
