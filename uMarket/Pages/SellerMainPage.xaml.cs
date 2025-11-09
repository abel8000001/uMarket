using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using uMarket.Services;
using uMarket.Models.Sellers;
using uMarket.Models.Chat;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace uMarket.Pages
{
    public partial class SellerMainPage : ContentPage
    {
        readonly UserProfileService _profile;
        readonly HubService _hub;
        readonly AuthService _auth;
        readonly SellersService _sellersService;
        readonly ConversationsService _conversationsService;
        readonly INavigationService _navigation;

        public ObservableCollection<object> Requests { get; } = new ObservableCollection<object>();
        public ObservableCollection<ConversationSummaryDto> Conversations { get; } = new ObservableCollection<ConversationSummaryDto>();

        private bool _isLoadingStatus = false;

        public SellerMainPage(
            UserProfileService profile, 
            HubService hub, 
            AuthService auth, 
            SellersService sellersService, 
            ConversationsService conversationsService,
            INavigationService navigation)
        {
            InitializeComponent();
            Debug.WriteLine("[SellerMainPage] Constructor called");
            
            _profile = profile;
            _hub = hub;
            _auth = auth;
            _sellersService = sellersService;
            _conversationsService = conversationsService;
            _navigation = navigation;
            RequestsCollection.ItemsSource = Requests;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            Debug.WriteLine("[SellerMainPage] OnAppearing START");
            
            try
            {
                var token = await _profile.GetTokenAsync();
                
                await LoadSellerStatusAsync(token);
                
                if (!_hub.IsConnected)
                {
                    Debug.WriteLine("[SellerMainPage] Hub not connected, connecting...");
                    await _hub.Initialize(async () => await Task.FromResult(token));
                    await _hub.StartAsync();
                    Debug.WriteLine("[SellerMainPage] Hub connected");
                }

                try
                {
                    var convView = this.FindByName<CollectionView>("ConversationsCollection");
                    if (convView != null)
                    {
                        convView.ItemsSource = Conversations;
                        Debug.WriteLine("[SellerMainPage] ConversationsCollection bound");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SellerMainPage] Error binding ConversationsCollection: {ex}");
                }

                _hub.OnChatRequested += Hub_OnChatRequested;
                _hub.OnChatRequestResponseSaved += Hub_OnChatRequestResponseSaved;
                _hub.OnConversationEnded += Hub_OnConversationEnded;

                var list = await _sellersService.GetPendingRequestsAsync(token);
                Requests.Clear();
                if (list != null)
                {
                    Debug.WriteLine($"[SellerMainPage] Loaded {list.Count} pending requests");
                    foreach (var r in list)
                        Requests.Add(r);
                }

                await LoadConversationsAsync();
                Debug.WriteLine("[SellerMainPage] OnAppearing COMPLETE");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] OnAppearing error: {ex}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Debug.WriteLine("[SellerMainPage] OnDisappearing");
            
            try
            {
                _hub.OnChatRequested -= Hub_OnChatRequested;
                _hub.OnChatRequestResponseSaved -= Hub_OnChatRequestResponseSaved;
                _hub.OnConversationEnded -= Hub_OnConversationEnded;
            }
            catch { }
        }

        private async Task LoadSellerStatusAsync(string token)
        {
            try
            {
                _isLoadingStatus = true;
                Debug.WriteLine("[SellerMainPage] Loading seller status...");
                
                var status = await _sellersService.GetMyStatusAsync(token);
                
                if (status != null)
                {
                    AvailableSwitch.IsToggled = status.IsAvailable;
                    DescriptionEditor.Text = status.CurrentDescription ?? string.Empty;
                    
                    UpdateAvailabilityLabel(status.IsAvailable);
                    
                    Debug.WriteLine($"[SellerMainPage] Loaded status: Available={status.IsAvailable}, Description={status.CurrentDescription}");
                }
                
                _isLoadingStatus = false;
            }
            catch (Exception ex)
            {
                _isLoadingStatus = false;
                Debug.WriteLine($"[SellerMainPage] LoadSellerStatus error: {ex}");
            }
        }

        private void UpdateAvailabilityLabel(bool isAvailable)
        {
            var statusLabel = this.FindByName<Label>("AvailabilityStatusLabel");
            if (statusLabel != null)
            {
                statusLabel.Text = isAvailable ? "Actualmente Disponible" : "Actualmente No Disponible";
                statusLabel.TextColor = isAvailable ? Colors.Green : Colors.Gray;
            }
        }

        private void Hub_OnChatRequested(Guid requestId, string fromUserId, DateTimeOffset createdAt)
        {
            MainThread.BeginInvokeOnMainThread(() => { _ = RefreshPendingRequestsAsync(); });
        }

        private void Hub_OnChatRequestResponseSaved(Guid requestId, bool accepted, Guid? conversationId)
        {
            Debug.WriteLine($"[SellerMainPage] Hub_OnChatRequestResponseSaved: accepted={accepted}, conversationId={conversationId}");
            
            if (accepted && conversationId.HasValue)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        Debug.WriteLine($"[SellerMainPage] Navigating to chat: {conversationId.Value}");
                        await _navigation.NavigateToChatAsync(conversationId.Value);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SellerMainPage] Navigation error: {ex}");
                    }
                });
            }
        }

        private void Hub_OnConversationEnded(Guid conversationId, DateTimeOffset closedAt)
        {
            Debug.WriteLine($"[SellerMainPage] Hub_OnConversationEnded: conversationId={conversationId}");
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);
                if (conversation != null)
                {
                    Debug.WriteLine($"[SellerMainPage] Removing ended conversation from list: {conversationId}");
                    Conversations.Remove(conversation);
                }
            });
        }

        private async Task RefreshPendingRequestsAsync()
        {
            try
            {
                var token = await _profile.GetTokenAsync();
                var list = await _sellersService.GetPendingRequestsAsync(token);
                Requests.Clear();
                if (list != null)
                {
                    foreach (var r in list)
                        Requests.Add(r);
                }
                
                Debug.WriteLine($"[SellerMainPage] Refreshed pending requests, count: {Requests.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] RefreshPendingRequests error: {ex}");
            }
        }

        private async Task LoadConversationsAsync()
        {
            try
            {
                Debug.WriteLine("[SellerMainPage] LoadConversationsAsync START");
                
                var list = await _conversationsService.GetConversationsAsync();
                Debug.WriteLine($"[SellerMainPage] Loaded {list?.Count ?? 0} conversations");
                
                Conversations.Clear();
                
                if (list != null && list.Count > 0)
                {
                    foreach (var c in list)
                    {
                        Debug.WriteLine($"[SellerMainPage] Conversation: id={c.ConversationId}, other={c.OtherFullName}");
                        Conversations.Add(c);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] LoadConversations error: {ex}");
            }
        }

        async void AvailableSwitch_Toggled(object? sender, ToggledEventArgs e)
        {
            if (_isLoadingStatus)
            {
                Debug.WriteLine("[SellerMainPage] Ignoring toggle during status load");
                return;
            }

            var isAvailable = e.Value;
            Debug.WriteLine($"[SellerMainPage] Availability toggled to: {isAvailable}");
            
            UpdateAvailabilityLabel(isAvailable);

            try
            {
                AvailableSwitch.IsEnabled = false;
                
                var token = await _profile.GetTokenAsync();
                var currentDescription = DescriptionEditor.Text?.Trim();
                
                var req = new UpdateSellerRequest 
                { 
                    IsAvailable = isAvailable, 
                    CurrentDescription = currentDescription 
                };
                
                var (ok, error) = await _sellersService.UpdateMyProfileAsync(token, req);
                
                if (!ok)
                {
                    Debug.WriteLine($"[SellerMainPage] Failed to update availability: {error}");
                    _isLoadingStatus = true;
                    AvailableSwitch.IsToggled = !isAvailable;
                    _isLoadingStatus = false;
                    UpdateAvailabilityLabel(!isAvailable);
                    await DisplayAlert("Error", error ?? "No se pudo actualizar la disponibilidad", "OK");
                }
                else
                {
                    Debug.WriteLine("[SellerMainPage] Availability updated successfully");
                }
                
                AvailableSwitch.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] AvailableSwitch_Toggled error: {ex}");
                _isLoadingStatus = true;
                AvailableSwitch.IsToggled = !isAvailable;
                _isLoadingStatus = false;
                UpdateAvailabilityLabel(!isAvailable);
                AvailableSwitch.IsEnabled = true;
                await DisplayAlert("Error", "No se pudo actualizar la disponibilidad", "OK");
            }
        }

        private async void SaveButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                var saveBtn = this.FindByName<Button>("SaveButton");
                if (saveBtn != null)
                    saveBtn.IsEnabled = false;
                
                var token = await _profile.GetTokenAsync();
                var description = DescriptionEditor.Text?.Trim();
                
                var req = new UpdateSellerRequest 
                { 
                    IsAvailable = AvailableSwitch.IsToggled, 
                    CurrentDescription = description 
                };
                
                Debug.WriteLine("[SellerMainPage] Saving description and broadcasting update...");
                var (ok, error) = await _sellersService.UpdateMyProfileAsync(token, req);
                
                if (ok)
                {
                    Debug.WriteLine("[SellerMainPage] Description saved and broadcasted successfully");
                    await ShowToastAsync("Descripción actualizada correctamente", ToastDuration.Short);
                }
                else
                {
                    await DisplayAlert("Error", error ?? "No se pudo guardar la descripción", "OK");
                }
                
                if (saveBtn != null)
                    saveBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] SaveButton error: {ex}");
                var saveBtn = this.FindByName<Button>("SaveButton");
                if (saveBtn != null)
                    saveBtn.IsEnabled = true;
                await DisplayAlert("Error", ex.Message, "OK");
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
                Debug.WriteLine($"[SellerMainPage] Toast error: {ex}");
            }
        }

        private async void AcceptButton_Clicked(object? sender, EventArgs e)
        {
            if (!(sender is Button b)) return;
            var reqObj = b.BindingContext;
            if (reqObj == null) return;

            try
            {
                var requestId = Guid.Parse(reqObj.GetType().GetProperty("RequestId")?.GetValue(reqObj)?.ToString() ?? string.Empty);
                
                Debug.WriteLine($"[SellerMainPage] Accepting chat request: {requestId}");
                
                if (!_hub.IsConnected)
                {
                    var token = await _profile.GetTokenAsync();
                    await _hub.Initialize(async () => await Task.FromResult(token));
                    await _hub.StartAsync();
                }
                
                await _hub.RespondToRequestAsync(requestId, true);
                Requests.Remove(reqObj);
                
                Debug.WriteLine("[SellerMainPage] Chat request accepted");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] AcceptButton error: {ex}");
                await DisplayAlert("Error", "No se pudo aceptar la solicitud", "OK");
            }
        }

        private async void DenyButton_Clicked(object? sender, EventArgs e)
        {
            if (!(sender is Button b)) return;
            var reqObj = b.BindingContext;
            if (reqObj == null) return;

            try
            {
                var requestId = Guid.Parse(reqObj.GetType().GetProperty("RequestId")?.GetValue(reqObj)?.ToString() ?? string.Empty);
                
                Debug.WriteLine($"[SellerMainPage] Denying chat request: {requestId}");
                
                if (!_hub.IsConnected)
                {
                    var token = await _profile.GetTokenAsync();
                    await _hub.Initialize(async () => await Task.FromResult(token));
                    await _hub.StartAsync();
                }
                
                await _hub.RespondToRequestAsync(requestId, false);
                Requests.Remove(reqObj);
                
                Debug.WriteLine("[SellerMainPage] Chat request denied");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] DenyButton error: {ex}");
                await DisplayAlert("Error", "No se pudo rechazar la solicitud", "OK");
            }
        }

        private async void ConversationFrame_Tapped(object sender, TappedEventArgs e)
        {
            Debug.WriteLine("[SellerMainPage] ConversationFrame_Tapped FIRED");
            
            if (sender is not Frame frame || frame.BindingContext is not ConversationSummaryDto selected)
            {
                Debug.WriteLine("[SellerMainPage] No conversation in BindingContext");
                return;
            }

            Debug.WriteLine($"[SellerMainPage] Selected conversation: {selected.OtherFullName} (ID: {selected.ConversationId})");

            try
            {
                Debug.WriteLine($"[SellerMainPage] Navigating to conversation {selected.ConversationId}");
                await _navigation.NavigateToChatAsync(selected.ConversationId);
                Debug.WriteLine("[SellerMainPage] Navigation initiated");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] Navigation error: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo abrir la conversación: {ex.Message}", "OK");
            }
        }

        private async void ConversationsCollection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            Debug.WriteLine("[SellerMainPage] ConversationsCollection_SelectionChanged FIRED");
            
            var selected = e.CurrentSelection.FirstOrDefault() as ConversationSummaryDto;
            
            if (selected == null)
            {
                Debug.WriteLine("[SellerMainPage] No conversation selected");
                return;
            }

            Debug.WriteLine($"[SellerMainPage] Selected: {selected.OtherFullName}");

            if (sender is CollectionView cv)
                cv.SelectedItem = null;

            try
            {
                Debug.WriteLine($"[SellerMainPage] Navigating to conversation {selected.ConversationId}");
                await _navigation.NavigateToChatAsync(selected.ConversationId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] Error: {ex.Message}");
                await DisplayAlert("Error", $"No se pudo abrir la conversación: {ex.Message}", "OK");
            }
        }

        private async void LogoutButton_Clicked(object? sender, EventArgs e)
        {
            Debug.WriteLine("[SellerMainPage] LogoutButton_Clicked");
            
            try
            {
                await _profile.ClearAsync();
                
                var secureStorage = (Application.Current as App)?.Handler?.MauiContext?.Services?
                    .GetService(typeof(SecureStorageService)) as SecureStorageService;
                
                if (secureStorage != null)
                {
                    var rememberMe = await secureStorage.GetRememberMeAsync();
                    if (!rememberMe)
                    {
                        await secureStorage.ClearCredentialsAsync();
                        Debug.WriteLine("[SellerMainPage] Cleared saved credentials on logout");
                    }
                }
                
                _auth.ClearAuthorization();
                await _navigation.NavigateToLoginAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SellerMainPage] Logout error: {ex}");
                await DisplayAlert("Error", "Error al cerrar sesión", "OK");
            }
        }
    }
}
