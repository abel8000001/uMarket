using System.Collections.Generic;
using System.Threading.Tasks;

namespace uMarket.Services
{
    /// Navigation service interface for centralized, testable navigation
    public interface INavigationService
    {
        /// Navigate to a route with optional parameters
        Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);

        /// Navigate back in the navigation stack
        Task NavigateBackAsync();

        /// Navigate to the main app based on user role
        Task NavigateToMainAsync(bool isSeller);

        /// Navigate to login page
        Task NavigateToLoginAsync();

        /// Navigate to register page
        Task NavigateToRegisterAsync();

        /// Navigate to chat page with conversation ID
        Task NavigateToChatAsync(System.Guid conversationId);

        /// Navigate to role selection page
        Task NavigateToRoleSelectionAsync();
    }
}
