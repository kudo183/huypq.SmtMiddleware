namespace huypq.SmtMiddlewareTest
{
    public class UserClaimDto : SmtMiddleware.SmtIDto
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public int TenantID { get; set; }
        public string Claim { get; set; }
        public int State { get; set; }
    }
}
