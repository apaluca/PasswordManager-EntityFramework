using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
        public class PasswordPolicyModel
        {
                public int PolicyId { get; set; }
                public int MinLength { get; set; }
                public bool RequireUppercase { get; set; }
                public bool RequireLowercase { get; set; }
                public bool RequireNumbers { get; set; }
                public bool RequireSpecialChars { get; set; }
                public int MaxLoginAttempts { get; set; }
                public int LockoutDurationMinutes { get; set; }
                public DateTime CreatedDate { get; set; }
                public DateTime ModifiedDate { get; set; }
                public int? ModifiedByUserId { get; set; }
                public bool IsActive { get; set; }
                public string ModifiedByUsername { get; set; }
        }
}
