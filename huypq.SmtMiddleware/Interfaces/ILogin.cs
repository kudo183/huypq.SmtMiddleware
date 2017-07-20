namespace huypq.SmtMiddleware
{
    public interface ILogin
    {
        bool IsLocked { get; set; }
        string Email { get; set; }
        string PasswordHash { get; set; }
        long TokenValidTime { get; set; }
    }
}
