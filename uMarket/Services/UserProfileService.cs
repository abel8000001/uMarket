using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using uMarket.Models.Auth;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace uMarket.Services
{
    public class UserProfileService
    {
        const string TokenKey = "access_token";
        const string FullNameKey = "full_name";
        const string InstitutionalIdKey = "institutional_id";
        const string RolesKey = "roles_json";
        const string UserIdKey = "user_id";

        public async Task SaveProfileAsync(AuthResponse auth)
        {
            if (auth == null) throw new ArgumentNullException(nameof(auth));

            try
            {
                if (!string.IsNullOrEmpty(auth.Token))
                    await SecureStorage.Default.SetAsync(TokenKey, auth.Token);

                if (!string.IsNullOrEmpty(auth.FullName))
                    await SecureStorage.Default.SetAsync(FullNameKey, auth.FullName);

                if (auth.InstitutionalId.HasValue)
                    await SecureStorage.Default.SetAsync(InstitutionalIdKey, auth.InstitutionalId.Value.ToString());

                if (!string.IsNullOrEmpty(auth.UserId))
                    await SecureStorage.Default.SetAsync(UserIdKey, auth.UserId);

                if (auth.Roles != null)
                {
                    var json = JsonSerializer.Serialize(auth.Roles);
                    await SecureStorage.Default.SetAsync(RolesKey, json);
                }
            }
            catch
            {
                // ignore storage errors in this simple service; callers may choose to surface errors
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try { return await SecureStorage.Default.GetAsync(TokenKey); }
            catch { return null; }
        }

        public async Task<string?> GetUserIdAsync()
        {
            try 
            { 
                // Try direct storage first
                var userId = await SecureStorage.Default.GetAsync(UserIdKey);
                if (!string.IsNullOrEmpty(userId))
                    return userId;

                // Fallback: parse from token
                var token = await GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                    return null;

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return null;

                var jwt = handler.ReadJwtToken(token);
                var claim = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "sub");
                return claim?.Value;
            }
            catch { return null; }
        }

        public async Task<string?> GetFullNameAsync()
        {
            try { return await SecureStorage.Default.GetAsync(FullNameKey); }
            catch { return null; }
        }

        public async Task<int?> GetInstitutionalIdAsync()
        {
            try
            {
                var v = await SecureStorage.Default.GetAsync(InstitutionalIdKey);
                if (int.TryParse(v, out var id)) return id;
            }
            catch { }
            return null;
        }

        public async Task<string[]?> GetRolesAsync()
        {
            try
            {
                var json = await SecureStorage.Default.GetAsync(RolesKey);
                if (string.IsNullOrEmpty(json)) return null;
                return JsonSerializer.Deserialize<string[]>(json);
            }
            catch { return null; }
        }

        public async Task ClearAsync()
        {
            try
            {
                SecureStorage.Default.Remove(TokenKey);
                SecureStorage.Default.Remove(FullNameKey);
                SecureStorage.Default.Remove(InstitutionalIdKey);
                SecureStorage.Default.Remove(UserIdKey);
                SecureStorage.Default.Remove(RolesKey);
                await Task.CompletedTask;
            }
            catch { }
        }
    }
}
