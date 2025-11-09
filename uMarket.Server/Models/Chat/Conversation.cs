using System;
using System.Collections.Generic;

namespace uMarket.Server.Models.Chat
{
    public class Conversation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Title { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsClosed { get; set; }
        public DateTimeOffset? ClosedAt { get; set; }

        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
