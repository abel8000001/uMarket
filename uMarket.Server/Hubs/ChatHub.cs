using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using uMarket.Server.Data;
using uMarket.Server.Models;
using uMarket.Server.Models.Chat;

namespace uMarket.Server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        private string GetUserId()
        {
            var id = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return id ?? string.Empty;
        }

        public override async Task OnConnectedAsync()
        {
            // Ensure connection is added to a group for the authenticated user id so server can target via group
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var groupName = GetUserGroupName(userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var groupName = GetUserGroupName(userId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task RequestChat(string sellerUserId)
        {
            var fromUserId = GetUserId();
            if (string.IsNullOrEmpty(fromUserId) || string.IsNullOrEmpty(sellerUserId))
                return;

            var req = new ChatRequest
            {
                FromUserId = fromUserId,
                ToUserId = sellerUserId,
                Status = ChatRequestStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.ChatRequests.Add(req);
            await _db.SaveChangesAsync();

            // Notify seller (by user group)
            var groupName = GetUserGroupName(sellerUserId);
            await Clients.Group(groupName).SendAsync("ChatRequested", new { RequestId = req.Id, FromUserId = fromUserId, CreatedAt = req.CreatedAt });
        }

        public async Task RespondToRequest(Guid requestId, bool accept)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            if (accept)
            {
                // Use stored procedure for atomic operation
                await _db.Database.ExecuteSqlAsync($@"
                    EXEC dbo.sp_AcceptChatRequest 
                        @ChatRequestId = {requestId}, 
                        @AcceptedBy = {userId}");

                // Get the created conversation ID
                var req = await _db.ChatRequests
                    .Where(r => r.Id == requestId)
                    .Select(r => r.ConversationId)
                    .FirstOrDefaultAsync();

                // Notify requester
                var chatReq = await _db.ChatRequests.FindAsync(requestId);
                if (chatReq != null)
                {
                    var requesterGroup = GetUserGroupName(chatReq.FromUserId);
                    await Clients.Group(requesterGroup).SendAsync("ChatRequestResponded",
                        new { RequestId = requestId, Accepted = true, ConversationId = req });

                    var sellerGroup = GetUserGroupName(userId);
                    await Clients.Group(sellerGroup).SendAsync("ChatRequestResponseSaved",
                        new { RequestId = requestId, Accepted = true, ConversationId = req });
                }
            }
            else
            {
                // Denial doesn't need stored proc
                var req = await _db.ChatRequests.FindAsync(requestId);
                if (req == null || req.ToUserId != userId) return;

                req.Status = ChatRequestStatus.Denied;
                await _db.SaveChangesAsync();

                var requesterGroup = GetUserGroupName(req.FromUserId);
                await Clients.Group(requesterGroup).SendAsync("ChatRequestResponded",
                    new { RequestId = requestId, Accepted = false, ConversationId = (Guid?)null });
            }
        }

        public async Task JoinConversation(Guid conversationId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            // optional: verify participant
            var isParticipant = await _db.ConversationParticipants.FindAsync(conversationId, userId) != null;
            if (!isParticipant) return;

            var groupName = GetGroupName(conversationId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveConversation(Guid conversationId)
        {
            var groupName = GetGroupName(conversationId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendMessage(Guid conversationId, string content)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            // check participant
            var isParticipant = await _db.ConversationParticipants.FindAsync(conversationId, userId) != null;
            if (!isParticipant) return;

            var msg = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                Content = content,
                SentAt = DateTimeOffset.UtcNow
            };

            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();

            var groupName = GetGroupName(conversationId);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", new { MessageId = msg.Id, ConversationId = conversationId, SenderId = userId, Content = content, SentAt = msg.SentAt });
        }

        public async Task EndConversation(Guid conversationId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return;

            var convo = await _db.Conversations.FindAsync(conversationId);
            if (convo == null) return;

            // optional: enforce seller-only end; for now allow participants
            convo.IsClosed = true;
            convo.ClosedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            var groupName = GetGroupName(conversationId);
            await Clients.Group(groupName).SendAsync("ConversationEnded", new { ConversationId = conversationId, ClosedAt = convo.ClosedAt });
        }

        private string GetGroupName(Guid conversationId) => $"conversation:{conversationId}";
        private string GetUserGroupName(string userId) => $"user:{userId}";
    }
}
