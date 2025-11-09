using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using uMarket.Models.Sellers;
using System.Diagnostics;

namespace uMarket.Services
{
    public class SellersService
    {
        private readonly HttpClient _http;

        public SellersService(string baseUrl)
        {
            // Ensure BaseAddress ends with a slash for consistent combination
            var b = baseUrl?.TrimEnd('/') + "/";
            _http = new HttpClient { BaseAddress = new Uri(b), Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<List<SellerDto>> GetAvailableSellersAsync()
        {
            try
            {
                var res = await _http.GetFromJsonAsync<List<SellerDto>>("/api/sellers/available");
                return res ?? new List<SellerDto>();
            }
            catch
            {
                return new List<SellerDto>();
            }
        }

        public async Task<SellerDto?> GetMyStatusAsync(string token)
        {
            try
            {
                using var client = new HttpClient { BaseAddress = _http.BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                Debug.WriteLine("[SellersService] Getting seller status...");
                var res = await client.GetFromJsonAsync<SellerDto>("/api/sellers/me/status");
                Debug.WriteLine($"[SellersService] Status retrieved: IsAvailable={res?.IsAvailable}");
                return res;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellersService] GetMyStatus error: {ex}");
                return null;
            }
        }

        public async Task<List<PendingRequestDto>> GetPendingRequestsAsync(string token)
        {
            try
            {
                using var client = new HttpClient { BaseAddress = _http.BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var res = await client.GetFromJsonAsync<List<PendingRequestDto>>("/api/sellers/requests/pending");
                return res ?? new List<PendingRequestDto>();
            }
            catch
            {
                return new List<PendingRequestDto>();
            }
        }

        public async Task<(bool ok, string? error)> UpdateMyProfileAsync(string token, UpdateSellerRequest req)
        {
            try
            {
                using var client = new HttpClient { BaseAddress = _http.BaseAddress, Timeout = TimeSpan.FromSeconds(30) };
                if (!string.IsNullOrEmpty(token))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Build the absolute request URI so we can log it and include it in errors
                var requestUri = new Uri(client.BaseAddress, "api/sellers/me").ToString();
                Debug.WriteLine($"[SellersService] POST {requestUri}");

                var res = await client.PostAsJsonAsync("/api/sellers/me", req);
                if (res.IsSuccessStatusCode) return (true, null);

                var content = await res.Content.ReadAsStringAsync();
                var message = $"{(int)res.StatusCode} {res.ReasonPhrase} -> {requestUri}: {content}";
                Debug.WriteLine($"[SellersService] UpdateMyProfile failed: {message}");
                return (false, message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellersService] UpdateMyProfile exception: {ex}");
                return (false, ex.Message);
            }
        }
    }
}
