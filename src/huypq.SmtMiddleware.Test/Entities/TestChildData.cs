using System;
using System.Collections.Generic;

namespace huypq.SmtMiddleware.Test
{
    public partial class TestChildData : huypq.SmtMiddleware.IEntity
    {
        public TestChildData()
        {
        }

        public long CreateTime { get; set; }
        public string Data { get; set; }
        public int ID { get; set; }
        public long LastUpdateTime { get; set; }
        public int TenantID { get; set; }
        public int TestDataID { get; set; }


        public TestData TestDataIDNavigation { get; set; }
    }
}
