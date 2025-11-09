using System;

namespace uMarket.Server.Models.Chat
{
    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public string SenderId { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsSystem { get; set; }
        public string? Metadata { get; set; }
    }
}
