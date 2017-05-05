using System;
using System.Collections.Generic;
using huypq.SmtMiddleware;

namespace huypq.SmtMiddlewareTest
{
    public partial class TestData : IEntity
    {
        public TestData()
        {
            TestChildDataTestDataIDNavigation = new HashSet<TestChildData>();
        }

        public long CreateTime { get; set; }
        public string Data { get; set; }
        public int ID { get; set; }
        public long LastUpdateTime { get; set; }
        public int TenantID { get; set; }

        public ICollection<TestChildData> TestChildDataTestDataIDNavigation { get; set; }
    }
}
