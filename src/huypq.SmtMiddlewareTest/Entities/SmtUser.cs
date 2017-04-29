using System;
using System.Collections.Generic;

namespace huypq.SmtMiddlewareTest
{
    public partial class SmtUser : huypq.SmtMiddleware.SmtIUser
    {
        public SmtUser()
        {
            SmtUserClaimUserIDNavigation = new HashSet<SmtUserClaim>();
        }

        public System.DateTime CreateDate { get; set; }
        public string Email { get; set; }
        public int ID { get; set; }
        public string PasswordHash { get; set; }
        public int TenantID { get; set; }
        public long TokenValidTime { get; set; }
        public string UserName { get; set; }
        public long CreateTime { get; set; }
        public long LastUpdateTime { get; set; }
        public bool IsConfirmed { get; set; }

        public bool IsLocked { get; set; }

        public ICollection<SmtUserClaim> SmtUserClaimUserIDNavigation { get; set; }

        public SmtTenant TenantIDNavigation { get; set; }
    }
}
