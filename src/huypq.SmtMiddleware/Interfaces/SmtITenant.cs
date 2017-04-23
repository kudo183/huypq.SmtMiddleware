namespace huypq.SmtMiddleware
{
    public interface SmtITenant: SmtILogin
    {
        int ID { get; }
        System.DateTime CreateDate { get; set; }
        string TenantName { get; set; }
    }
}
