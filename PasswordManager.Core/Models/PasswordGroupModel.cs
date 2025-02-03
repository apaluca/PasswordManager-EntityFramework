using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PasswordManager.Core.Models
{
        public class PasswordGroupModel
        {
                public int GroupId { get; set; }
                public int UserId { get; set; }
                public string GroupName { get; set; }
                public string Description { get; set; }
                public DateTime? CreatedDate { get; set; }
                public DateTime? ModifiedDate { get; set; }
                public ICollection<StoredPasswordModel> Passwords { get; set; }

                public PasswordGroupModel()
                {
                        Passwords = new List<StoredPasswordModel>();
                }
        }
}
