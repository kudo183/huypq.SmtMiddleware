namespace huypq.SmtMiddleware
{
    public interface SmtITenant
    {
        int ID { get; }
        string Email { get; set; }
        string PasswordHash { get; set; }
        System.DateTime CreateDate { get; set; }
        string TenantName { get; set; }
        long TokenValidTime { get; set; }
    }
}
