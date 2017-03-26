using System;
using huypq.SmtShared;

namespace huypq.SmtMiddlewareTest
{
    public class UserDto : SmtIDto
    {
        public System.DateTime CreateDate { get; set; }
        public string Email { get; set; }
        public int ID { get; set; }
        public int TenantID { get; set; }
        public string UserName { get; set; }
        public long TokenValidTime { get; set; }
        public int State { get; set; }

        public bool HasChange()
        {
            throw new NotImplementedException();
        }
    }
}
