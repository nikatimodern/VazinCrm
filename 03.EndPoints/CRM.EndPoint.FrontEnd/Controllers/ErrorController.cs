using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class ErrorController : Controller {
    [Route("Error/{statusCode:int}")]
    public IActionResult Index(int statusCode)
    {
        Response.StatusCode = statusCode;

        ViewBag.StatusCode = statusCode;
        ViewBag.ErrorMessage = HttpContext.Items["ErrorMessage"];

        var reExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

        ViewBag.OriginalPath = reExecuteFeature?.OriginalPath;

        return View();
    }
}