using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using uMarket.Models.Sellers;
using uMarket.Services;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using uMarket.Models.Chat;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace uMarket.Pages
{
    public partial class StudentMainPage : ContentPage
    {
        readonly SellersService _sellers;
        readonly HubService _hub;
        readonly UserProfileService _profile;
        readonly ConversationsService _conversations;
        readonly INavigationService _navigation;
        readonly SecureStorageService _secureStorage;

        public ObservableCollection<SellerDto> Sellers { get; } = new ObservableCollection<SellerDto>();
        public ObservableCollection<ConversationSummaryDto> Conversations { get; } = new ObservableCollection<ConversationSummaryDto>();

        public StudentMainPage(
            SellersService sellers,
            HubService hub,
            UserProfileService profile,
            ConversationsService conversations,
            INavigationService navigation,
            SecureStorageService secureStorage)
        {
            InitializeComponent();
            Debug.WriteLine("[StudentMainPage] Constructor called");
            
            _sellers = sellers;
            _hub = hub;
            _profile = profile;
            _conversations = conversations;
            _navigation = navigation;
            _secureStorage = secureStorage;
            SellersCollection.ItemsSource = Sellers;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("[StudentMainPage] OnAppearing START");
            
            Sellers.Clear();
            var list = await _sellers.GetAvailableSellersAsync();
            Debug.WriteLine($"[StudentMainPage] Loaded {list.Count} sellers");
            
            foreach (var s in list)
                Sellers.Add(s);

            try
            {
                var convView = this.FindByName<CollectionView>("ConversationsCollection");
                if (convView != null)
                {
                    convView.ItemsSource = Conversations;
                    Debug.WriteLine("[StudentMainPage] ConversationsCollection bound to ItemsSource");
                }
                else
                {
                    Debug.WriteLine("[StudentMainPage] ERROR: ConversationsCollection not found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentMainPage] ERROR binding ConversationsCollection: {ex}");
            }

            _hub.OnSellerUpdated += Hub_OnSellerUpdated;
            _hub.OnChatRequestResponded += Hub_OnChatRequestResponded;
            _hub.OnConversationEnded += Hub_OnConversationEnded;

            if (!_hub.IsConnected)
            {
                Debug.WriteLine("[StudentMainPage] Hub not connected, connecting...");
                var token = await _profile.GetTokenAsync();
                await _hub.Initialize(async () => await Task.FromResult(token));
                await _hub.StartAsync();
                Debug.WriteLine("[StudentMainPage] Hub connected");
            }
            else
            {
                Debug.WriteLine("[StudentMainPage] Hub already connected");
            }

            await LoadConversationsAsync();
            Debug.WriteLine("[StudentMainPage] OnAppearing COMPLETE");
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Debug.WriteLine("[StudentMainPage] OnDisappearing");
            
            _hub.OnSellerUpdated -= Hub_OnSellerUpdated;
            _hub.OnChatRequestResponded -= Hub_OnChatRequestResponded;
            _hub.OnConversationEnded -= Hub_OnConversationEnded;
        }

        private void Hub_OnSellerUpdated(SellerDto dto)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existing = Sellers.FirstOrDefault(x => x.UserId == dto.UserId);
                if (dto.IsAvailable)
                {
                    if (existing == null)
                        Sellers.Add(dto);
                    else
                    {
                        existing.CurrentDescription = dto.CurrentDescription;
                        var idx = Sellers.IndexOf(existing);
                        Sellers[idx] = existing;
                    }
                }
                else
                {
                    if (existing != null)
                        Sellers.Remove(existing);
                }
            });
        }

        private void Hub_OnChatRequestResponded(Guid requestId, bool accepted, Guid? conversationId)
        {
            Debug.WriteLine($"[StudentMainPage] Hub_OnChatRequestResponded: accepted={accepted}, conversationId={conversationId}");
            
            if (accepted && conversationId.HasValue)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        Debug.WriteLine($"[StudentMainPage] Navigating to chat: {conversationId.Value}");
                        await _navigation.NavigateToChatAsync(conversationId.Value);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[StudentMainPage] Navigation to chat error: {ex}");
                        await DisplayAlert("Error", $"No se pudo abrir el chat: {ex.Message}", "OK");
                    }
                });
            }
            else if (!accepted)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await ShowToastAsync("Tu solicitud de chat fue rechazada", ToastDuration.Short);
                });
            }
        }

        private void Hub_OnConversationEnded(Guid conversationId, DateTimeOffset closedAt)
        {
            Debug.WriteLine($"[StudentMainPage] Hub_OnConversationEnded: conversationId={conversationId}");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);
                if (conversation != null)
                {
                    Debug.WriteLine($"[StudentMainPage] Removing ended conversation from list: {conversationId}");
                    Conversations.Remove(conversation);
                }
            });
        }

        private async Task LoadConversationsAsync()
        {
            try
            {
                Debug.WriteLine("[StudentMainPage] LoadConversationsAsync START");
                
                var list = await _conversations.GetConversationsAsync();
                Debug.WriteLine($"[StudentMainPage] Loaded {list?.Count ?? 0} conversations from API");
                
                Conversations.Clear();
                
                if (list != null && list.Count > 0)
                {
                    foreach (var c in list)
                    {
                        Debug.WriteLine($"[StudentMainPage] Conversation: id={c.ConversationId}, other={c.OtherFullName}");
                        Conversations.Add(c);
                    }
                    Debug.WriteLine($"[StudentMainPage] Added {Conversations.Count} conversations to UI");
                }
                else
                {
                    Debug.WriteLine("[StudentMainPage] No conversations found");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentMainPage] LoadConversations error: {ex.Message}");
            }
        }

        private async void SellerFrame_Tapped(object sender, TappedEventArgs e)
        {
            Debug.WriteLine("[StudentMainPage] SellerFrame_Tapped FIRED");
            
            if (sender is not Frame frame || frame.BindingContext is not SellerDto selected)
            {
                Debug.WriteLine("[StudentMainPage] No seller in BindingContext");
                return;
            }

            Debug.WriteLine($"[StudentMainPage] Selected seller: {selected.FullName} (ID: {selected.UserId})");

            var confirmed = await DisplayAlert("Solicitar Chat", $"¿Solicitar chat con {selected.FullName}?", "Sí", "No");
            
            if (!confirmed)
            {
                Debug.WriteLine("[StudentMainPage] User cancelled chat request");
                return;
            }

            try
            {
                Debug.WriteLine($"[StudentMainPage] Sending chat request to {selected.UserId}");
                
                if (!_hub.IsConnected)
                {
                    Debug.WriteLine("[StudentMainPage] Hub not connected, connecting...");
                    var token = await _profile.GetTokenAsync();
                    await _hub.Initialize(async () => await Task.FromResult(token));
                    await _hub.StartAsync();
                }
                
                await _hub.RequestChatAsync(selected.UserId);
                Debug.WriteLine("[StudentMainPage] Chat request sent successfully");
                
                await ShowToastAsync("Solicitud de chat enviada", ToastDuration.Short);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentMainPage] Request chat error: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo solicitar el chat: {ex.Message}", "OK");
            }
        }

        private async void SellersCollection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("[StudentMainPage] SellersCollection_SelectionChanged FIRED");
            Debug.WriteLine($"[StudentMainPage] CurrentSelection count: {e.CurrentSelection?.Count ?? 0}");
            
            var selected = e.CurrentSelection.FirstOrDefault() as SellerDto;
            
            if (selected == null)
            {
                Debug.WriteLine("[StudentMainPage] No seller selected (null)");
                return;
            }

            Debug.WriteLine($"[StudentMainPage] Selected seller: {selected.FullName}");

            ((CollectionView)sender!).SelectedItem = null;

            var confirmed = await DisplayAlert("Solicitar Chat", $"¿Solicitar chat con {selected.FullName}?", "Sí", "No");
            
            if (!confirmed)
            {
                Debug.WriteLine("[StudentMainPage] User cancelled");
                return;
            }

            try
            {
                Debug.WriteLine("[StudentMainPage] Sending chat request");
                
                if (!_hub.IsConnected)
                {
                    var token = await _profile.GetTokenAsync();
                    await _hub.Initialize(async () => await Task.FromResult(token));
                    await _hub.StartAsync();
                }
                
                await _hub.RequestChatAsync(selected.UserId);
                await ShowToastAsync("Solicitud de chat enviada", ToastDuration.Short);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentMainPage] Error: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo solicitar el chat: {ex.Message}", "OK");
            }
        }

        private async void ConversationFrame_Tapped(object sender, TappedEventArgs e)
        {
            Debug.WriteLine("[StudentMainPage] ConversationFrame_Tapped FIRED");
            
            if (sender is not Frame frame || frame.BindingContext is not ConversationSummaryDto selected)
            {
                Debug.WriteLine("[StudentMainPage] No conversation in BindingContext");
                return;
            }

            Debug.WriteLine($"[StudentMainPage] Selected conversation: {selected.OtherFullName} (ID: {selected.ConversationId})");

            try
            {
                Debug.WriteLine($"[StudentMainPage] Navigating to conversation {selected.ConversationId}");
                await _navigation.NavigateToChatAsync(selected.ConversationId);
                Debug.WriteLine("[StudentMainPage] Navigation initiated");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentMainPage] Navigation error: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo abrir la conversación: {ex.Message}", "OK");
            }
        }

        private async void ConversationsCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("[StudentMainPage] ConversationsCollection_SelectionChanged FIRED");
            Debug.WriteLine($"[StudentMainPage] CurrentSelection count: {e.CurrentSelection?.Count ?? 0}");
            
            var selected = e.CurrentSelection.FirstOrDefault() as ConversationSummaryDto;
            
            if (selected == null)
            {
                Debug.WriteLine("[StudentMainPage] No conversation selected (null)");
                return;
            }

            Debug.WriteLine($"[StudentMainPage] Selected conversation: {selected.OtherFullName}");

            ((CollectionView)sender!).SelectedItem = null;

            try
            {
                Debug.WriteLine($"[StudentMainPage] Navigating to conversation {selected.ConversationId}");
                await _navigation.NavigateToChatAsync(selected.ConversationId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentMainPage] Error: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo abrir la conversación: {ex.Message}", "OK");
            }
        }

        private async void LogoutButton_Clicked(object? sender, EventArgs e)
        {
            Debug.WriteLine("[StudentMainPage] LogoutButton_Clicked");
            
            try
            {
                await _profile.ClearAsync();

                var rememberMe = await _secureStorage.GetRememberMeAsync();
                if (!rememberMe)
                {
                    await _secureStorage.ClearCredentialsAsync();
                    Debug.WriteLine("[StudentMainPage] Cleared saved credentials on logout");
                }

                var auth = (Application.Current as App)?.Handler?.MauiContext?.Services?
                    .GetService(typeof(AuthService)) as AuthService;
                auth?.ClearAuthorization();

                await _navigation.NavigateToLoginAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StudentMainPage] Logout error: {ex}");
                await DisplayAlert("Error", "Error al cerrar sesión", "OK");
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
                Debug.WriteLine($"[StudentMainPage] Toast error: {ex}");
            }
        }
    }
}
