using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using uMarket.Server.Models;
using uMarket.Server.Models.Auth;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace uMarket.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _userManager = userManager;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (req == null) return BadRequest();

            var existing = await _userManager.Users.FirstOrDefaultAsync(u => u.InstitutionalId == req.InstitutionalId);
            if (existing != null)
            {
                return Conflict(new { error = "InstitutionalId already registered" });
            }

            var user = new ApplicationUser
            {
                UserName = req.InstitutionalId.ToString(),
                InstitutionalId = req.InstitutionalId,
                FullName = req.FullName,
                Email = null // optional
            };

            var result = await _userManager.CreateAsync(user, req.Password ?? "");
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            var role = (req.Role ?? "Estudiante");
            if (role != "Estudiante" && role != "Vendedor")
            {
                return BadRequest(new { error = "Invalid role" });
            }

            await _userManager.AddToRoleAsync(user, role);

            // Create token and return AuthResponse so client can auto-login
            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            var response = new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60")),
                UserId = user.Id,
                Roles = roles.ToArray(),
                FullName = user.FullName,
                InstitutionalId = user.InstitutionalId
            };

            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null) return BadRequest();

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.InstitutionalId == req.InstitutionalId);
            if (user == null) return Unauthorized();

            var valid = await _userManager.CheckPasswordAsync(user, req.Password ?? "");
            if (!valid) return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60")),
                UserId = user.Id,
                Roles = roles.ToArray(),
                FullName = user.FullName,
                InstitutionalId = user.InstitutionalId
            });
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Missing Jwt:Key")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
             {
             new Claim(JwtRegisteredClaimNames.Sub, user.Id),
             new Claim("institutionalId", user.InstitutionalId?.ToString() ?? ""),
             new Claim(ClaimTypes.Name, user.FullName ?? user.UserName ?? ""),
             new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
             };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var expires = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresMinutes"] ?? "60"));
            var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
