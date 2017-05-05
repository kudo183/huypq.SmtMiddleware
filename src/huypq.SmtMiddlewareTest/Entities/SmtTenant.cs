using System;
using System.Collections.Generic;

namespace huypq.SmtMiddlewareTest
{
    public partial class SmtTenant : huypq.SmtMiddleware.ITenant
    {
        public SmtTenant()
        {
            SmtUserTenantIDNavigation = new HashSet<SmtUser>();
            SmtUserClaimTenantIDNavigation = new HashSet<SmtUserClaim>();
        }

        public System.DateTime CreateDate { get; set; }
        public string Email { get; set; }
        public int ID { get; set; }
        public string PasswordHash { get; set; }
        public string TenantName { get; set; }
        public long TokenValidTime { get; set; }

        public ICollection<SmtUser> SmtUserTenantIDNavigation { get; set; }
        public ICollection<SmtUserClaim> SmtUserClaimTenantIDNavigation { get; set; }

        public bool IsConfirmed { get; set; }

        public bool IsLocked { get; set; }
    }
}
