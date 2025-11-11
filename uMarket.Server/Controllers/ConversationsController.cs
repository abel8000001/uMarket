using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using uMarket.Server.Data;
using uMarket.Server.Models.Chat;
using uMarket.Models.Chat;

namespace uMarket.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ConversationsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyConversations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var convs = await _db.Conversations
                .Where(c => c.Participants.Any(p => p.UserId == userId) && !c.IsClosed)
                .Select(c => new
                {
                    ConversationId = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.Messages.OrderByDescending(m => m.SentAt).Select(m => (DateTimeOffset?)m.SentAt).FirstOrDefault(),
                    OtherParticipantId = c.Participants.Where(p => p.UserId != userId).Select(p => p.UserId).FirstOrDefault()
                })
                .ToListAsync();

            var result = new List<ConversationSummaryDto>();
            foreach (var c in convs)
            {
                string? otherName = null;
                if (!string.IsNullOrEmpty(c.OtherParticipantId))
                {
                    otherName = await _db.Users.Where(u => u.Id == c.OtherParticipantId).Select(u => u.FullName).FirstOrDefaultAsync();
                }

                result.Add(new ConversationSummaryDto
                {
                    ConversationId = c.ConversationId,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.LastMessageAt,
                    OtherUserId = c.OtherParticipantId,
                    OtherFullName = otherName
                });
            }

            return Ok(result);
        }

        [Authorize]
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> GetConversation(Guid conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var isParticipant = await _db.ConversationParticipants.FindAsync(conversationId, userId) != null;
            if (!isParticipant) return Forbid();

            var conversation = await _db.Conversations.FindAsync(conversationId);
            if (conversation == null) return NotFound();

            return Ok(new { ConversationId = conversation.Id, IsClosed = conversation.IsClosed, ClosedAt = conversation.ClosedAt });
        }

        [Authorize]
        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> GetMessages(
    Guid conversationId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 50)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var isParticipant = await _db.ConversationParticipants.FindAsync(conversationId, userId) != null;
            if (!isParticipant) return Forbid();

            var msgs = await _db.Database
                .SqlQuery<ChatMessageDto>($@"
                    EXEC dbo.sp_GetConversationMessages_Paged 
                        @ConversationId = {conversationId}, 
                        @Page = {page}, 
                        @PageSize = {pageSize}")
                .ToListAsync();

            return Ok(msgs);
        }
    }
}
