namespace huypq.SmtMiddleware
{
    public interface ITenant: ILogin
    {
        int ID { get; }
        System.DateTime CreateDate { get; set; }
        string TenantName { get; set; }
    }
}
