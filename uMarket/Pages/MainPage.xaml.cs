using System;
using Microsoft.Maui.Controls;
using uMarket.Services;

namespace uMarket.Pages
{
    public partial class MainPage : ContentPage
    {
        readonly UserProfileService _profile;
        readonly AuthService _auth;
        readonly INavigationService _navigation;

        int count = 0;

        public MainPage(UserProfileService profile, AuthService auth, INavigationService navigation)
        {
            InitializeComponent();
            _profile = profile;
            _auth = auth;
            _navigation = navigation;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var name = await _profile.GetFullNameAsync();
                if (!string.IsNullOrEmpty(name))
                {
                    WelcomeLabel.Text = $"Bienvenido, {name}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] OnAppearing error: {ex}");
            }
        }

        async void LogoutButton_Clicked(object? sender, EventArgs e)
        {
            try
            {
                var confirmed = await DisplayAlert("Cerrar sesión", "¿Está seguro que desea cerrar sesión?", "Sí", "No");
                if (!confirmed)
                    return;

                await _profile.ClearAsync();
                _auth.ClearAuthorization();
                await _navigation.NavigateToLoginAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Logout error: {ex}");
                await DisplayAlert("Error", "Failed to logout", "OK");
            }
        }

        private void OnCounterClicked(object? sender, System.EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }
    }
}
