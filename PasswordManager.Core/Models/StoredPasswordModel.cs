﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
        public class StoredPasswordModel
        {
                public int Id { get; set; }
                public int UserId { get; set; }
                public int? GroupId { get; set; }
                public string SiteName { get; set; }
                public string SiteUrl { get; set; }
                public string Username { get; set; }
                public string EncryptedPassword { get; set; }
                public string Notes { get; set; }
                public DateTime? CreatedDate { get; set; }
                public DateTime? ModifiedDate { get; set; }
                public DateTime? ExpirationDate { get; set; }
                public string GroupName { get; set; }

                public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.Now;
                public bool IsExpiringSoon =>
                            ExpirationDate.HasValue &&
                            ExpirationDate.Value <= DateTime.Now.AddDays(30) &&
                            ExpirationDate.Value > DateTime.Now;
        }
}
