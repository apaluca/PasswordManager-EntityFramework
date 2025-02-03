using PasswordManager.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Configuration
{
        public class AppConfiguration
        {
                private readonly IKeyVaultService _keyVaultService;
                private static AppConfiguration _instance;
                private string _connectionString;
                private string _encryptionKey;

                public static AppConfiguration Instance => _instance ??
                    throw new InvalidOperationException("Configuration not initialized");

                private AppConfiguration(IKeyVaultService keyVaultService)
                {
                        _keyVaultService = keyVaultService;
                }

                public static async Task InitializeAsync(IKeyVaultService keyVaultService)
                {
                        var config = new AppConfiguration(keyVaultService);
                        await config.LoadSecretsAsync();
                        _instance = config;
                }

                private async Task LoadSecretsAsync()
                {
                        _encryptionKey = await _keyVaultService.GetSecretAsync("EncryptionKey");
                }

                public string EncryptionKey => _encryptionKey ??
                    throw new InvalidOperationException("Encryption key not loaded");
        }
}
