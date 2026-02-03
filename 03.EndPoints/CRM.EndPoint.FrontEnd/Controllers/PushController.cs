using System.Security.Claims;
using System.Threading.Tasks;
using CRM.Core.Domain.AppUsers.Entities;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models;
using CRM.EndPoint.FrontEnd.Models.ViewModel.PushNotification;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebPush;

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