using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CRM.EndPoint.FrontEnd.Services.SignalR;

// Persisted chat messages are sent by ChatController to verified participants.
// This legacy hub must not broadcast arbitrary client-supplied messages.
[Authorize]
public class ChatHub : Hub;
