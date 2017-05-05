namespace huypq.SmtMiddleware
{
    public interface IEntity
    {
        int ID { get; set; }
        int TenantID { get; set; }
        long LastUpdateTime { get; set; }
        long CreateTime { get; set; }
    }
}
