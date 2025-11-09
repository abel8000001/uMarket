using System;
using Microsoft.Maui.Controls;
using uMarket.Services;
using uMarket.Models.Auth;

namespace uMarket.Pages
{
    public partial class RegisterPage : ContentPage
    {
        readonly AuthService _auth;
        readonly UserProfileService _profile;
        readonly INavigationService _navigation;

        bool _isPasswordVisible = false;

        public RegisterPage(AuthService auth, UserProfileService profile, INavigationService navigation)
        {
            InitializeComponent();
            _auth = auth;
            _profile = profile;
            _navigation = navigation;

            // Ensure the toggle shows the correct initial icon
            PasswordToggleButton.Source = "eye_hidden.png";
        }

        // Prevent hardware/back-button navigation while on the register screen.
        protected override bool OnBackButtonPressed()
        {
            // Cancel back navigation while on the register page.
            return true;
        }

        void PasswordToggleButton_Clicked(object? sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            PasswordEntry.IsPassword = !_isPasswordVisible;
            ConfirmPasswordEntry.IsPassword = !_isPasswordVisible;
            PasswordToggleButton.Source = _isPasswordVisible ? "eye_visible.png" : "eye_hidden.png";
        }

        async void RegisterButton_Clicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;
            RegisterButton.IsEnabled = false;

            try
            {
                var fullName = FullNameEntry.Text?.Trim() ?? string.Empty;
                var username = UsernameEntry.Text?.Trim() ?? string.Empty;
                var password = PasswordEntry.Text ?? string.Empty;
                var confirm = ConfirmPasswordEntry.Text ?? string.Empty;

                // Determine selected role: prefer Picker, fall back to radio buttons if present
                string role = string.Empty;
                var rolePicker = this.FindByName<Picker>("RolePicker");
                if (rolePicker?.SelectedItem is string rp && !string.IsNullOrEmpty(rp))
                {
                    role = rp;
                }
                else
                {
                    var roleStudent = this.FindByName<RadioButton>("RoleStudentRadio");
                    var roleVendor = this.FindByName<RadioButton>("RoleVendorRadio");
                    if (roleStudent?.IsChecked == true) role = "Estudiante";
                    else if (roleVendor?.IsChecked == true) role = "Vendedor";
                }

                // Validation
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    ErrorLabel.Text = "Ingrese su nombre completo";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                if (!int.TryParse(username, out var id))
                {
                    ErrorLabel.Text = "Ingrese su ID institucional como nombre de usuario";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                if (password.Length < 6)
                {
                    ErrorLabel.Text = "La contraseña debe tener al menos 6 caracteres";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                if (password != confirm)
                {
                    ErrorLabel.Text = "Las contraseñas no coinciden";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                if (string.IsNullOrEmpty(role))
                {
                    ErrorLabel.Text = "Seleccione un rol";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                var result = await _auth.RegisterAsync(id, fullName, password, role);
                if (result == null)
                {
                    ErrorLabel.Text = "Registro fallido: ID ya registrado u otros errores";
                    ErrorLabel.IsVisible = true;
                    return;
                }

                await _profile.SaveProfileAsync(result);

                // Navigate to main app based on role
                var isSeller = role == "Vendedor";
                await _navigation.NavigateToMainAsync(isSeller);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegisterPage] Register error: {ex}");
                ErrorLabel.Text = "Error al registrar. Intente nuevamente.";
                ErrorLabel.IsVisible = true;
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                RegisterButton.IsEnabled = true;
            }
        }

        async void GoToLoginButton_Clicked(object? sender, EventArgs e)
        {
            await _navigation.NavigateToLoginAsync();
        }
    }
}
