using System;

namespace uMarket.Models.Chat
{
    public class ConversationSummaryDto
    {
        public Guid ConversationId { get; set; }
        public string? Title { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastMessageAt { get; set; }
        public string? OtherUserId { get; set; }
        public string? OtherFullName { get; set; }
    }
}
