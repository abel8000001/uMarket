using Microsoft.Maui.Controls;
using System.Linq;

namespace uMarket
{
    public partial class AppShell : Shell
    {
        private ShellContent? _currentRoleContent;

        public AppShell()
        {
            InitializeComponent();

            // Register modal/detail pages (not in visual hierarchy)
            Routing.RegisterRoute(nameof(Pages.ChatPage), typeof(Pages.ChatPage));
            Routing.RegisterRoute(nameof(Pages.RegisterPage), typeof(Pages.RegisterPage));
            Routing.RegisterRoute("RoleSelectionPage", typeof(Pages.RoleSelectionPage));
        }

        /// <summary>
        /// Show the appropriate content based on user role by adding it to Shell
        /// </summary>
        public void ShowRoleBasedContent(bool isSeller)
        {
            // Remove any existing role content first
            if (_currentRoleContent != null && Items.Contains(_currentRoleContent))
            {
                Items.Remove(_currentRoleContent);
                _currentRoleContent = null;
            }

            // Create and add the appropriate ShellContent for the role
            if (isSeller)
            {
                _currentRoleContent = new ShellContent
                {
                    Title = "Panel",
                    Route = "main",
                    ContentTemplate = new DataTemplate(typeof(Pages.SellerMainPage))
                };
            }
            else
            {
                _currentRoleContent = new ShellContent
                {
                    Title = "Vendedores",
                    Route = "main",
                    ContentTemplate = new DataTemplate(typeof(Pages.StudentMainPage))
                };
            }

            // Add the new content to Shell
            Items.Add(_currentRoleContent);
        }

        /// <summary>
        /// Hide all role-based content (for login/logout) by removing it from Shell
        /// </summary>
        public void HideAllRoleContent()
        {
            if (_currentRoleContent != null && Items.Contains(_currentRoleContent))
            {
                Items.Remove(_currentRoleContent);
                _currentRoleContent = null;
            }
        }
    }
}
