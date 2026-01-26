using CRM.Core.Domain.AppUsers.Entities;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services.CustomValidator
{
    public class CustomUserValidator : IUserValidator<AppUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
        {
            if (user.Email.ToLower().EndsWith("@gmail.com"))
            {
                return Task.FromResult(IdentityResult.Success);
            }
            else
            {
                return Task.FromResult(IdentityResult.Failed(new IdentityError
                {
                    Code = "EmailDomainError",
                    Description = "Only gmail.com email addresses are allowed"
                }));
            }
        }
    }
}
