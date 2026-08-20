using CRM.Core.Domain.AppUsers.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var users = userManager.Users.Where(f => f.OrganAdmin == false).ToList();
            var rols = roleManager.Roles.ToList();
            var model = new AdminViewModel() {
                Roles = rols,
                Users = users
            };

            foreach (var item in model.Users)
            {
                item.RolsUsers = userManager.GetRolesAsync(item).Result;
            }

            return View(model);
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
        public IActionResult Create(CreateModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new AppUser {
                    UserName = model.UserName,
                    PhoneNumber = model.PhoneNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    IsEmployee = true
                };
                IdentityResult result = userManager.CreateAsync(user, model.Password).Result;
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                } else
                {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            return View(model);
        }

        //[HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            AppUser user = await userManager.FindByIdAsync(id);
            if (user != null)
            {
                IdentityResult result = await userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index");
                } else
                {
                    AddErrorsFromResult(result);
                }
            } else
            {
                ModelState.AddModelError("", "User Not Found");
            }

            return Ok();
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

            await userManager.RemoveFromRoleAsync(User, role);


            return RedirectToAction("Index");
        }


        [HttpPost]
        public IActionResult AddUserToRole(string userName, string roleName)
        {
            var user = userManager.FindByNameAsync(userName).Result;
            //var role = roleManager.FindByNameAsync(roleName).Result;

            var result = userManager.AddToRoleAsync(user, roleName).Result;
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }

            return BadRequest();
        }

        public IActionResult GetUserRoles(string userName)
        {
            var user = userManager.FindByNameAsync(userName).Result;
            var roles = userManager.GetRolesAsync(user).Result;
            return Json(roles);
        }
    }
}