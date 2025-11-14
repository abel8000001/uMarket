namespace uMarket
{
    /// API Settings - Base URL configured per environment
    public static class ApiSettings
    {
#if DEBUG
        // Development: Local server
        public static string BaseUrl { get; set; } = "http://localhost:5000/";
#else
        // Production: Azure App Service
        public static string BaseUrl { get; set; } = "https://umarket-api-gee3bbewh7evg3a5.canadacentral-01.azurewebsites.net/";
#endif
    }
}
