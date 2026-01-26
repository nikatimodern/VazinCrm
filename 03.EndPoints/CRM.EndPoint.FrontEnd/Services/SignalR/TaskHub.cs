using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CRM.EndPoint.FrontEnd.Services.SignalR
{
    public class TaskHub : Hub
    {
        // ذخیره‌ی ConnectionIdها بر اساس UserId
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier ?? Context.ConnectionId; // شناسه کاربر
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
            var userId = Context.UserIdentifier ?? Context.ConnectionId;
            _connections.TryRemove(userId, out _);

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
