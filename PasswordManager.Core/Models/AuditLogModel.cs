using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
        public class AuditLogModel
        {
                public DateTime? ActionDate { get; set; }
                public string Action { get; set; }
                public string Details { get; set; }
                public string Username { get; set; }
        }
}
