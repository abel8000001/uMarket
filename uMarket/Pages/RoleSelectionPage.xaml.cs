using Microsoft.Maui.Controls;
using uMarket.Services;

namespace uMarket.Pages
{
    public partial class RoleSelectionPage : ContentPage
    {
        readonly INavigationService _navigation;

        public RoleSelectionPage(INavigationService navigation)
        {
            InitializeComponent();
            _navigation = navigation;
        }

        private async void StudentButton_Clicked(object? sender, System.EventArgs e)
        {
            await _navigation.NavigateToMainAsync(isSeller: false);
        }

        private async void SellerButton_Clicked(object? sender, System.EventArgs e)
        {
            await _navigation.NavigateToMainAsync(isSeller: true);
        }
    }
}
