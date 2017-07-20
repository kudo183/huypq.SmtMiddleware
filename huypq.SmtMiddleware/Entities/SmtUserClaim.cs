using System.Collections.Generic;
using huypq.SmtMiddleware;

namespace huypq.SmtMiddleware.Entities
{
    public partial class SmtUserClaim : IUserClaim
    {
        public SmtUserClaim()
        {

        }

        public int ID { get; set; }
        public int UserID { get; set; }
        public string Claim { get; set; }
        public int TenantID { get; set; }
        public long LastUpdateTime { get; set; }
        public long CreateTime { get; set; }

        public SmtUser UserIDNavigation { get; set; }
        public SmtTenant TenantIDNavigation { get; set; }
		

    }
}
