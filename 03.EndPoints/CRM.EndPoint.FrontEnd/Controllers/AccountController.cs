using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Accounts.Dtos;
using CRM.EndPoint.FrontEnd.Models.Customer;
using CRM.EndPoint.FrontEnd.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CRM.Core.ApplicationServices.Accounts.Queries.GetLoginPage;
using CRM.Core.ApplicationServices.Accounts.Queries.GetOtpLoginPage;
using CRM.Core.ApplicationServices.Accounts.Queries.GetAccountHome;
using CRM.Core.ApplicationServices.Accounts.Queries.GetResetPasswordPage;
using CRM.Core.ApplicationServices.Accounts.Commands.PasswordLogin;
using CRM.Core.ApplicationServices.Accounts.Commands.RequestOtp;
using CRM.Core.ApplicationServices.Accounts.Commands.SelectAccount;
using CRM.Core.ApplicationServices.Accounts.Commands.VerifyOtp;
using CRM.Core.ApplicationServices.Accounts.Commands.CheckOtpTimer;
using CRM.Core.ApplicationServices.Accounts.Commands.ResetAccountPassword;
using CRM.Core.ApplicationServices.Accounts.Queries.GetCurrentAccount;
using CRM.Core.ApplicationServices.Accounts.Commands.UpdateAccountProfile;
using CRM.Core.ApplicationServices.Accounts.Queries.GetAccountUsers;
using CRM.Core.ApplicationServices.Accounts.Commands.LogoutAccount;
using CRM.Core.ApplicationServices.Accounts.Commands.HandleAccountAccessDenied;
using CRM.Core.ApplicationServices.Accounts.Commands.ToggleAccountLockout;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class AccountController(ICqrsDispatcher requests) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl, bool accesDenid, CancellationToken cancellationToken)
    {
        var signedIn = await requests.QueryAsync(new GetLoginPageQuery(), cancellationToken);
        if (accesDenid) ViewBag.AccesDe = "ورود به صفحه لاگین به دلیل عدم دسترسی به صفحه مورد نظر";
        if (signedIn) return RedirectToAction("index", "Dashboard");
        ViewBag.returnUrl = returnUrl;
        return View();
    }
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginModel details, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return StatusCode(405, new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
        var result = await requests.CommandAsync(new PasswordLoginCommand(details.UserName, details.Password, details.returnUrl), cancellationToken);
        if (result.Status == 200) return Ok(new { message = result.Message, url = ResolveUrl(result.Redirect) });
        return StatusCode(result.Status, new
        {
            message = result.Message,
            retryAfterSeconds = result.RetryAfterSeconds,
            remainingAttempts = result.RemainingAttempts
        });
    }
    public async Task<IActionResult> Home(CancellationToken cancellationToken)
    {
        var result = await requests.QueryAsync(new GetAccountHomeQuery(), cancellationToken);
        if (result.Redirect is not null) return RedirectToAction(result.Redirect.Action, result.Redirect.Controller);
        return View(result.Customers.Select(c => new CustomerPanelVModel { CustomerId = c.CustomerId, CustomerName = c.CustomerName }).ToList());
    }
    [HttpGet]
    public async Task<IActionResult> OtpLogin(string type = "Login", CancellationToken cancellationToken = default)
    {
        var redirect = await requests.QueryAsync(new GetOtpLoginPageQuery(), cancellationToken);
        if (redirect is not null) return RedirectToAction(redirect.Action, redirect.Controller);
        ViewBag.type = type;
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> RequestOtp([FromBody] LoginOtpRequest model, [FromQuery] string type = "Login", CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid) return Ok(new { status = 405, message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() });
        var result = await requests.CommandAsync(new RequestOtpCommand(model.Mobile, type), cancellationToken);
        return Ok(new { status = result.Status, message = result.Message, retryAfterSeconds = result.RetryAfterSeconds });
    }
    [HttpGet]
    public async Task<IActionResult> SelectAccount(string userId, string type = "Login", CancellationToken cancellationToken = default)
    {
        var result = await requests.CommandAsync(new SelectAccountCommand(userId, type), cancellationToken);
        if (result.Status != 200) return Ok(new { status = result.Status, message = result.Message });
        return Ok(new { status = result.Status, message = result.Message, url = ResolveUrl(result.Redirect) });
    }
    [HttpPost]
    public async Task<IActionResult> VerifyOtpGeneric([FromBody] VerifyOtpViewModel model, [FromQuery] string type = "Login", CancellationToken cancellationToken = default)
    {
        var result = await requests.CommandAsync(new VerifyOtpCommand(model.OtpCode, model.Mobile, type), cancellationToken);
        if (result.Errors is not null) return Ok(new { status = result.Status, errors = result.Errors });
        if (result.Accounts is not null) return Ok(new { status = result.Status, message = result.Message, accounts = result.Accounts });
        if (result.Redirect is not null) return Ok(new { status = result.Status, message = result.Message, url = ResolveUrl(result.Redirect) });
        return Ok(new { status = result.Status, message = result.Message, retryAfterSeconds = result.RetryAfterSeconds, remainingAttempts = result.RemainingAttempts });
    }
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> CheckTimerCode(string type, string? mobile, CancellationToken cancellationToken)
    {
        var result = await requests.CommandAsync(new CheckOtpTimerCommand(type, mobile), cancellationToken);
        return result is null ? Ok(new { status = 201, message = "خطای سرور", data = "" }) : Ok(result);
    }
    [HttpGet]
    public async Task<IActionResult> ResetPassword(CancellationToken cancellationToken)
    {
        var result = await requests.QueryAsync(new GetResetPasswordPageQuery(), cancellationToken);
        ViewBag.PhoneNumber = result.Phone;
        return result.CanReset ? View(new ResetPasswordModel()) : RedirectToAction("OtpLogin");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return Ok(new { status = 405, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList() });
        var result = await requests.CommandAsync(new ResetAccountPasswordCommand(model.NewPassword), cancellationToken);
        if (result.Errors is not null) return Ok(new { status = result.Status, errors = result.Errors });
        if (result.Redirect is not null) return Ok(new { status = result.Status, message = result.Message, url = ResolveUrl(result.Redirect) });
        return Ok(new { status = result.Status, message = result.Message });
    }
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> GetUserLogin(CancellationToken cancellationToken) =>
        Ok(await requests.QueryAsync(new GetCurrentAccountQuery(), cancellationToken));
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UpPersonSetting(UpPersonSettingViewModel model, CancellationToken cancellationToken)
    {
        AccountUpload upload = null;
        if (model.imageStatus == 2 && model.PersonImage is not null)
        {
            using var stream = new MemoryStream();
            await model.PersonImage.CopyToAsync(stream, cancellationToken);
            upload = new(stream.ToArray(), model.PersonImage.FileName, model.PersonImage.ContentType);
        }
        await requests.CommandAsync(new UpdateAccountProfileCommand(model.FirstName, model.LastName, model.PhoneNumber, model.imageStatus, upload), cancellationToken);
        return Ok();
    }
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetUsers(int? type = 2, CancellationToken cancellationToken = default) =>
        Ok(await requests.QueryAsync(new GetAccountUsersQuery(type), cancellationToken));
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await requests.CommandAsync(new LogoutAccountCommand(), cancellationToken);
        return RedirectToAction(result.Action, result.Controller);
    }
    public async Task<IActionResult> AccessDenied(string ReturnUrl, bool showNotifyOnly = false, CancellationToken cancellationToken = default)
    {
        var notified = await requests.CommandAsync(new HandleAccountAccessDeniedCommand(showNotifyOnly), cancellationToken);
        return notified ? Ok() : RedirectToAction("Login", "Account", new { returnUrl = ReturnUrl, accesDenid = true });
    }
    public async Task<IActionResult> ToggleUserLockout(string userId, CancellationToken cancellationToken)
    {
        var result = await requests.CommandAsync(new ToggleAccountLockoutCommand(userId), cancellationToken);
        if (result is null) return NotFound("رکوردی یافت نشد!");
        return result.Succeeded ? Ok() : BadRequest(result.Errors);
    }
    private string ResolveUrl(AccountRedirect redirect) => redirect?.Url ?? Url.Action(redirect.Action, redirect.Controller);
}
