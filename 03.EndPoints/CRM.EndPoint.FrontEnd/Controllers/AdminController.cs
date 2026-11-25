using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.Administration.Dtos;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.Models.Admin;
using CRM.EndPoint.FrontEnd.Models.Login;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminPage;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminUsers;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminUser;
using CRM.Core.ApplicationServices.Administration.Queries.GetAdminUserRoles;
using CRM.Core.ApplicationServices.Administration.Commands.CreateAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.CreateLegacyAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.DeleteAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.UpdateAdminUser;
using CRM.Core.ApplicationServices.Administration.Commands.RemoveAdminUserRole;
using CRM.Core.ApplicationServices.Administration.Commands.UpdateAdminUserRoles;

namespace CRM.EndPoint.FrontEnd.Controllers;
[Authorize(Roles = "مدیر ارشد")]
public class AdminController(ICqrsDispatcher dispatcher) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Head = "Users";
        var roles = await dispatcher.QueryAsync(new GetAdminPageQuery(), ct);
        return View(new AdminViewModel { Users = [], Roles = roles.Select(r => new IdentityRole { Id = r.Id, Name = r.Name }).ToList() });
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(int page = 1, int pageSize = 5, string search = null, string employeeType = "all", string accountStatus = "all", List<string> roles = null, CancellationToken ct = default)
    {
        var result = await dispatcher.QueryAsync(new GetAdminUsersQuery(page, pageSize, search, employeeType, accountStatus, roles), ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Current-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
        return PartialView("_UserCardsPartial", result.Users);
    }

    [HttpPost]
    public async Task<string> CreateOrUpUserSportMan(AppUser model, CancellationToken ct)
    {
        ViewBag.Head = "Users";
        return JsonConvert.SerializeObject(await dispatcher.CommandAsync(new CreateLegacyAdminUserCommand(model), ct));
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        ViewBag.Head = "CreateUser";
        ViewData["Roles"] = await dispatcher.QueryAsync(new GetAdminPageQuery(), ct);
        return View(new CreateModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = string.Join("، ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)) });
        var result = await dispatcher.CommandAsync(new CreateAdminUserCommand(model.UserName, model.PhoneNumber, model.FirstName, model.LastName, model.Email, model.Password), ct);
        if (result.Status != 200)
            return Map(result);
        var user = result.User!;
        return Json(new { success = true, user = new { id = user.Id, userName = user.UserName, firstName = user.FirstName, lastName = user.LastName, phoneNumber = user.PhoneNumber, email = user.Email, internalTels = user.InternalTels } });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) => Map(await dispatcher.CommandAsync(new DeleteAdminUserCommand(id), ct));
    public async Task<IActionResult> Edit(string id, CancellationToken ct)
    {
        var user = await dispatcher.QueryAsync(new GetAdminUserQuery(id), ct);
        return user == null ? RedirectToAction("Index") : View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(AppUser model, List<string> InternalTelsArray, CancellationToken ct)
    {
        var result = await dispatcher.CommandAsync(new UpdateAdminUserCommand(model.Id, model.Email, model.UserName, model.FirstName, model.LastName, model.PhoneNumber, model.PasswordHash, InternalTelsArray), ct);
        if (result.Saved)
            return RedirectToAction("Index");
        foreach (var error in result.Errors)
            ModelState.AddModelError("", error);
        // Prevent a submitted password from being rendered back into the form.
        ModelState.Remove(nameof(AppUser.PasswordHash));
        return View(result.User);
    }

    [HttpPost]
    public async Task<IActionResult> DelUserRol(string id, string role, CancellationToken ct) => Map(await dispatcher.CommandAsync(new RemoveAdminUserRoleCommand(id, role), ct));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUserRoles(string userName, List<string> roleNames, CancellationToken ct)
    {
        var result = await dispatcher.CommandAsync(new UpdateAdminUserRolesCommand(userName, roleNames), ct);
        return result.Status == 200 ? Json(new { success = true, roles = result.Roles }) : Map(result);
    }

    public async Task<IActionResult> GetUserRoles(string userName, CancellationToken ct)
    {
        var roles = await dispatcher.QueryAsync(new GetAdminUserRolesQuery(userName), ct);
        return roles == null ? NotFound() : Json(roles);
    }

    private IActionResult Map(AdminMutationResult result) => result.Status == 200 ? Json(new { success = true }) : StatusCode(result.Status, new { success = false, message = result.Message });
}
