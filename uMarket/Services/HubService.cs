using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.Controls;
using uMarket.Models.Chat;
using System.Text.Json;
using uMarket.Models.Sellers;
using System.Diagnostics;

namespace uMarket.Services
{
    public class HubService
    {
        private HubConnection? _connection;
        private readonly string _defaultHubUrl;
        public event Action<ChatMessageDto>? OnMessageReceived;
        public event Action<Guid, string, DateTimeOffset>? OnChatRequested;
        public event Action<Guid, bool, Guid?>? OnChatRequestResponded;
        public event Action<Guid, bool, Guid?>? OnChatRequestResponseSaved;
        public event Action<SellerDto>? OnSellerUpdated;
        public event Action<Guid, DateTimeOffset>? OnConversationEnded;

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public HubService(string defaultHubUrl)
        {
            _defaultHubUrl = defaultHubUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(defaultHubUrl));
        }

        public Task Initialize(Func<Task<string?>> accessTokenProvider) => InitializeAsync(accessTokenProvider);

        public async Task InitializeAsync(Func<Task<string?>> accessTokenProvider)
        {
            var hubUrl = _defaultHubUrl;
            Debug.WriteLine($"[HubService] InitializeAsync with hubUrl={hubUrl}");

            if (_connection != null)
            {
                try
                {
                    Debug.WriteLine($"[HubService] Stopping previous connection (state={_connection.State})");
                    await _connection.StopAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HubService] Error stopping previous connection: {ex}");
                }
                finally
                {
                    _connection = null;
                }
            }

