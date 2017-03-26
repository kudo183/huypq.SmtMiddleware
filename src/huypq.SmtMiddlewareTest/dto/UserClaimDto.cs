using System;
using huypq.SmtShared;

namespace huypq.SmtMiddlewareTest
{
    public class UserClaimDto : SmtIDto
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int TenantID { get; set; }
        public string Claim { get; set; }
        public int State { get; set; }

        public bool HasChange()
        {
            throw new NotImplementedException();
        }
    }
}
