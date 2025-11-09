using Microsoft.AspNetCore.Identity;

namespace uMarket.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        // InstitutionalId used as numeric username
        public int? InstitutionalId { get; set; }

        // Full name for display
        public string? FullName { get; set; }

        // Whether seller is available to accept chat requests
        public bool IsAvailable { get; set; }

        // Short description of what the seller is currently selling
        public string? CurrentDescription { get; set; }
    }
}
