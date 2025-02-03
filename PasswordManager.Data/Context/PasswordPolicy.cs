//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PasswordManager.Data.Context
{
    using System;
    using System.Collections.Generic;
    
    public partial class PasswordPolicy
    {
        public int PolicyId { get; set; }
        public int MinLength { get; set; }
        public bool RequireUppercase { get; set; }
        public bool RequireLowercase { get; set; }
        public bool RequireNumbers { get; set; }
        public bool RequireSpecialChars { get; set; }
        public int MaxLoginAttempts { get; set; }
        public int LockoutDurationMinutes { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public Nullable<int> ModifiedByUserId { get; set; }
        public Nullable<bool> IsActive { get; set; }
    
        public virtual User User { get; set; }
    }
}
