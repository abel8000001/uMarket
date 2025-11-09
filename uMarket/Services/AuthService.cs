using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using uMarket.Models.Auth;
using System.Text.Json;
using System.Diagnostics;

namespace uMarket.Services
{
    public class AuthService
    {
        private readonly HttpClient _http;

        public AuthService(HttpClient http)
        {
            _http = http;
        }

        public async Task<AuthResponse?> LoginAsync(int institutionalId, string password)
        {
            var req = new LoginRequest { InstitutionalId = institutionalId, Password = password };

            try
            {
                using var res = await _http.PostAsJsonAsync("api/auth/login", req).ConfigureAwait(false);

                var content = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[AuthService] Login response status: {(int)res.StatusCode} - {res.StatusCode}");
                Debug.WriteLine($"[AuthService] Login response body: {content}");

                if (!res.IsSuccessStatusCode)
                {
                    return null;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {
                    var auth = JsonSerializer.Deserialize<AuthResponse>(content, options);
                    if (auth != null && !string.IsNullOrEmpty(auth.Token))
                    {
                        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                    }

                    return auth;
                }
                catch (JsonException jex)
                {
                    Debug.WriteLine($"[AuthService] JSON deserialize error: {jex}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthService] Login exception: {ex}");
                return null;
            }
        }

        public async Task<AuthResponse?> RegisterAsync(int institutionalId, string fullName, string password, string role)
        {
            var req = new RegisterRequest { InstitutionalId = institutionalId, FullName = fullName, Password = password, Role = role };

            try
            {
                using var res = await _http.PostAsJsonAsync("api/auth/register", req).ConfigureAwait(false);

                var content = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                Debug.WriteLine($"[AuthService] Register response status: {(int)res.StatusCode} - {res.StatusCode}");
                Debug.WriteLine($"[AuthService] Register response body: {content}");

                if (!res.IsSuccessStatusCode)
                {
                    return null;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {
                    var auth = JsonSerializer.Deserialize<AuthResponse>(content, options);
                    if (auth != null && !string.IsNullOrEmpty(auth.Token))
                    {
                        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                    }

                    return auth;
                }
                catch (JsonException jex)
                {
                    Debug.WriteLine($"[AuthService] JSON deserialize error: {jex}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AuthService] Register exception: {ex}");
                return null;
            }
        }

        // Clear Authorization header (called on logout)
        public void ClearAuthorization()
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization = null;
            }
            catch
            {
                // ignore
            }
        }
    }
}