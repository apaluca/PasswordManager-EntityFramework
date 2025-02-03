using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using PasswordManager.Core.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Services
{
        public class KeyVaultService : IKeyVaultService
        {
                private readonly SecretClient _secretClient;

                public KeyVaultService(string keyVaultUrl, string tenantId, string clientId, string clientSecret)
                {
                        var credential = new ClientSecretCredential(
                            tenantId,
                            clientId,
                            clientSecret
                        );

                        _secretClient = new SecretClient(new Uri(keyVaultUrl), credential);
                }

                public async Task<string> GetSecretAsync(string secretName)
                {
                        try
                        {
                                var secret = await _secretClient.GetSecretAsync(secretName);
                                return secret.Value.Value;
                        }
                        catch (Exception ex)
                        {
                                throw new Exception($"Failed to retrieve secret {secretName} from Key Vault", ex);
                        }
                }

                public async Task SetSecretAsync(string secretName, string secretValue)
                {
                        try
                        {
                                await _secretClient.SetSecretAsync(secretName, secretValue);
                        }
                        catch (Exception ex)
                        {
                                throw new Exception($"Failed to set secret {secretName} in Key Vault", ex);
                        }
                }
        }
}