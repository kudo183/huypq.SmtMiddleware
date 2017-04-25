using System;
using System.Collections.Generic;

namespace huypq.SmtMiddleware
{
    public sealed class SmtSettings
    {
        private static readonly SmtSettings _instance = new SmtSettings()
        {
            DefaultPageSize = 50,
            DefaultOrderOption = new QueryBuilder.OrderByExpression.OrderOption() { PropertyPath = "ID", IsAscending = false },
            MaxItemAllowed = 1000,
            AllowAnonymousActions = new List<string>(),
            DefaultPermissions = new Dictionary<string, List<string>>(),
            JsonSerializer = new SmtJsonSerializer(),
            BinarySerializer = new SmtProtobufSerializer(),
            EmailFolderPath = @"c:\emails"
        };
        
        public static SmtSettings Instance
        {
            get { return _instance; }
        }
        
        /// <summary>
        /// Email folder path
        /// </summary>
        public string EmailFolderPath { get; set; }

        /// <summary>
        /// Default page size
        /// </summary>
        public int DefaultPageSize { get; set; }

        /// <summary>
        /// Default order option
        /// </summary>
        public QueryBuilder.OrderByExpression.OrderOption DefaultOrderOption { get; set; }

        /// <summary>
        /// Max Item Allowed per request
        /// </summary>
        public int MaxItemAllowed { get; set; }

        /// <summary>
        /// Specify list of action which allow anonymous user (not check Header["token"])
        /// </summary>
        public List<string> AllowAnonymousActions { get; set; }

        /// <summary>
        /// Specify list of action which is allowed for logged in user
        /// </summary>
        public Dictionary<string, List<string>> DefaultPermissions { get; set; }

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
