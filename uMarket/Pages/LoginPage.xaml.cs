using System;
using Microsoft.Maui.Controls;
using uMarket.Services;
using uMarket.Models.Auth;
using System.Diagnostics;

namespace uMarket.Pages
{
    public partial class LoginPage : ContentPage
    {
        readonly AuthService _auth;
        readonly UserProfileService _profile;
        readonly INavigationService _navigation;
        readonly SecureStorageService _secureStorage;

        bool _isPasswordVisible = false;

        public LoginPage(AuthService auth, UserProfileService profile, INavigationService navigation, SecureStorageService secureStorage)
        {
            InitializeComponent();
            _auth = auth;
            _profile = profile;
            _navigation = navigation;
            _secureStorage = secureStorage;

            PasswordToggleButton.Source = "eye_hidden.png";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                var rememberMe = await _secureStorage.GetRememberMeAsync();
                RememberCheck.IsChecked = rememberMe;
                
                if (rememberMe)
                {
                    Debug.WriteLine("[LoginPage] Remember me is enabled, loading credentials");
                    var (username, password) = await _secureStorage.GetSavedCredentialsAsync();
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        UsernameEntry.Text = username;
                        PasswordEntry.Text = password;
                        Debug.WriteLine($"[LoginPage] Auto-filled credentials for user: {username}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] OnAppearing error: {ex}");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            return true;
        }

        void PasswordToggleButton_Clicked(object? sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            PasswordEntry.IsPassword = !_isPasswordVisible;
            PasswordToggleButton.Source = _isPasswordVisible ? "eye_visible.png" : "eye_hidden.png";
        }

        async void LoginButton_Clicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            LoginButton.IsEnabled = false;

            try
            {
                var username = UsernameEntry.Text?.Trim() ?? string.Empty;
                var password = PasswordEntry.Text ?? string.Empty;
                var rememberMe = RememberCheck.IsChecked;

                if (!int.TryParse(username, out var id))
                {
                    ErrorLabel.Text = "Ingrese su ID institucional";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    ErrorLabel.Text = "Ingrese su contraseña";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                Debug.WriteLine($"[LoginPage] Attempting login for user: {id}, RememberMe: {rememberMe}");
                
                var result = await _auth.LoginAsync(id, password);
                if (result == null)
                {
                    ErrorLabel.Text = "ID o contraseña incorrectos";
                    ErrorLabel.IsVisible = true;
                    
                    if (rememberMe)
                    {
                        await _secureStorage.ClearCredentialsAsync();
                    }
                    return;
                }

                Debug.WriteLine($"[LoginPage] Login successful for user: {result.UserId}");
                
                await _profile.SaveProfileAsync(result);

                if (rememberMe)
                {
                    Debug.WriteLine("[LoginPage] Saving credentials (Remember Me enabled)");
                    await _secureStorage.SetRememberMeAsync(true);
                    await _secureStorage.SaveCredentialsAsync(username, password);
                }
                else
                {
                    Debug.WriteLine("[LoginPage] Clearing saved credentials (Remember Me disabled)");
                    await _secureStorage.ClearCredentialsAsync();
                    
                    UsernameEntry.Text = string.Empty;
                    PasswordEntry.Text = string.Empty;
                    Debug.WriteLine("[LoginPage] Cleared input fields");
                }

                var isSeller = result.Roles?.Contains("Vendedor") == true;
                await _navigation.NavigateToMainAsync(isSeller);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginPage] Login error: {ex}");
                ErrorLabel.Text = "Error al iniciar sesión. Intente nuevamente.";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                LoginButton.IsEnabled = true;
            }
        }

        async void GoToRegisterButton_Clicked(object? sender, EventArgs e)
        {
            await _navigation.NavigateToRegisterAsync();
        }
    }
}