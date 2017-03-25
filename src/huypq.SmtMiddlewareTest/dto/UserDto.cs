namespace huypq.SmtMiddlewareTest
{
    public class UserDto : SmtMiddleware.SmtIDto
    {
        public System.DateTime CreateDate { get; set; }
        public string Email { get; set; }
        public int ID { get; set; }
        public int TenantID { get; set; }
        public string UserName { get; set; }
        public long TokenValidTime { get; set; }
        public int State { get; set; }
    }
}
