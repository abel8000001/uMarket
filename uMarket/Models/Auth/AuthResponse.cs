namespace uMarket.Models.Auth
{
 public class AuthResponse
 {
 public string? Token { get; set; }
 public DateTime ExpiresAt { get; set; }
 public string? UserId { get; set; }
 public string[]? Roles { get; set; }
 public string? FullName { get; set; }
 public int? InstitutionalId { get; set; }
 }
}
