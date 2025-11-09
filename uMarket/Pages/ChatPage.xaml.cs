using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using uMarket.Services;
using uMarket.Models.Chat;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace uMarket.Pages
{
    [QueryProperty("ConversationIdString", "conversationId")]
    public partial class ChatPage : ContentPage
    {
        readonly HubService _hub;
        readonly ConversationsService _conversations;
        readonly UserProfileService _profile;
        readonly INavigationService _navigation;

        public ObservableCollection<ChatMessageViewModel> Messages { get; } = new ObservableCollection<ChatMessageViewModel>();

        private string? _currentUserId;
        private bool _isConversationClosed = false;
        private string? _otherPersonName;

        // Backing GUID used internally
        public Guid ConversationId { get; set; }

        // QueryProperty target (string) — parse into ConversationId to avoid invalid cast
        string _conversationIdString = string.Empty;
        public string ConversationIdString
        {
            get => _conversationIdString;
            set
            {
                _conversationIdString = value;
                if (!string.IsNullOrEmpty(value) && Guid.TryParse(value, out var g))
                {
                    ConversationId = g;
                    Debug.WriteLine($"[ChatPage] Parsed conversationId query param to Guid: {ConversationId}");
                }
                else
                {
                    Debug.WriteLine($"[ChatPage] Failed to parse conversationId query param: '{value}'");
                }
            }
        }

        public ChatPage(
            HubService hub, 
            ConversationsService conversations, 
            UserProfileService profile,
            INavigationService navigation)
        {
            InitializeComponent();
            Debug.WriteLine("[ChatPage] Constructor called");
            
            _hub = hub;
            _conversations = conversations;
            _profile = profile;
            _navigation = navigation;
            MessagesCollection.ItemsSource = Messages;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine($"[ChatPage] OnAppearing START - ConversationId: {ConversationId}");
            
            try
            {
                // Get current user ID
                _currentUserId = await _profile.GetUserIdAsync();
                Debug.WriteLine($"[ChatPage] Current user ID: {_currentUserId}");

                Debug.WriteLine("[ChatPage] Checking if conversation is closed...");
                var (isClosed, closedAt) = await _conversations.IsConversationClosedAsync(ConversationId);
                
                if (isClosed)
                {
                    Debug.WriteLine($"[ChatPage] Conversation is CLOSED (closed at: {closedAt})");
                    _isConversationClosed = true;
                    DisableChatUI();
                    
                    await DisplayAlert("Conversación Finalizada", "Esta conversación ha sido cerrada y no puede reabrirse.", "OK");
                    await Task.Delay(500);
                    await _navigation.NavigateBackAsync();
                    return;
                }
                
                Debug.WriteLine("[ChatPage] Conversation is OPEN, proceeding normally");

                // subscribe
                _hub.OnMessageReceived += Hub_OnMessageReceived;
                _hub.OnConversationEnded += Hub_OnConversationEnded;
                Debug.WriteLine("[ChatPage] Subscribed to hub events");

                // Connect to hub if needed
                if (!_hub.IsConnected)
                {
                    Debug.WriteLine("[ChatPage] Hub not connected, initializing...");
                    var token = await _profile.GetTokenAsync();
                    await _hub.Initialize(async () => await Task.FromResult(token));
                    await _hub.StartAsync();
                    Debug.WriteLine("[ChatPage] Hub connected successfully");
                }
                else
                {
                    Debug.WriteLine("[ChatPage] Hub already connected");
                }

                // join conversation group
                Debug.WriteLine($"[ChatPage] Joining conversation group: {ConversationId}");
                await _hub.JoinConversationAsync(ConversationId);
                Debug.WriteLine("[ChatPage] Joined conversation group");

                // load recent messages
                Debug.WriteLine($"[ChatPage] Loading messages for conversation: {ConversationId}");
                var list = await _conversations.GetMessagesAsync(ConversationId);
                Debug.WriteLine($"[ChatPage] Retrieved {list.Count} messages from API");
                
                Messages.Clear();
                Debug.WriteLine("[ChatPage] Cleared existing messages");
                
                foreach (var m in list.OrderBy(m => m.SentAt))
                {
                    var vm = CreateMessageViewModel(m);
                    Messages.Add(vm);
                }
                
                Debug.WriteLine($"[ChatPage] Total messages in UI: {Messages.Count}");

                // Load other participant's name
                await LoadOtherPersonNameAsync();

                // Scroll to last item
                await ScrollToLastMessage();
                Debug.WriteLine("[ChatPage] OnAppearing COMPLETE");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] OnAppearing error: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo cargar el chat: {ex.Message}", "OK");
            }
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            Debug.WriteLine("[ChatPage] OnDisappearing");
            
            try
            {
                _hub.OnMessageReceived -= Hub_OnMessageReceived;
                _hub.OnConversationEnded -= Hub_OnConversationEnded;
                // Leave conversation group
                await _hub.LeaveConversationAsync(ConversationId);
                Debug.WriteLine("[ChatPage] Left conversation group");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] OnDisappearing error: {ex}");
            }
        }

        private async Task LoadOtherPersonNameAsync()
        {
            try
            {
                var conversations = await _conversations.GetConversationsAsync();
                var currentConversation = conversations.FirstOrDefault(c => c.ConversationId == ConversationId);
                
                if (currentConversation != null && !string.IsNullOrEmpty(currentConversation.OtherFullName))
                {
                    _otherPersonName = currentConversation.OtherFullName;
                    var nameLabel = this.FindByName<Label>("OtherPersonNameLabel");
                    if (nameLabel != null)
                    {
                        nameLabel.Text = _otherPersonName;
                        Debug.WriteLine($"[ChatPage] Set other person name to: {_otherPersonName}");
                    }
                }
                else
                {
                    Debug.WriteLine("[ChatPage] Could not find conversation or other person name");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] LoadOtherPersonName error: {ex}");
            }
        }

        private void Hub_OnMessageReceived(ChatMessageDto dto)
        {
            Debug.WriteLine($"[ChatPage] Hub_OnMessageReceived: {dto.Content} for conversation {dto.ConversationId}");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (dto.ConversationId == ConversationId)
                {
                    Debug.WriteLine("[ChatPage] Message is for this conversation, adding to UI");
                    Messages.Add(CreateMessageViewModel(dto));
                }
                else
                {
                    Debug.WriteLine($"[ChatPage] Message is for different conversation, ignoring");
                }
            });
        }

        private void Hub_OnConversationEnded(Guid conversationId, DateTimeOffset closedAt)
        {
            Debug.WriteLine($"[ChatPage] Hub_OnConversationEnded: conversationId={conversationId}");
            
            if (conversationId == ConversationId)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Debug.WriteLine("[ChatPage] This conversation was ended");
                    _isConversationClosed = true;
                    DisableChatUI();
                    
                    Messages.Add(new ChatMessageViewModel
                    {
                        MessageId = Guid.NewGuid(),
                        ConversationId = conversationId,
                        SenderId = "system",
                        Content = "Esta conversación ha finalizado.",
                        SentAt = closedAt,
                        IsMyMessage = false,
                        IsOtherMessage = false
                    });
                });
            }
        }

        private void DisableChatUI()
        {
            Debug.WriteLine("[ChatPage] Disabling chat UI");
            MessageEntry.IsEnabled = false;
            MessageEntry.Placeholder = "Esta conversación ha finalizado";
            SendButton.IsEnabled = false;
            EndChatButton.IsEnabled = false;
        }

        private ChatMessageViewModel CreateMessageViewModel(ChatMessageDto dto)
        {
            var isMyMessage = !string.IsNullOrEmpty(_currentUserId) && dto.SenderId == _currentUserId;
            return new ChatMessageViewModel
            {
                MessageId = dto.MessageId,
                ConversationId = dto.ConversationId,
                SenderId = dto.SenderId,
                Content = dto.Content,
                SentAt = dto.SentAt,
                IsMyMessage = isMyMessage,
                IsOtherMessage = !isMyMessage
            };
        }

        private async Task ScrollToLastMessage()
        {
            try
            {
                if (Messages.Count > 0)
                {
                    Debug.WriteLine("[ChatPage] Scrolling to last message");
                    await Task.Delay(100);
                    MessagesCollection.ScrollTo(Messages.Last(), position: ScrollToPosition.End, animate: false);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] ScrollToLastMessage error: {ex}");
            }
        }

        private async void SendButton_Clicked(object? sender, EventArgs e)
        {
            if (_isConversationClosed)
            {
                await DisplayAlert("Conversación Finalizada", "Esta conversación ha sido cerrada.", "OK");
                return;
            }

            var text = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                Debug.WriteLine("[ChatPage] SendButton_Clicked: Empty message, ignoring");
                return;
            }

            Debug.WriteLine($"[ChatPage] SendButton_Clicked: Sending message: {text}");

            try
            {
                await _hub.SendMessageAsync(ConversationId, text);
                MessageEntry.Text = string.Empty;
                Debug.WriteLine("[ChatPage] Message sent successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] Send error: {ex}");
                await DisplayAlert("Error", "No se pudo enviar el mensaje", "OK");
            }
        }

        private async void BackButton_Clicked(object? sender, EventArgs e)
        {
            Debug.WriteLine("[ChatPage] BackButton_Clicked");
            
            try
            {
                await _navigation.NavigateBackAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] BackButton error: {ex}");
                await DisplayAlert("Error", "No se pudo volver atrás", "OK");
            }
        }

        private async void EndChatButton_Clicked(object? sender, EventArgs e)
        {
            Debug.WriteLine("[ChatPage] EndChatButton_Clicked");
            
            if (_isConversationClosed)
            {
                await ShowToastAsync("Esta conversación ya ha sido cerrada", ToastDuration.Short);
                return;
            }

            try
            {
                var confirmed = await DisplayAlert("Finalizar Chat", "¿Estás seguro de que quieres finalizar esta conversación? Esta acción no se puede deshacer.", "Finalizar Chat", "Cancelar");
                if (!confirmed)
                {
                    Debug.WriteLine("[ChatPage] User cancelled end chat");
                    return;
                }

                Debug.WriteLine($"[ChatPage] Ending conversation: {ConversationId}");
                await _hub.EndConversationAsync(ConversationId);
                
                _isConversationClosed = true;
                DisableChatUI();
                
                await ShowToastAsync("La conversación ha sido cerrada", ToastDuration.Short);
                Debug.WriteLine("[ChatPage] Conversation ended successfully");
                
                await Task.Delay(1500);
                await _navigation.NavigateBackAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] EndChatButton error: {ex}");
                await DisplayAlert("Error", "No se pudo finalizar la conversación", "OK");
            }
        }

        private async Task ShowToastAsync(string message, ToastDuration duration)
        {
            try
            {
                var toast = Toast.Make(message, duration, 14);
                await toast.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ChatPage] Toast error: {ex}");
            }
        }
    }

    // ViewModel for message display with sender differentiation
    public class ChatMessageViewModel
    {
        public Guid MessageId { get; set; }
        public Guid ConversationId { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
        public bool IsMyMessage { get; set; }
        public bool IsOtherMessage { get; set; }
    }
}
