using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using uMarket.Server.Data;
using uMarket.Server.Models.Sellers;
using Microsoft.AspNetCore.SignalR;
using uMarket.Server.Hubs;

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

            var requests = await _db.ChatRequests
            .Where(r => r.ToUserId == userId && r.Status == uMarket.Server.Models.Chat.ChatRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                RequestId = r.Id,
                FromUserId = r.FromUserId,
                FromFullName = _db.Users.Where(u => u.Id == r.FromUserId).Select(u => u.FullName).FirstOrDefault(),
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

            return Ok(requests);
        }
    }
}
