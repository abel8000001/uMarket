using System;

namespace uMarket.Server.Models.Chat
{
    public enum ChatRequestStatus { Pending = 0, Accepted = 1, Denied = 2 }

    public class ChatRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FromUserId { get; set; } = null!; // student
        public string ToUserId { get; set; } = null!; // seller
        public ChatRequestStatus Status { get; set; } = ChatRequestStatus.Pending;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public Guid? ConversationId { get; set; }
    }
}
