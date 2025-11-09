using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace uMarket.Services
{
    public class SecureStorageService
    {
        private const string RememberMeKey = "remember_me";
        private const string SavedUsernameKey = "saved_username";
        private const string SavedPasswordKey = "saved_password";

        public async Task<bool> GetRememberMeAsync()
        {
            try
            {
                var value = await SecureStorage.GetAsync(RememberMeKey);
                return value == "true";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SecureStorageService] GetRememberMe error: {ex}");
                return false;
            }
        }

        public async Task SetRememberMeAsync(bool remember)
        {
            try
            {
                await SecureStorage.SetAsync(RememberMeKey, remember ? "true" : "false");
                Debug.WriteLine($"[SecureStorageService] Remember me set to: {remember}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SecureStorageService] SetRememberMe error: {ex}");
            }
        }

        public async Task SaveCredentialsAsync(string username, string password)
        {
            try
            {
                await SecureStorage.SetAsync(SavedUsernameKey, username);
                await SecureStorage.SetAsync(SavedPasswordKey, password);
                Debug.WriteLine($"[SecureStorageService] Credentials saved for user: {username}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SecureStorageService] SaveCredentials error: {ex}");
            }
        }

        public async Task<(string? username, string? password)> GetSavedCredentialsAsync()
        {
            try
            {
                var username = await SecureStorage.GetAsync(SavedUsernameKey);
                var password = await SecureStorage.GetAsync(SavedPasswordKey);
                return (username, password);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SecureStorageService] GetSavedCredentials error: {ex}");
                return (null, null);
            }
        }

        public async Task ClearCredentialsAsync()
        {
            try
            {
                SecureStorage.Remove(SavedUsernameKey);
                SecureStorage.Remove(SavedPasswordKey);
                SecureStorage.Remove(RememberMeKey);
                Debug.WriteLine("[SecureStorageService] Credentials cleared");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SecureStorageService] ClearCredentials error: {ex}");
            }
        }
    }
}
