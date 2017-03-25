namespace huypq.SmtMiddleware
{
    public class SmtActionResult
    {
        public enum ActionResultType
        {
            Stream = 1,
            Status = 2,
            File = 3,
            Object = 4
        }

        /// <summary>
        /// http response status code
        /// </summary>
        public System.Net.HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Mime type for response content, can retrive from file name by MimeMapping.GetMimeType(string fileName)
        /// </summary>
        public string ContentType { get; set; }

        public object ResultValue { get; set; }
        /// <summary>
        /// Default Name of the being download file, only need if ResultType is File
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// ResultType
        /// </summary>
        public ActionResultType ResultType { get; set; }
    }
}
