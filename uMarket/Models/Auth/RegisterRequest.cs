namespace uMarket.Models.Auth
{
 public class RegisterRequest
 {
 public int InstitutionalId { get; set; }
 public string? FullName { get; set; }
 public string? Password { get; set; }
 public string? Role { get; set; }
 }
}
