using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services.SignalR
{
    [Authorize]
    public class TaskHub : Hub
    {
        // ذخیره‌ی ConnectionIdها بر اساس UserId
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) { Context.Abort(); return; }
            _connections[userId] = Context.ConnectionId; // ذخیره


            var roles = Context.User?
                .Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    // هر Role = یک Group
                    await Groups.AddToGroupAsync(Context.ConnectionId, role);
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
                ((System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, string>>)_connections)
                    .Remove(new(userId, Context.ConnectionId));

            var roles = Context.User?
                .Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            if (roles != null)
            {
                foreach (var role in roles)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, role);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string? GetConnectionId(string userId)
        {
            _connections.TryGetValue(userId, out var connectionId);
            return connectionId;
        }
    }
}
