using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Queries.GetCurrentAccount;

public sealed record GetCurrentAccountQuery() : IQuery<GetCurrentAccountResult>;

public sealed class GetCurrentAccountQueryHandler(IAccountUserRepository users, IAccountRequestContext context) : IQueryHandler<GetCurrentAccountQuery, GetCurrentAccountResult>
{
    public async Task<GetCurrentAccountResult> HandleAsync(GetCurrentAccountQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken) ?? throw new UnauthorizedAccessException();
        var roles = await users.GetRolesAsync(user, cancellationToken);
        var roleMap = new Dictionary<string, string>
        {
            ["مدیر ارشد"] = "Admin",
            ["مدیر عامل"] = "ManagingDirector",
            ["مدیر اجرایی"] = "ExecutiveDirector",
            ["مدیر فروش"] = "SalesAdmin",
            ["کارشناس فروش"] = "SalesExpert",
            ["مدیر تامین"] = "TaminAdmin",
            ["کارشناس تامین"] = "TaminExpert",
            ["مدیر مالی"] = "FinancialAdmin",
            ["کارشناس مالی"] = "FinancialExpert",
            ["مشتری"] = "Customer",
            ["تنخواه دار"] = "TanKhah"
        };
        return new(user.Id, user.FirstName?.Trim(), user.LastName?.Trim(), roles.FirstOrDefault(), roles.Select(r => new AccountRole(roleMap.GetValueOrDefault(r, r), r)).ToList(), user.PersonImageUrl, user.PersonImage, user.PhoneNumber);
    }
}
public sealed record AccountRole(string Name, string FaName);
public sealed record GetCurrentAccountResult(string Id, string? FirstName, string? LastName, string? RollName, List<AccountRole> RollNames, string? PersonImageUrl, byte[]? PersonImage, string? PhoneNumber);
