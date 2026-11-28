using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Core.ApplicationServices.Abstractions.Cqrs;
using CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmployeeProfile;
using CRM.Core.ApplicationServices.HumanResources.Commands.SaveEmploymentContract;
using CRM.Core.ApplicationServices.HumanResources.Queries.GetEmployeeDirectory;
using CRM.Core.ApplicationServices.Security;
using CRM.Core.Domain.HumanResources.Repositories;
using CRM.EndPoint.FrontEnd.Models.HumanResources;
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
        int contractPage = 1, CancellationToken ct = default) =>
        View(await dispatcher.QueryAsync(new GetEmployeeDirectoryQuery(search, page, employeeId, contractPage), ct));

    [HttpPost]
    public async Task<IActionResult> SaveEmployee(EmployeeProfileForm input, CancellationToken ct)
    {
        if (!ModelState.IsValid || !input.TryGetDetails(out var details))
        {
            TempData["HrEmployeeMessage"] = "اطلاعات واردشده یا تاریخ تولد معتبر نیست.";
            TempData["HrEmployeeSuccess"] = false;
            return RedirectToAction(nameof(Index), new { employeeId = input.Id });
        }
        var result = await dispatcher.CommandAsync(new SaveEmployeeProfileCommand(input.Id, input.PersonnelCode,
            input.FirstName, input.LastName, input.AppUserId, input.RowVersion, details), ct);
        if (result.Status == EmployeeSaveStatus.Denied) return Forbid();
        if (result.Status == EmployeeSaveStatus.NotFound) return NotFound();
        TempData["HrEmployeeMessage"] = result.Status switch
        {
            EmployeeSaveStatus.Saved => "اطلاعات پرسنلی ذخیره شد.",
            EmployeeSaveStatus.Conflict => "کد پرسنلی، کد ملی یا حساب تکراری است، یا اطلاعات هم‌زمان تغییر کرده است. دوباره بررسی کنید.",
            _ => "اطلاعات پرسنلی معتبر نیست."
        };
        TempData["HrEmployeeSuccess"] = result.Status == EmployeeSaveStatus.Saved;
        return RedirectToAction(nameof(Index), new { employeeId = result.Id ?? input.Id });
    }
    [HttpPost]
    public async Task<IActionResult> SaveContract(EmploymentContractForm input, CancellationToken ct)
    {
        if (!ModelState.IsValid || !input.TryGetChange(out var change))
        {
            TempData["HrEmployeeMessage"] = "اطلاعات قرارداد یا تاریخ‌ها معتبر نیست.";
            TempData["HrEmployeeSuccess"] = false;
            return RedirectToAction(nameof(Index), new { employeeId = input.EmployeeId });
        }
        var result = await dispatcher.CommandAsync(new SaveEmploymentContractCommand(change), ct);
        if (result.Status == ContractSaveStatus.Denied) return Forbid();
        if (result.Status == ContractSaveStatus.NotFound) return NotFound();
        TempData["HrEmployeeMessage"] = result.Status switch
        {
            ContractSaveStatus.Saved => "عملیات قرارداد با موفقیت ثبت شد.",
            ContractSaveStatus.Conflict => "شماره قرارداد تکراری است یا اطلاعات هم‌زمان تغییر کرده است؛ دوباره بررسی کنید.",
            ContractSaveStatus.Overlap => "بازه قرارداد با قرارداد تأییدشده دیگری هم‌پوشانی دارد.",
            ContractSaveStatus.InvalidPredecessor => "ارتباط با قرارداد قبلی یا تمدید موجود معتبر نیست.",
            _ => "اطلاعات یا وضعیت قرارداد برای این عملیات معتبر نیست."
        };
        TempData["HrEmployeeSuccess"] = result.Status == ContractSaveStatus.Saved;
        return RedirectToAction(nameof(Index), new { employeeId = input.EmployeeId });
    }
}
