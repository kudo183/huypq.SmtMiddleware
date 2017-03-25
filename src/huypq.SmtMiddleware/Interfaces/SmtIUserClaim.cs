namespace huypq.SmtMiddleware
{
    public interface SmtIUserClaim: SmtIEntity
    {
        int UserID { get; set; }
        string Claim { get; set; }
    }
}
