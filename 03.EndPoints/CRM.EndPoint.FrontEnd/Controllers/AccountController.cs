using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Models.ViewModel;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Login;
using CRM.EndPoint.FrontEnd.Services;
using Generics.Infrastructures.Sql;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.Services.SignalR;
using Microsoft.AspNetCore.SignalR;
using CRM.Core.Domain.LogSystems.Entities;
using CRM.EndPoint.FrontEnd.Models.Sms;
using CRM.EndPoint.FrontEnd.Models.ViewModel.Customer;
using CRM.EndPoint.FrontEnd.Services.SmsServices;

namespace CRM.EndPoint.FrontEnd.Controllers {
    public class AccountController : Controller {
        private readonly IConfiguration configuration;
        private readonly FileSaver fileSaver;
        private readonly UserContextDb UDb;
        private readonly UserManager<AppUser> userManager;
        private readonly SignInManager<AppUser> signInManager;
        private readonly IHubContext<TaskHub> hub;
        private readonly SmsService smsServ;
        private readonly DateTimeServices dtS;
        private readonly Service genService;
        private readonly CRMContextDb db;
        private readonly LogSystemService _logSystemService;
        private readonly ISmsQueue smsQueue;

        public AccountController(IConfiguration configuration, FileSaver fileSaver, UserContextDb UDb,
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager, IHubContext<TaskHub> hub, SmsService SmsServ, DateTimeServices dtS,
            Service GenService, CRMContextDb db, LogSystemService logSystemService, ISmsQueue smsQueue)
        {
            this.configuration = configuration;
            this.fileSaver = fileSaver;
            this.UDb = UDb;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.hub = hub;
            smsServ = SmsServ;
            this.dtS = dtS;
            genService = GenService;
            this.db = db;
            _logSystemService = logSystemService;
            this.smsQueue = smsQueue;
        }


        [AllowAnonymous]
        public IActionResult Login(string returnUrl, bool accesDenid)
        {
            if (accesDenid)
            {
                ViewBag.AccesDe = "ورود به صفحه لاگین به دلیل عدم دسترسی به صفحه مورد نظر";
            }

            var usr = userManager.GetUserAsync(HttpContext.User);

            if (usr.Result == null)
            {
                ViewBag.returnUrl = returnUrl;
                return View();
            }

            return RedirectToAction("index", "Dashboard");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginModel details)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return StatusCode(405, new { errors });
            }


            var user = await userManager.FindByNameAsync(details.UserName);
            if (user != null)
            {
                await signInManager.SignOutAsync();
                //var tryCount = user.AccessFailedCount;
                //bool lockUser = tryCount >= 5;
                var result = await signInManager.PasswordSignInAsync(user, details.Password, false, true);

                if (result.Succeeded)
                {
                    var url = await LoginSuccess(details.returnUrl, user);
                    return Ok(new { message = "موفق", url });
                }

                if (result.IsLockedOut)
                {
                    return StatusCode(403, new { message = "حساب کاربری شما غیرفعال شده است." });
                }
            }

