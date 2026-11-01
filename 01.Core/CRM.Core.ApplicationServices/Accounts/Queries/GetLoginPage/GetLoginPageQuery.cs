using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Queries.GetLoginPage;

public sealed record GetLoginPageQuery() : IQuery<bool>;

public sealed class GetLoginPageQueryHandler(IAccountUserRepository users, IAccountRequestContext context) : IQueryHandler<GetLoginPageQuery, bool>
{
    public async Task<bool> HandleAsync(GetLoginPageQuery request, CancellationToken cancellationToken)
    {
        return await users.FindByIdAsync(context.UserId, cancellationToken) is not null;
    }
}
