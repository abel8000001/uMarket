using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using uMarket.Services;
using uMarket.Pages;
using CommunityToolkit.Maui;

namespace uMarket
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Use compile-time ApiSettings as the single source of truth for the server URL
            var apiBase = ApiSettings.BaseUrl ?? throw new InvalidOperationException("ApiSettings.BaseUrl must be set.");
            var hubUrl = apiBase.TrimEnd('/') + "/hubs/chat";

            // Register Navigation Service
            builder.Services.AddSingleton<INavigationService, NavigationService>();

            // Register a simple HttpClient for API calls.
            builder.Services.AddSingleton<AuthService>(sp =>
            {
                var http = new HttpClient { BaseAddress = new Uri(apiBase), Timeout = TimeSpan.FromSeconds(30) };
                return new AuthService(http);
            });

            // Register TokenService and UserProfileService for token/profile handling
            builder.Services.AddSingleton<TokenService>();
            builder.Services.AddSingleton<UserProfileService>();

            // Register HubService with default hub url
            builder.Services.AddSingleton<HubService>(sp => new HubService(hubUrl));

            // Register SellersService for fetching available sellers
            builder.Services.AddSingleton<SellersService>(sp => new SellersService(apiBase));

            // Register ConversationsService with UserProfileService dependency
            builder.Services.AddSingleton<ConversationsService>(sp =>
            {
                var profile = sp.GetRequiredService<UserProfileService>();
                return new ConversationsService(apiBase, profile);
            });

            // Register SecureStorageService in dependency injection container
            builder.Services.AddSingleton<SecureStorageService>();

            // Page registrations
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<RoleSelectionPage>();
            builder.Services.AddTransient<StudentMainPage>();
            builder.Services.AddTransient<SellerMainPage>();
            builder.Services.AddTransient<ChatPage>();

            var app = builder.Build();

            return app;
        }
    }
}