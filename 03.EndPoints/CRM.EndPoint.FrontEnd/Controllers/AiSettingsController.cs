using CRM.Core.ApplicationServices.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AiSettingsController : Controller
{
    // Read-only landing page until provider configuration and catalog permissions are ready.
    [HttpGet]
    public IActionResult Index() => View();
}
