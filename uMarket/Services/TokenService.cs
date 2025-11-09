using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace uMarket.Services
{
    public class TokenService
    {
        public async Task<string?> GetDisplayNameFromStoredTokenAsync()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync("access_token");
                if (string.IsNullOrWhiteSpace(token))
                    return null;

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return null;

                var jwt = handler.ReadJwtToken(token);

                // Prefer institutionalId claim if present
                var inst = jwt.Claims.FirstOrDefault(c => string.Equals(c.Type, "institutionalId", StringComparison.OrdinalIgnoreCase))?.Value;
                if (!string.IsNullOrEmpty(inst))
                    return inst;

                // Common name claim types
                var nameClaim = jwt.Claims.FirstOrDefault(c =>
                string.Equals(c.Type, System.Security.Claims.ClaimTypes.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Type, "name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Type, "given_name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.Type, "nickname", StringComparison.OrdinalIgnoreCase));

                if (nameClaim != null && !string.IsNullOrEmpty(nameClaim.Value))
                    return nameClaim.Value;

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
