namespace huypq.SmtMiddleware
{
    public interface IUser: IEntity, ILogin
    {
        System.DateTime CreateDate { get; set; }
        string UserName { get; set; }
    }
}
