using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace project_lifecycle.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        /// <summary>
        /// When a client connects, add them to a group named after their UserId
        /// so we can target messages to specific users.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Join a conversation group so the user receives real-time messages for that conversation.
        /// </summary>
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        }

        /// <summary>
        /// Leave a conversation group.
        /// </summary>
        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conv_{conversationId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
