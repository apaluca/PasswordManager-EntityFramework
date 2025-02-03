using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PasswordManager.Core.Utilities;

namespace PasswordManager.Core.Models
{
        public class SecurityAlertModel
        {
                public int AlertId { get; set; }
                public AlertType AlertType { get; set; }
                public AlertSeverity Severity { get; set; }
                public string Description { get; set; }
                public string Source { get; set; }
                public string IPAddress { get; set; }
                public string Username { get; set; }
                public DateTime CreatedDate { get; set; }
                public bool IsResolved { get; set; }
                public int? ResolvedByUserId { get; set; }
                public DateTime? ResolvedDate { get; set; }
                public string Notes { get; set; }
                public string ResolvedByUsername { get; set; }
        }
}
