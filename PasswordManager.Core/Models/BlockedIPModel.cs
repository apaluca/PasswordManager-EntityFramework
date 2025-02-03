using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
        public class BlockedIPModel
        {
                public int BlockId { get; set; }
                public string IPAddress { get; set; }
                public string Reason { get; set; }
                public DateTime BlockedDate { get; set; }
                public int BlockedByUserId { get; set; }
                public DateTime? ExpiryDate { get; set; }
                public bool IsActive { get; set; }
                public string Notes { get; set; }
                public string BlockedByUsername { get; set; }
        }
}
