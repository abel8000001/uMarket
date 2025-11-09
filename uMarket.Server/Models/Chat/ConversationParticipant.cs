using System;

namespace uMarket.Server.Models.Chat
{
    public class ConversationParticipant
    {
        public Guid ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public int Role { get; set; } //0=Student,1=Seller (optional)
    }
}
