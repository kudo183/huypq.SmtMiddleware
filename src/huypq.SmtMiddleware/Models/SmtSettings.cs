using System;
using System.Collections.Generic;

namespace huypq.SmtMiddleware
{
    public sealed class SmtSettings
    {
        private static readonly SmtSettings _instance = new SmtSettings()
        {
            DefaultPageSize = 50,
            MaxItemAllowed = 1000,
            AllowAnonymousActions = new List<string>(),
            JsonSerializer = new SmtJsonSerializer(),
            BinarySerializer = new SmtProtobufSerializer()
        };

        //because javascript max number is 53 bit, so need substract some date to make this number smaller
        public static long ServerStartTime = DateTime.UtcNow.Ticks - new DateTime(2015, 1, 1).Ticks;

        public static SmtSettings Instance
        {
            get { return _instance; }
        }

        /// <summary>
        /// Result paging size
        /// </summary>
        public int DefaultPageSize { get; set; }

        /// <summary>
        /// Result paging size
        /// </summary>
        public int MaxItemAllowed { get; set; }
        
        /// <summary>
        /// Specify list of action which allow anonymous user
        /// Default is contain "user.register" for register acion
        /// </summary>
        public List<string> AllowAnonymousActions { get; set; }

        /// <summary>
        /// Use for deserialize request parameter and serialize response if Header["response"]="json"
        /// </summary>
        public SmtISerializer JsonSerializer { get; set; }

        /// <summary>
        /// Use for serialize response if Header["response"]="protobuf"
        /// </summary>
        public SmtISerializer BinarySerializer { get; set; }
    }
}
