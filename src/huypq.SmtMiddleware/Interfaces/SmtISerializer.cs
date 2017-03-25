namespace huypq.SmtMiddleware
{
    public interface SmtISerializer
    {
        void Serialize(System.IO.Stream output, object data);
        T Deserialize<T>(System.IO.Stream data);
        T Deserialize<T>(object data);
    }
}
