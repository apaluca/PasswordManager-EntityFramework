using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
        public class SecurityMetricsModel
        {
                public int TotalActiveAlerts { get; set; }
                public int CriticalAlerts { get; set; }
                public int BlockedIPs { get; set; }
                public int RecentFailedLogins { get; set; }
                public int ExpiredPasswords { get; set; }
                public DateTime LastPolicyUpdate { get; set; }
                public int AccountLockouts { get; set; }
        }
}
