namespace huypq.SmtMiddleware
{
    public interface SmtIUser: SmtIEntity, SmtILogin
    {
        System.DateTime CreateDate { get; set; }
        string UserName { get; set; }
    }
}
