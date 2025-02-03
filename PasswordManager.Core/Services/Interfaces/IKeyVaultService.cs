using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Services.Interfaces
{
        public interface IKeyVaultService
        {
                Task<string> GetSecretAsync(string secretName);
                Task SetSecretAsync(string secretName, string secretValue);
        }
}
