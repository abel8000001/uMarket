using System.Collections.Generic;
using System.Threading.Tasks;

namespace uMarket.Services
{
    /// <summary>
    /// Navigation service interface for centralized, testable navigation
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigate to a route with optional parameters
        /// </summary>
        Task NavigateToAsync(string route, IDictionary<string, object>? parameters = null);

        /// <summary>
        /// Navigate back in the navigation stack
        /// </summary>
        Task NavigateBackAsync();

        /// <summary>
        /// Navigate to the main app based on user role
        /// </summary>
        Task NavigateToMainAsync(bool isSeller);

        /// <summary>
        /// Navigate to login page
        /// </summary>
        Task NavigateToLoginAsync();

        /// <summary>
        /// Navigate to register page
        /// </summary>
        Task NavigateToRegisterAsync();

        /// <summary>
        /// Navigate to chat page with conversation ID
        /// </summary>
        Task NavigateToChatAsync(System.Guid conversationId);

        /// <summary>
        /// Navigate to role selection page
        /// </summary>
        Task NavigateToRoleSelectionAsync();
    }
}
