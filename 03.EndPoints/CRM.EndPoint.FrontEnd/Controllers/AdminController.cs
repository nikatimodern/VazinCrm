using CRM.Core.Domain.AppUsers.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;\nusing Microsoft.EntityFrameworkCore;
using CRM.EndPoint.FrontEnd.Models.Admin;
using CRM.EndPoint.FrontEnd.Models.Login;

namespace CRM.EndPoint.FrontEnd.Controllers {
    [Authorize(Roles = "مدیر ارشد")]
    public class AdminController : Controller {
        private readonly UserManager<AppUser> userManager;

        private readonly IUserValidator<AppUser> userValidator;
        private readonly IPasswordValidator<AppUser> passwordValidator;
        private readonly IPasswordHasher<AppUser> passwordHasher;
        private readonly RoleManager<IdentityRole> roleManager;

        public AdminController(UserManager<AppUser> userManager,
            IUserValidator<AppUser> userValid,
            IPasswordValidator<AppUser> passValid,
            IPasswordHasher<AppUser> passwordHash,
            RoleManager<IdentityRole> roleManager)
        {
            this.userManager = userManager;
            userValidator = userValid;
            passwordValidator = passValid;
            passwordHasher = passwordHash;
            this.roleManager = roleManager;
        }

        public IActionResult Index()
        {
            ViewBag.Head = "Users";
            var model = new AdminViewModel
            {
                Roles = roleManager.Roles.ToList(),
                Users = new List<AppUser>()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await userManager.Users
                .Where(user => user.OrganAdmin == false)
                .OrderBy(user => user.UserName)
                .ToListAsync();

            // Use the scoped Identity DbContext sequentially to avoid concurrent EF operations.
            foreach (var user in users)
            {
                user.RolsUsers = await userManager.GetRolesAsync(user);
            }

            return PartialView("_UserCardsPartial", users);
        }

        [HttpPost]
        public string CreateOrUpUserSportMan(AppUser model)
        {
            ViewBag.Head = "Users";
            List<IdentityError> identityErrors = new List<IdentityError>();
            if (model.UserName == null)
            {
                identityErrors.Add(
                    new IdentityError() {
                        Code = "501",
                        Description = "نام کاربری اجباریست"
                    }
                );
            }

            if (model.PasswordHash == null)
            {
                identityErrors.Add(
                    new IdentityError() {
                        Code = "502",
                        Description = "کلمه عبور اجباریست"
                    }
                );
            }

            if (identityErrors.Count > 0)
            {
                return JsonConvert.SerializeObject(identityErrors.ToList());
            } else
            {
                IdentityResult result = userManager.CreateAsync(model, model.PasswordHash).Result;
                if (result.Succeeded)
                {
                    var user = userManager.FindByNameAsync(model.UserName).Result;
                    var resultAddRol = userManager.AddToRoleAsync(user, "User").Result;
                    List<IdentityError> _IdentityError = new List<IdentityError>();
                    _IdentityError.Add(new IdentityError() { Code = "200", Description = "حسابت کاربری با موفقیت ساخته شد" });
                    return JsonConvert.SerializeObject(_IdentityError);
                } else
                {
                    return JsonConvert.SerializeObject(result.Errors.ToList());
                }
            }

            //if (ModelState.IsValid)
            //{
            //}
            //else
            //{
            //    var errors = ModelState.Select(x => x.Value.Errors)
            //           .Where(y => y.Count > 0)
            //           .ToList();
            //}
            //return null;
        }

        //public ViewResult Create() => View();
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Head = "CreateUser";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateModel model)
        {
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .SelectMany(x => x.Value.Errors.Select(e => e.ErrorMessage))
                    .ToList();
                return BadRequest(new { success = false, message = string.Join("، ", validationErrors) });
            }

            var user = new AppUser
            {
                UserName = model.UserName,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                IsEmployee = true
            };

            var result = await userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, message = string.Join("، ", result.Errors.Select(e => e.Description)) });
            }

            return Json(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    userName = user.UserName,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    phoneNumber = user.PhoneNumber,
                    email = user.Email,
                    internalTels = user.InternalTels
                }
            });
        }

        //[HttpPost]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { success = false, message = "کاربر پیدا نشد." });
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, message = string.Join("، ", result.Errors.Select(e => e.Description)) });
            }

            return Json(new { success = true });
        }

        public async Task<IActionResult> Edit(string id)
        {
            AppUser user = await userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.PasswordHash = "";
                return View(user);
            } else
            {
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AppUser model, List<string> InternalTelsArray)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user != null)
            {
                user.Email = model.Email;
                user.UserName = model.UserName;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                user.InternalTels = InternalTelsArray != null
                    ? string.Join(",", InternalTelsArray.Where(x => !string.IsNullOrWhiteSpace(x)))
                    : null;

                IdentityResult validEmail = await userValidator.ValidateAsync(userManager, user);
                if (!validEmail.Succeeded)
                {
                    AddErrorsFromResult(validEmail);
                }

                IdentityResult validPass = null;
                if (!string.IsNullOrEmpty(model.PasswordHash))
                {
                    validPass = await passwordValidator.ValidateAsync(userManager, user, model.PasswordHash);

                    if (validPass.Succeeded)
                    {
                        user.PasswordHash = passwordHasher.HashPassword(user, model.PasswordHash);
                    } else
                    {
                        AddErrorsFromResult(validPass);
                    }
                }

                if (validEmail.Succeeded && validPass == null || validEmail.Succeeded && model.PasswordHash != string.Empty && validPass.Succeeded)
                {
                    IdentityResult result = await userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    } else
                    {
                        AddErrorsFromResult(result);
                    }
                }
            } else
            {
                ModelState.AddModelError("", "نام کاربری یافت نشد");
            }

            return View(user);
        }


        private void AddErrorsFromResult(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DelUserRol(string id, string role)
        {
            AppUser User = await userManager.FindByIdAsync(id);

            if (User == null)
            {
                return NotFound(new { success = false, message = "کاربر پیدا نشد." });
            }

            var result = await userManager.RemoveFromRoleAsync(User, role);
            if (!result.Succeeded)
            {
                return BadRequest(new { success = false, message = string.Join("، ", result.Errors.Select(e => e.Description)) });
            }

            return Json(new { success = true });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRoles(string userName, List<string> roleNames)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound(new { success = false, message = "کاربر پیدا نشد." });

            var selectedRoles = roleNames ?? new List<string>();
            foreach (var role in selectedRoles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    return BadRequest(new { success = false, message = "یک یا چند نقش انتخاب‌شده معتبر نیست." });
            }

            var currentRoles = await userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                    return BadRequest(new { success = false, message = string.Join("، ", removeResult.Errors.Select(e => e.Description)) });
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                    return BadRequest(new { success = false, message = string.Join("، ", addResult.Errors.Select(e => e.Description)) });
            }

            var updatedRoles = await userManager.GetRolesAsync(user);
            return Json(new { success = true, roles = updatedRoles });
        }

        public IActionResult GetUserRoles(string userName)
        {
            var user = userManager.FindByNameAsync(userName).Result;
            if (user == null) return NotFound();
            var roles = userManager.GetRolesAsync(user).Result;
            return Json(roles);
        }
    }
}