using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace CRM.EndPoint.FrontEnd.Services.SignalR;

[Authorize(Policy = "NotCustomerOnly")]
public class CallHub : Hub;