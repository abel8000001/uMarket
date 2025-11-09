using System;

namespace uMarket
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();

            // Force app to use Light theme regardless of device setting
            this.UserAppTheme = AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}