using System.Collections.Generic;
using huypq.SmtMiddleware;

namespace huypq.SmtMiddleware.Entities
{
    public partial class SmtTenant : ITenant
    {
        public SmtTenant()
        {
            SmtUserTenantIDNavigation = new HashSet<SmtUser>();
            SmtUserClaimTenantIDNavigation = new HashSet<SmtUserClaim>();
        }

        public int ID { get; set; }
        public System.DateTime CreateDate { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string TenantName { get; set; }
        public long TokenValidTime { get; set; }
        public long LastUpdateTime { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsLocked { get; set; }


		
		public ICollection<SmtUser> SmtUserTenantIDNavigation { get; set; }
		public ICollection<SmtUserClaim> SmtUserClaimTenantIDNavigation { get; set; }
    }
}
