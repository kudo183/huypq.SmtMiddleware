using System.Collections.Generic;
using huypq.SmtMiddleware;

namespace huypq.SmtMiddleware.Entities
{
    public partial class SmtUser : IUser
    {
        public SmtUser()
        {
            SmtUserClaimUserIDNavigation = new HashSet<SmtUserClaim>();
        }

        public int ID { get; set; }
        public System.DateTime CreateDate { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string UserName { get; set; }
        public int TenantID { get; set; }
        public long TokenValidTime { get; set; }
        public long LastUpdateTime { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsLocked { get; set; }
        public long CreateTime { get; set; }

        public SmtTenant TenantIDNavigation { get; set; }
		
		public ICollection<SmtUserClaim> SmtUserClaimUserIDNavigation { get; set; }
    }
}
