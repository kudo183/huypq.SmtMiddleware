namespace huypq.SmtMiddleware
{
    public interface SmtIUser: SmtIEntity
    {
        string Email { get; set; }
        string PasswordHash { get; set; }
        System.DateTime CreateDate { get; set; }
        string UserName { get; set; }
        long TokenValidTime { get; set; }
    }
}
