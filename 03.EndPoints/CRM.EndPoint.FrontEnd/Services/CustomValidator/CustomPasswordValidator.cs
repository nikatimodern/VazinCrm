using CRM.Core.Domain.AppUsers.Entities;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services.CustomValidator
{
    public class CustomPasswordValidator : PasswordValidator<AppUser>
    {
        public override async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string password)
        {
            //        IdentityResult result = await base.ValidateAsync(manager, user, password);

            //        List<IdentityError> errors = result.Succeeded ?
            //new List<IdentityError>() : result.Errors.ToList();

            List<IdentityError> errors = new List<IdentityError>();

            if (password.ToLower().Contains(user.UserName.ToLower()))
            {
                errors.Add(new IdentityError
                {
                    Code = "کلمه کاربری و رمز عبور یکیست",
                    Description = "کلمه عبور و نام کاربری نباید مثل هم باشند"
                });
            }
            if (password.Count() < 3 || user.UserName.Count() < 3)
            {
                errors.Add(new IdentityError
                {
                    Code = "رمزعبور و نام کاربری باید بیشتر از 3 کاراکتر باشند",
                    Description = "رمزعبور و نام کاربری باید بیشتر از 3 کاراکتر باشند"
                });
            }
            //if (password.Contains("12345"))
            //{
            //    errors.Add(new IdentityError
            //    {
            //        Code = "کلمه عبور متوالی",
            //        Description = "کلمه عبور متوالی "
            //    });
            //}
            return errors.Count == 0 ? IdentityResult.Success
            : IdentityResult.Failed(errors.ToArray());

        }

    }

    public class CustomPasswordValidator_OLD
        : IPasswordValidator<AppUser>
    {
        public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager,
            AppUser user, string password)
        {
            List<IdentityError> errors = new List<IdentityError>();
            if (password.ToLower().Contains(user.UserName.ToLower()))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordContainsUserName",
                    Description = "Password cannot contain username"
                });
            }
            if (password.Contains("12345"))
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordContainsSequence",
                    Description = "Password cannot contain numeric sequence"
                });
            }
            return Task.FromResult(errors.Count == 0 ?
            IdentityResult.Success : IdentityResult.Failed(errors.ToArray()));
        }
    }
}
