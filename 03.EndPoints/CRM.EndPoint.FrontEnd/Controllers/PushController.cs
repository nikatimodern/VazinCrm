using System.Threading.Tasks;
using CRM.EndPoint.FrontEnd.ISerives;
using CRM.EndPoint.FrontEnd.Models;
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
        await pushService.SubscribeAsync(model);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> TestPush()
    {
        await pushService.SendTestAsync();
        return Ok("Push sent");
    }

}