using System;

namespace uMarket.Models.Chat
{
    public class ChatMessageDto
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
    }
}
