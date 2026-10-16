using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

[Authorize(Policy = "NotCustomerOnly")]
[Authorize(Roles = "مدیر فروش,مدیر ارشد,مدیر عامل")]
public class OverdueDealController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
