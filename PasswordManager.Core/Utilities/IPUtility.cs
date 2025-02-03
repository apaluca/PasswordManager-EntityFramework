using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Utilities
{
        public static class IPUtility
        {
                // Cache the IP for a certain period to avoid too many API calls
                private static string _cachedIP;
                private static DateTime _lastFetch = DateTime.MinValue;
                private static readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

                public static async Task<string> GetPublicIPAsync()
                {
                        try
                        {
                                // Return cached IP if it's still valid
                                if (_cachedIP != null && (DateTime.Now - _lastFetch) < _cacheTimeout)
                                {
                                        return _cachedIP;
                                }

                                using (var client = new HttpClient())
                                {
                                        // Using ipify API - a free service to get public IP
                                        var response = await client.GetStringAsync("https://api.ipify.org");
                                        _cachedIP = response.Trim();
                                        _lastFetch = DateTime.Now;
                                        return _cachedIP;
                                }
                        }
                        catch
                        {
                                // Fallback if the service is unavailable
                                return "Unknown";
                        }
                }

                // Synchronous version for compatibility
                public static string GetPublicIP()
                {
                        return GetPublicIPAsync().GetAwaiter().GetResult();
                }
        }
}
