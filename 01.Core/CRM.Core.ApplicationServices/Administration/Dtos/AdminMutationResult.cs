using CRM.Core.Domain.AppUsers.Entities;
using CRM.Core.Domain.AppUsers.Repositories;

namespace CRM.Core.ApplicationServices.Administration.Dtos;
public sealed record AdminMutationResult(int Status, string? Message = null, AppUser? User = null, IList<string>? Roles = null)
{
    public static AdminMutationResult Failure(AccountIdentityResult result) => new(400, string.Join("، ", result.Errors.Select(e => e.Description)));
    public static AdminMutationResult Missing() => new(404, "کاربر پیدا نشد.");
}
