using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using uMarket.Models.Chat;
using System.Diagnostics;
using System.Text.Json;

namespace uMarket.Services
{
    public class ConversationsService
    {
        private readonly HttpClient _http;
        private readonly UserProfileService _profile;

        public ConversationsService(string baseUrl, UserProfileService profile)
        {
            var b = baseUrl?.TrimEnd('/') + "/";
            _http = new HttpClient { BaseAddress = new Uri(b), Timeout = TimeSpan.FromSeconds(30) };
            _profile = profile;
        }

        private async Task<string?> GetTokenAsync()
        {
            return await _profile.GetTokenAsync();
        }

        public async Task<(bool isClosed, DateTimeOffset? closedAt)> IsConversationClosedAsync(Guid conversationId)
        {
            try
            {
                Debug.WriteLine($"[ConversationsService] Checking if conversation is closed: {conversationId}");

                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var url = $"api/conversations/{conversationId}";
                var response = await _http.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"[ConversationsService] Failed to get conversation status: {response.StatusCode}");
                    return (false, null);
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<JsonElement>(json, options);

                var isClosed = data.TryGetProperty("isClosed", out var closedProp) && closedProp.GetBoolean();
                DateTimeOffset? closedAt = null;

                if (data.TryGetProperty("closedAt", out var closedAtProp) && closedAtProp.ValueKind != JsonValueKind.Null)
                {
                    closedAt = closedAtProp.GetDateTimeOffset();
                }

                Debug.WriteLine($"[ConversationsService] Conversation {conversationId} isClosed={isClosed}");
                return (isClosed, closedAt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConversationsService] IsConversationClosedAsync error: {ex}");
                return (false, null);
            }
        }

        public async Task<List<ChatMessageDto>> GetMessagesAsync(Guid conversationId)
        {
            try
            {
                Debug.WriteLine($"[ConversationsService] GetMessagesAsync for conversation: {conversationId}");

                // Add authorization header
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    Debug.WriteLine($"[ConversationsService] Token added to request");
                }
                else
                {
                    Debug.WriteLine($"[ConversationsService] WARNING: No token available!");
                }

                var url = $"api/conversations/{conversationId}/messages";
                Debug.WriteLine($"[ConversationsService] Requesting: {url}");

                var response = await _http.GetAsync(url);
                Debug.WriteLine($"[ConversationsService] Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[ConversationsService] Error response: {errorContent}");
                    return new List<ChatMessageDto>();
                }

                var json = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[ConversationsService] Messages JSON (truncated): {json.Substring(0, Math.Min(500, json.Length))}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var res = JsonSerializer.Deserialize<List<ChatMessageDto>>(json, options);
                
                Debug.WriteLine($"[ConversationsService] Loaded {res?.Count ?? 0} messages");
                
                return res ?? new List<ChatMessageDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConversationsService] GetMessagesAsync error: {ex}");
                Debug.WriteLine($"[ConversationsService] Stack trace: {ex.StackTrace}");
                return new List<ChatMessageDto>();
            }
        }

        public async Task<List<ConversationSummaryDto>> GetConversationsAsync()
        {
            try
            {
                // Add authorization header
                var token = await GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                Debug.WriteLine($"[ConversationsService] Fetching conversations with token: {(!string.IsNullOrEmpty(token) ? "Present" : "Missing")}");

                // Fetch raw JSON for debugging
                var json = await _http.GetStringAsync("api/conversations");
                if (!string.IsNullOrEmpty(json))
                {
                    Debug.WriteLine($"[ConversationsService] Raw conversations JSON (truncated): {json.Substring(0, Math.Min(1000, json.Length))}");
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var res = JsonSerializer.Deserialize<List<ConversationSummaryDto>>(json, options);
                
                Debug.WriteLine($"[ConversationsService] Deserialized {res?.Count ?? 0} conversations");
                
                return res ?? new List<ConversationSummaryDto>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ConversationsService] GetConversationsAsync error: {ex}");
                return new List<ConversationSummaryDto>();
            }
        }
    }
}
