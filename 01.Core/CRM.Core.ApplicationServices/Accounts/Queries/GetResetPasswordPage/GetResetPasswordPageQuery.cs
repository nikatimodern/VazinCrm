using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Abstractions;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.Core.ApplicationServices.Accounts.Services;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Accounts.Queries.GetResetPasswordPage;

public sealed record GetResetPasswordPageQuery() : IQuery<GetResetPasswordPageResult>;

public sealed class GetResetPasswordPageQueryHandler(IAccountRequestContext context) : IQueryHandler<GetResetPasswordPageQuery, GetResetPasswordPageResult>
{
    public Task<GetResetPasswordPageResult> HandleAsync(GetResetPasswordPageQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var phone = context.GetSession("Phone");
        var userId = context.GetSession("UserId");
        return Task.FromResult(new GetResetPasswordPageResult(!string.IsNullOrEmpty(phone) && context.GetSession("VerifiedOtp") == "true" && !string.IsNullOrEmpty(userId), phone));
    }
}
public sealed record GetResetPasswordPageResult(bool CanReset, string? Phone);
