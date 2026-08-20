using System.Security.Claims;
using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.IServices.PushNotification;
using CRM.EndPoint.FrontEnd.Models.PushNotification;
using Microsoft.AspNetCore.Mvc;

namespace CRM.EndPoint.FrontEnd.Controllers;

public class PushController(IPushService pushService) : Controller {

    [HttpGet]
    public IActionResult GetPublicKey()
    {
        return Ok(pushService.GetPublicKey());
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto model)
    {
        var userId = User.Identity?.IsAuthenticated == true
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
        await pushService.SubscribeAsync(model, userId);
        return Ok();
    }

}