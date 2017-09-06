namespace huypq.SmtMiddleware.Entities
{
    public class SmtFile : IFileEntity
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public int ID { get; set; }
        public int TenantID { get; set; }
        public long LastUpdateTime { get; set; }
        public long CreateTime { get; set; }
    }
}
