using System;
using System.Collections.Generic;

namespace huypq.SmtMiddleware.Test
{
    public partial class SmtUserClaim : huypq.SmtMiddleware.IUserClaim
    {
        public SmtUserClaim()
        {
        }

        public string Claim { get; set; }
        public int ID { get; set; }
        public int TenantID { get; set; }
        public int UserID { get; set; }
        public long CreateTime { get; set; }
        public long LastUpdateTime { get; set; }

        public SmtTenant TenantIDNavigation { get; set; }
        public SmtUser UserIDNavigation { get; set; }
    }
}
