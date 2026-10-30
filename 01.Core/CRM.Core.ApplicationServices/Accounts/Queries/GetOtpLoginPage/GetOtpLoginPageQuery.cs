using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Queries.GetOtpLoginPage;

public sealed record GetOtpLoginPageQuery() : IQuery<AccountRedirect?>;

public sealed class GetOtpLoginPageQueryHandler(IAccountUserRepository users, IAccountRequestContext context) : IQueryHandler<GetOtpLoginPageQuery, AccountRedirect?>
{
    public async Task<AccountRedirect?> HandleAsync(GetOtpLoginPageQuery request, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(context.UserId, cancellationToken);
        return user is null ? null : user.IsEmployee ? new("index", "Dashboard") : new("Home");
    }
}