            return NotFound(new { message = "نام کاربری یا رمز عبور اشتباه است!" });
        }


        public async Task<IActionResult> Home()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            if (user == null)
                return RedirectToAction("OtpLogin");

            if (user.IsEmployee)
                return RedirectToAction("Index", "Dashboard");


            var customers = await db.Customers.Where(f => f.Mobile == user.PhoneNumber).Select(f => new CustomerPanelVModel {
                CustomerId = f.Id,
                CustomerName = string.IsNullOrWhiteSpace(f.CompanyName) ? $"{f.AgentName}" : $"{f.CompanyName} ({f.AgentName})"
            }).ToListAsync();

            return View(customers);
        }

        private async Task<string> LoginSuccess(string returnUrl, AppUser user)
        {
            await signInManager.SignInAsync(user, new AuthenticationProperties {
                AllowRefresh = true,
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.Now.AddHours(1)
            });

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

            var ua = Request.Headers.UserAgent.ToString().ToLower();

            var device = ua.Contains("mobi") || ua.Contains("iphone") || ua.Contains("android") ? "Mobile"
                : ua.Contains("ipad") || ua.Contains("tablet") ? "Tablet" : "Desktop";

            var os = ua.Contains("windows") ? "Windows"
                : ua.Contains("android") ? "Android"
                : ua.Contains("iphone") ? "iOS"
                : ua.Contains("mac") ? "macOS"
                : ua.Contains("linux") ? "Linux" : "Unknown";

            var browser = ua.Contains("edg") ? "Edge"
                : ua.Contains("samsungbrowser") ? "SamsungBrowser"
                : ua.Contains("chrome") ? "Chrome"
                : ua.Contains("safari") ? "Safari"
                : ua.Contains("firefox") ? "Firefox"
                : ua.Contains("opr") ? "Opera" : "Unknown";

            await _logSystemService.LogChange(user, null, LogActionEnum.ورود_کاربر, user, null,
                $"login: {user.UserName}, {device}, {os}, {browser}, {ip}");
            await db.SaveChangesAsync();

            string url;
            var userLogin = await userManager.GetUserAsync(HttpContext.User);
            if (userLogin.IsEmployee)
            {
                url = string.IsNullOrEmpty(returnUrl) ? Url.Action("Index", "Dashboard") : returnUrl;
            } else
            {
                url = Url.Action("Home");
            }

            return url;

            /*if (!string.IsNullOrEmpty(returnUrl))
            {
                url = returnUrl;
            } else if (await userManager.IsInRoleAsync(userLogin, "مشتری") || await userManager.IsInRoleAsync(userLogin, "تامین کننده"))
            {
                url = Url.Action("CustomerProfile", "Customer") + "?Id=" + userLogin.PersonId;
            } else
            {
                url = Url.Action("Index", "Dashboard");
            }*/
        }


        ///////////////////////////////////OTP LOGIN & RESET PASSWORD//////////////////////////////////

        [HttpGet]
        public async Task<IActionResult> OtpLogin(string type = "Login")
        {
            var user = await userManager.GetUserAsync(HttpContext.User);

            if (user != null)
            {
                if (user.IsEmployee)
                    return RedirectToAction("index", "Dashboard");

                return RedirectToAction("Home");
            }

            ViewBag.type = type;
            return View();
        }


        private async Task SendOtpAsync(string mobile, string type)
        {
            var otp = new Random().Next(1000, 9999).ToString();

            //await smsServ.SendOtpSms(mobile, otp, type);
            await smsQueue.QueueAsync(new SmsJob {
                Type = type == "Forgot" ? SmsType.OtpForgot : SmsType.OtpLogin,
                Mobile = mobile,
                Code = otp
            });

            HttpContext.Session.SetString($"{type}IsExpireOtp", "false");
            HttpContext.Session.SetString($"{type}OtpCode", otp);
            HttpContext.Session.SetString($"{type}OtpStartTime", DateTime.Now.ToString("O"));
            HttpContext.Session.SetString("Phone", mobile);
        }

        private async Task<string> LoginAfterOtp(AppUser user, string type)
        {
            string url;
            switch (type)
            {
                case "Forgot":
                    url = Url.Action("ResetPassword", "Account");
                    HttpContext.Session.SetString("VerifiedOtp", "true");
                    HttpContext.Session.SetString("UserId", user.Id);
                    break;

                /*case "Chat":
                    HttpContext.Session.Clear();
                    url = await LoginSuccess(Url.Action("Main", "Chat"), user);
                    break;*/

                default: // type == "Login"
                    HttpContext.Session.Clear();
                    url = await LoginSuccess(null, user);
                    break;
            }

            return url;
        }

        [HttpPost]
        public async Task<IActionResult> RequestOtp([FromBody] LoginOtpRequest model, [FromQuery] string type = "Login")
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
                return Ok(new { status = 405, error });
            }

            /*var anyUsers = await userManager.Users.AnyAsync(u => u.PhoneNumber == model.Mobile);
            var anyCustomer = await db.Customers.AnyAsync(f => f.Mobile == model.Mobile);
            if (!anyUsers && !anyCustomer && type != "Chat")
            {
                return Ok(new { status = 404, message = "کاربری با این شماره یافت نشد." });
            }*/

            await SendOtpAsync(model.Mobile, type);
            return Ok(new { status = 200, message = "کد یکبار مصرف ارسال شد" });
        }

        [HttpGet]
        public async Task<IActionResult> SelectAccount(string userId, string type = "Login")
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return Ok(new { status = 404, message = "کاربر یافت نشد" });


            var url = await LoginAfterOtp(user, type);

            return Ok(new { status = 200, message = "موفق", url });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtpGeneric([FromBody] VerifyOtpViewModel model, [FromQuery] string type = "Login")
        {
            var correctOtp = HttpContext.Session.GetString($"{type}OtpCode");

            if (HttpContext.Session.GetString($"{type}IsExpireOtp") == "true")
                return Ok(new { status = 401, message = "کد منقضی شده است" });

            if (model.OtpCode != correctOtp)
                return Ok(new { status = 403, message = "کد نادرست است" });


            var users = await userManager.Users.Where(u => u.PhoneNumber == model.Mobile).ToListAsync();
            if (users.Count == 0)
                users = await userManager.Users.Where(u => u.UserName == model.Mobile).ToListAsync();

            if (users.Count == 0)
            {
                var customer = await db.Customers.Where(f => f.Mobile == model.Mobile).FirstOrDefaultAsync();
//                if (customer != null || type == "Chat")
                if (true)
                {
                    var newUser = new AppUser {
                        UserName = model.Mobile,
                        PhoneNumber = model.Mobile,
                        FirstName = customer?.CompanyName,
                        LastName = customer?.AgentName ?? model.Mobile,
                        PersonId = customer?.Id,
                    };
                    var result = await userManager.CreateAsync(newUser);
                    if (result.Succeeded)
                    {
//                        if (type == "Chat")
                        {
                            var mainChats = await db.MainChats.Where(f => f.ToUserId == model.Mobile).ToListAsync();
                            foreach (var item in mainChats)
                            {
                                item.ToUserId = newUser.Id;
                            }
                        }

                        foreach (var role in customer?.RoleTypeTxt?.Split(" ", StringSplitOptions.RemoveEmptyEntries) ?? [])
                        {
                            await userManager.AddToRoleAsync(newUser, role);
                        }

                        await _logSystemService.LogChange(newUser, null, LogActionEnum.ثبت, newUser, null, customer?.RoleTypeTxt);


                        users.Add(newUser);
                    } else
                    {
                        var errors = result.Errors.Select(e =>
                            e.Code == "DuplicateUserName" ? $"نام کاربری '{newUser.UserName}' قبلاً ثبت شده است." : e.Description
                        ).ToList();

                        return Ok(new { status = 405, errors });
                    }
                } else
                {
                    return Ok(new { status = 404, message = "کاربری با این شماره یافت نشد." });
                }
            }

            if (users.Count > 1)
            {
                var accounts = users.Select(u => new { u.Id, u.UserName, u.FullName });
                return Ok(new { status = 300, message = "چند حساب یافت شد. لطفا یکی را انتخاب کنید.", accounts });
            }

            var user = users.First();
            var url = await LoginAfterOtp(user, type);

            return Ok(new { status = 200, message = "موفق", url });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult CheckTimerCode(string type)
        {
            if (HttpContext.Session.GetString($"{type}OtpStartTime") != null)
            {
                var otpTime = DateTime.Parse(HttpContext.Session.GetString($"{type}OtpStartTime"));
                CheckTimeModel timerState = dtS.CheckTime(otpTime);
                if (timerState?.ElsapsTime == "0")
                    HttpContext.Session.SetString($"{type}IsExpireOtp", "true");

                return Ok(timerState);
            }

            return Ok(new { status = 201, message = "خطای سرور", data = "" });
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var phone = HttpContext.Session.GetString("Phone");
            var userId = HttpContext.Session.GetString("UserId");
            var verified = HttpContext.Session.GetString("VerifiedOtp");
            ViewBag.PhoneNumber = phone;

            if (string.IsNullOrEmpty(phone) || verified != "true" || string.IsNullOrEmpty(userId))
                return RedirectToAction("OtpLogin");

            return View(new ResetPasswordModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Ok(new { status = 405, errors });
            }

            string url;
            var userId = HttpContext.Session.GetString("UserId");
            var phone = HttpContext.Session.GetString("Phone");
            if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(userId))
                return Ok(new { status = 404, message = "کاربر یافت نشد" });

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                url = Url.Action("OtpLogin", "Account");
                return Ok(new { status = 404, message = "کاربری با این شماره پیدا نشد", url });
            }


            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                return Ok(new {
                    status = 405,
                    errors = result.Errors.Select(e => e.Description).ToList()
                });
            }

            await _logSystemService.LogChange(user, null, LogActionEnum.ویرایش, user, null, "بازیابی رمز عبور");
            await db.SaveChangesAsync();
            HttpContext.Session.Clear();

            url = await LoginSuccess(null, user);
            return Ok(new { status = 200, message = "رمز عبور با موفقیت تغییر یافت", url });
        }
        /////////////////END///////////////////OTP LOGIN & RESET PASSWORD/////////////////END/////////////////


        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetUserLogin()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var roles = await userManager.GetRolesAsync(user);

            var roleMap = new Dictionary<string, string> {
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
                ["تنخواه دار"] = "TanKhah",
            };

            var model = new GetUserLogin {
                Id = user.Id,
                FirstName = user.FirstName?.Trim(),
                LastName = user.LastName?.Trim(),
                PersonImageUrl = user.PersonImageUrl,
                RollName = roles.FirstOrDefault(),
                RollNames = roles.Select(r => new RoleInfo { Name = roleMap.GetValueOrDefault(r, r), FaName = r }).ToList(),
                PhoneNumber = user.PhoneNumber,
                PersonImage = user.PersonImage
            };

            return Ok(model);
        }

        [Authorize]
        [HttpPost]
        public async Task UpPersonSetting(UpPersonSettingViewModel model)
        {
            var Usr = await userManager.GetUserAsync(HttpContext.User);
            var data = UDb.Users.FirstOrDefault(f => f.Id == Usr.Id);
            var ii = data.PersonImageUrl;
            switch (model.imageStatus)
            {
                case 0:
                    //data.PersonImage = null;
                    //data.PersonImageUrl = null;
                    break;
                case 1:
                    data.PersonImage = null;
                    data.PersonImageUrl = null;
                    break;
                case 2:
                    data.PersonImage = fileSaver.CompressImage(model.PersonImage);
                    data.PersonImageUrl =
                        fileSaver.SaveImagePersonLogin(model.PersonImage, data.Id, data.PersonImageUrl);
                    break;
            }

            data.FirstName = model.FirstName;
            data.LastName = model.LastName;
            data.PhoneNumber = model.PhoneNumber;
            await UDb.SaveChangesAsync();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetUsers(int? type = 2)
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            List<SelectOptionIdDto> data = null;

            switch (type)
            {
                case 1:
                    data = await UDb.Users
                        .Where(u => u.IsEmployee)
                        .Select(g => new SelectOptionIdDto {
                            id = Guid.Parse(g.Id),
                            text = g.FullName
                        })
                        .ToListAsync();
                    break;

                case 2:
                    data = userManager.GetUsersInRoleAsync("کارشناس فروش").Result
                        .Select(g => new SelectOptionIdDto() {
                            id = Guid.Parse(g.Id),
                            text = g.FullName
                        }).ToList();
                    break;
            }

            var item = data.FirstOrDefault(f => f.id == Guid.Parse(user.Id));
            if (item != null)
            {
                item.Target = true;
            }


            return Ok(data);
        }


        public async Task<IActionResult> Logout()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _logSystemService.LogChange(user, null, LogActionEnum.خروج_کاربر, user, null, $"logout: {user.UserName}, {ip}");
            await db.SaveChangesAsync();

            await signInManager.SignOutAsync();
            return RedirectToAction("OtpLogin", "Account");
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> AccessDenied(string ReturnUrl, bool showNotifyOnly = false)
        {
            var user = userManager.GetUserAsync(HttpContext.User).Result;
            if (showNotifyOnly)
            {
                await hub.Clients.User(user.Id).SendAsync("ShowAccessDeniedMessage", "شما به این صفحه دسترسی ندارید!");
                return Ok();
            } else
            {
                await signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account", new { returnUrl = ReturnUrl, accesDenid = true });
            }
        }

        public async Task<IActionResult> ToggleUserLockout(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                user.LockoutEnd = null;
            } else
            {
                user.LockoutEnd = DateTime.UtcNow.AddYears(300);
            }

            await userManager.UpdateSecurityStampAsync(user);

            var result = await userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(result.Errors);
        }
    }
}