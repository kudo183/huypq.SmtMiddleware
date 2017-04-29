using System;

namespace huypq.SmtMiddleware.Entities
{
    public class SmtDeletedItem
    {
        public SmtDeletedItem()
        {

        }

        public int ID { get; set; }
        public int TenantID { get; set; }
        public int DeletedID { get; set; }
        public int TableID { get; set; }
        public long CreateTime { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
