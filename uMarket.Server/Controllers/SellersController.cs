using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using uMarket.Server.Data;
using uMarket.Server.Hubs;
using uMarket.Server.Models.Sellers;

namespace uMarket.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<ChatHub> _hub;

        public SellersController(ApplicationDbContext db, IHubContext<ChatHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSellers()
        {
            var sellers = await _db.Users
            .Where(u => u.IsAvailable)
            .Select(u => new SellerDto { UserId = u.Id, FullName = u.FullName, CurrentDescription = u.CurrentDescription })
            .ToListAsync();

            return Ok(sellers);
        }

        [Authorize]
        [HttpGet("me/status")]
        public async Task<IActionResult> GetMyStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            var dto = new SellerDto 
            { 
                UserId = user.Id, 
                FullName = user.FullName, 
                CurrentDescription = user.CurrentDescription, 
                IsAvailable = user.IsAvailable 
            };

            return Ok(dto);
        }

        public class UpdateSellerRequest
        {
            public bool IsAvailable { get; set; }
            public string? CurrentDescription { get; set; }
        }

        [Authorize]
        [HttpPost("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateSellerRequest req)
        {
            // Console.WriteLine("[SellersController] UpdateMyProfile reached");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.IsAvailable = req.IsAvailable;
            user.CurrentDescription = req.CurrentDescription;

            await _db.SaveChangesAsync();

            // Broadcast update to connected clients so lists refresh in real-time
            var dto = new SellerDto { UserId = user.Id, FullName = user.FullName, CurrentDescription = user.CurrentDescription, IsAvailable = user.IsAvailable };
            try
            {
                await _hub.Clients.All.SendAsync("SellerUpdated", dto);
            }
            catch
            {
                // ignore hub errors for now
            }

            return NoContent();
        }

        [Authorize(Roles = "Vendedor")]
        [HttpGet("requests/pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Use table-valued function - eliminates N+1 query problem
            var requests = await _db.Database
                .SqlQueryRaw<PendingRequest>(
                    "SELECT RequestId, FromUserId, FromFullName, FromUserName, CreatedAt FROM dbo.tvf_GetPendingChatRequests(@UserId)",
                    new SqlParameter("@UserId", userId))
                .ToListAsync();

            return Ok(requests);
        }

        // Inline DTO for pending requests
        private class PendingRequest
        {
            public Guid RequestId { get; set; }
            public string FromUserId { get; set; } = string.Empty;
            public string FromFullName { get; set; } = string.Empty;
            public string? FromUserName { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
        }
    }
}
