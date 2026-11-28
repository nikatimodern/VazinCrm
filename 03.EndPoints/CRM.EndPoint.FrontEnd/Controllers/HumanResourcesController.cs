using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmployeeProfile;
using CRM.Core.ApplicationServices.HumanResources.Queries.GetEmployeeDirectory;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.HumanResources.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

// Administrator-only preview. Actual employee/contract access is separately
// enforced by the application handlers and currently denied by the locked adapter.
[Authorize(Roles = AppRoles.Admin)]
[AutoValidateAntiforgeryToken]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class HumanResourcesController(ICqrsDispatcher dispatcher) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string search, int page = 1, Guid? employeeId = null,
        CancellationToken ct = default) =>
        View(await dispatcher.QueryAsync(new GetEmployeeDirectoryQuery(search, page, employeeId), ct));

    [HttpPost]
    public async Task<IActionResult> SaveEmployee(Guid? id, string personnelCode, string firstName,
        string lastName, string appUserId, byte[] rowVersion, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest("اطلاعات واردشده معتبر نیست.");
        var result = await dispatcher.CommandAsync(new SaveEmployeeProfileCommand(id, personnelCode,
            firstName, lastName, appUserId, rowVersion), ct);
        if (result.Status == EmployeeSaveStatus.Denied) return Forbid();
        if (result.Status == EmployeeSaveStatus.NotFound) return NotFound();
        TempData["HrEmployeeMessage"] = result.Status switch
        {
            EmployeeSaveStatus.Saved => "اطلاعات پرسنلی ذخیره شد.",
            EmployeeSaveStatus.Conflict => "کد پرسنلی یا حساب تکراری است، یا اطلاعات هم‌زمان تغییر کرده است. دوباره بررسی کنید.",
            _ => "اطلاعات پرسنلی معتبر نیست."
        };
        TempData["HrEmployeeSuccess"] = result.Status == EmployeeSaveStatus.Saved;
        return RedirectToAction(nameof(Index), new { employeeId = result.Id ?? id });
    }
}
