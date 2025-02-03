using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
        public enum AlertType
        {
                BruteForceAttempt,
                SuspiciousIP,
                MultipleFailures,
                UnusualLoginTime,
                PasswordExpiring,
                AccountLocked,
                PolicyViolation,
                SystemChange
        }
}
