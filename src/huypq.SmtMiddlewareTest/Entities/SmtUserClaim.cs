using System;
using System.Collections.Generic;

namespace huypq.SmtMiddlewareTest
{
    public partial class SmtUserClaim : huypq.SmtMiddleware.SmtIUserClaim
    {
        public SmtUserClaim()
        {
        }

        public string Claim { get; set; }
        public int ID { get; set; }
        public int TenantID { get; set; }
        public int UserID { get; set; }
        public long LastUpdateTime { get; set; }


        public SmtTenant TenantIDNavigation { get; set; }
        public SmtUser UserIDNavigation { get; set; }
    }
}
