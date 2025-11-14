using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace uMarket.Services
{
    /// Shell-based navigation service implementation
    public class NavigationService : INavigationService
    {
        public async Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));

            try
            {
                if (parameters != null && parameters.Any())
                {
                    var navigationParameter = new ShellNavigationQueryParameters();
                    foreach (var param in parameters)
                    {
                        navigationParameter.Add(param.Key, param.Value);
                    }
                    await Shell.Current.GoToAsync(route, navigationParameter);
                }
                else
                {
                    await Shell.Current.GoToAsync(route);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigation failed: {ex}");
                throw;
            }
        }

        public async Task NavigateBackAsync()
        {
            try
            {
                // Try navigation stack pop first
                if (Shell.Current.Navigation.NavigationStack.Count > 1)
                {
                    await Shell.Current.Navigation.PopAsync();
                    return;
                }

                // Fallback to Shell back navigation
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Back navigation failed: {ex}");
                throw;
            }
        }

        public async Task NavigateToMainAsync(bool isSeller)
        {
            try
            {
                // Configure AppShell to show appropriate content based on role
                // This must be done BEFORE navigation so the route exists
                if (Shell.Current is AppShell appShell)
                {
                    appShell.ShowRoleBasedContent(isSeller);
                }

                // Small delay to ensure Shell has registered the new route
                await Task.Delay(100);

                // Navigate to the main route (both student and seller use "main" route)
                await Shell.Current.GoToAsync("//main");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigate to main failed: {ex}");
                throw;
            }
        }

        public async Task NavigateToLoginAsync()
        {
            try
            {
                // Hide all role-based content when logging out
                if (Shell.Current is AppShell appShell)
                {
                    appShell.HideAllRoleContent();
                }

                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigate to login failed: {ex}");
                throw;
            }
        }

        public async Task NavigateToRegisterAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(Pages.RegisterPage));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigate to register failed: {ex}");
                throw;
            }
        }

        public async Task NavigateToChatAsync(Guid conversationId)
        {
            try
            {
                await NavigateToAsync(
                    nameof(Pages.ChatPage),
                    new Dictionary<string, object>
                    {
                        { "conversationId", conversationId.ToString() }
                    }
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigate to chat failed: {ex}");
                throw;
            }
        }

        public async Task NavigateToRoleSelectionAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("RoleSelectionPage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NavigationService] Navigate to role selection failed: {ex}");
                throw;
            }
        }
    }
}
