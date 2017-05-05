namespace huypq.SmtMiddleware
{
    public interface IUserClaim: IEntity
    {
        int UserID { get; set; }
        string Claim { get; set; }
    }
}