            _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = accessTokenProvider;
            })
            .WithAutomaticReconnect()
            .Build();

            _connection.Reconnecting += async (ex) =>
            {
                Debug.WriteLine($"[HubService] Reconnecting: {ex?.Message}");
                await Task.CompletedTask;
            };

            _connection.Reconnected += async (connectionId) =>
            {
                Debug.WriteLine($"[HubService] Reconnected. ConnectionId: {connectionId}");
                await Task.CompletedTask;
            };

            _connection.Closed += async (ex) =>
            {
                Debug.WriteLine($"[HubService] Closed. Exception: {ex?.Message}");
                await Task.CompletedTask;
            };

            _connection.On<ChatMessageDto>("ReceiveMessage", (dto) =>
            {
                Debug.WriteLine($"[HubService] ReceiveMessage: ConversationId={dto.ConversationId} SenderId={dto.SenderId}");
                OnMessageReceived?.Invoke(dto);
            });

            _connection.On<JsonElement>("ChatRequested", (el) =>
            {
                try
                {
                    Debug.WriteLine($"[HubService] ChatRequested payload: {el}");
                    if (TryGetPropertyCaseInsensitive(el, "RequestId", out var p1))
                    {
                        var requestId = TryParseGuidFromElement(p1, out var rId) ? rId : Guid.Empty;
                        var fromUserId = TryGetPropertyCaseInsensitive(el, "FromUserId", out var p2) && p2.ValueKind == JsonValueKind.String ? p2.GetString() ?? string.Empty : string.Empty;
                        var createdAt = TryGetPropertyCaseInsensitive(el, "CreatedAt", out var p3) ? p3.GetDateTimeOffset() : DateTimeOffset.MinValue;
                        if (requestId != Guid.Empty)
                            OnChatRequested?.Invoke(requestId, fromUserId, createdAt);
                        else
                            Debug.WriteLine($"[HubService] ChatRequested: invalid RequestId value: {p1}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HubService] ChatRequested handler error: {ex}");
                }
            });

            _connection.On<JsonElement>("ChatRequestResponded", (el) =>
            {
                try
                {
                    Debug.WriteLine($"[HubService] ChatRequestResponded payload: {el}");
                    if (TryGetPropertyCaseInsensitive(el, "RequestId", out var p1))
                    {
                        if (!TryParseGuidFromElement(p1, out var requestId))
                        {
                            Debug.WriteLine($"[HubService] ChatRequestResponded: invalid RequestId: {p1}");
                            return;
                        }

                        var accepted = TryGetPropertyCaseInsensitive(el, "Accepted", out var p2) && p2.ValueKind == JsonValueKind.True;
                        Guid? conversationId = null;
                        if (TryGetPropertyCaseInsensitive(el, "ConversationId", out var p3) && p3.ValueKind != JsonValueKind.Null)
                        {
                            if (TryParseGuidFromElement(p3, out var convId))
                                conversationId = convId;
                            else
                                Debug.WriteLine($"[HubService] ChatRequestResponded: invalid ConversationId: {p3}");
                        }

                        OnChatRequestResponded?.Invoke(requestId, accepted, conversationId);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HubService] ChatRequestResponded handler error: {ex}");
                }
            });

            _connection.On<JsonElement>("ChatRequestResponseSaved", (el) =>
            {
                try
                {
                    Debug.WriteLine($"[HubService] ChatRequestResponseSaved payload: {el}");
                    if (TryGetPropertyCaseInsensitive(el, "RequestId", out var p1))
                    {
                        if (!TryParseGuidFromElement(p1, out var requestId))
                        {
                            Debug.WriteLine($"[HubService] ChatRequestResponseSaved: invalid RequestId: {p1}");
                            return;
                        }

                        var accepted = TryGetPropertyCaseInsensitive(el, "Accepted", out var p2) && p2.ValueKind == JsonValueKind.True;
                        Guid? conversationId = null;
                        if (TryGetPropertyCaseInsensitive(el, "ConversationId", out var p3) && p3.ValueKind != JsonValueKind.Null)
                        {
                            if (TryParseGuidFromElement(p3, out var convId))
                                conversationId = convId;
                            else
                                Debug.WriteLine($"[HubService] ChatRequestResponseSaved: invalid ConversationId: {p3}");
                        }

                        OnChatRequestResponseSaved?.Invoke(requestId, accepted, conversationId);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HubService] ChatRequestResponseSaved handler error: {ex}");
                }
            });

            _connection.On<JsonElement>("SellerUpdated", (el) =>
            {
                try
                {
                    Debug.WriteLine($"[HubService] SellerUpdated payload: {el}");
                    var id = TryGetPropertyCaseInsensitive(el, "UserId", out var pu) && pu.ValueKind == JsonValueKind.String ? pu.GetString() ?? string.Empty : string.Empty;
                    var name = TryGetPropertyCaseInsensitive(el, "FullName", out var pn) && pn.ValueKind == JsonValueKind.String ? pn.GetString() ?? string.Empty : string.Empty;
                    var desc = TryGetPropertyCaseInsensitive(el, "CurrentDescription", out var pcd) && pcd.ValueKind == JsonValueKind.String ? pcd.GetString() : null;
                    var isAvail = TryGetPropertyCaseInsensitive(el, "IsAvailable", out var pa) && (pa.ValueKind == JsonValueKind.True || pa.ValueKind == JsonValueKind.False) ? pa.GetBoolean() : false;
                    var seller = new SellerDto { UserId = id, FullName = name, CurrentDescription = desc, IsAvailable = isAvail };
                    OnSellerUpdated?.Invoke(seller);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HubService] SellerUpdated handler error: {ex}");
                }
            });

            _connection.On<JsonElement>("ConversationEnded", (el) =>
            {
                try
                {
                    Debug.WriteLine($"[HubService] ConversationEnded payload: {el}");
                    if (TryGetPropertyCaseInsensitive(el, "ConversationId", out var p1))
                    {
                        if (TryParseGuidFromElement(p1, out var conversationId))
                        {
                            var closedAt = TryGetPropertyCaseInsensitive(el, "ClosedAt", out var p2) ? p2.GetDateTimeOffset() : DateTimeOffset.UtcNow;
                            Debug.WriteLine($"[HubService] Conversation ended: {conversationId}");
                            OnConversationEnded?.Invoke(conversationId, closedAt);
                        }
                        else
                        {
                            Debug.WriteLine($"[HubService] ConversationEnded: invalid ConversationId: {p1}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HubService] ConversationEnded handler error: {ex}");
                }
            });
        }

        static bool TryGetPropertyCaseInsensitive(JsonElement el, string name, out JsonElement prop)
        {
            if (el.TryGetProperty(name, out prop)) return true;
            foreach (var p in el.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    prop = p.Value;
                    return true;
                }
            }
            prop = default;
            return false;
        }

        static bool TryParseGuidFromElement(JsonElement el, out Guid value)
        {
            value = Guid.Empty;
            try
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (Guid.TryParse(s, out var g))
                    {
                        value = g;
                        return true;
                    }
                    return false;
                }
                else if (el.ValueKind == JsonValueKind.Null)
                {
                    return false;
                }
                else if (el.ValueKind == JsonValueKind.Undefined)
                {
                    return false;
                }
                else
                {
                    try
                    {
                        value = el.GetGuid();
                        return true;
                    }
                    catch
                    {
                        Debug.WriteLine($"[HubService] TryParseGuidFromElement fallback failed for element: {el}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HubService] TryParseGuidFromElement error: {ex}");
                return false;
            }
        }

        public async Task StartAsync()
        {
            if (_connection == null) throw new InvalidOperationException("HubService not initialized");
            try
            {
                Debug.WriteLine($"[HubService] Starting connection (current state={_connection.State})");
                if (_connection.State == HubConnectionState.Disconnected)
                    await _connection.StartAsync();
                Debug.WriteLine($"[HubService] Connection started (state={_connection.State})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HubService] StartAsync error: {ex}");
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (_connection == null) return;
            try
            {
                Debug.WriteLine($"[HubService] Stopping connection (state={_connection.State})");
                await _connection.StopAsync();
                Debug.WriteLine($"[HubService] Connection stopped (state={_connection.State})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HubService] StopAsync error: {ex}");
            }
        }

        public async Task RequestChatAsync(string sellerUserId)
        {
            if (_connection == null) throw new InvalidOperationException();
            await _connection.InvokeAsync("RequestChat", sellerUserId);
        }

        public async Task RespondToRequestAsync(Guid requestId, bool accept)
        {
            if (_connection == null) throw new InvalidOperationException();
            await _connection.InvokeAsync("RespondToRequest", requestId, accept);
        }

        public async Task JoinConversationAsync(Guid conversationId)
        {
            if (_connection == null) throw new InvalidOperationException();
            await _connection.InvokeAsync("JoinConversation", conversationId);
        }

        public async Task LeaveConversationAsync(Guid conversationId)
        {
            if (_connection == null) return;
            await _connection.InvokeAsync("LeaveConversation", conversationId);
        }

        public async Task SendMessageAsync(Guid conversationId, string content)
        {
            if (_connection == null) throw new InvalidOperationException();
            await _connection.InvokeAsync("SendMessage", conversationId, content);
        }

        public async Task EndConversationAsync(Guid conversationId)
        {
            if (_connection == null) throw new InvalidOperationException();
            await _connection.InvokeAsync("EndConversation", conversationId);
        }
    }

    static class JsonElementExtensions
    {
        public static DateTimeOffset GetDateTimeOffset(this JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(el.GetString(), out var dto))
                return dto;
            return DateTimeOffset.MinValue;
        }
    }
}
