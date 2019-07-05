using System.Collections.Generic;

namespace huypq.SmtMiddleware
{
    public sealed class SmtSettings
    {
        public static SmtSettings Instance { get; } = new SmtSettings()
        {
            DefaultOrderOption = new QueryBuilder.OrderByExpression.OrderOption() { PropertyPath = nameof(IEntity.ID), IsAscending = true },
            MaxItemAllowed = 1000,
            AllowAnonymousActions = new List<string>(),
            DefaultPermissions = new Dictionary<string, List<string>>(),
            JsonSerializer = new SmtJsonSerializer(),
            BinarySerializer = new SmtProtobufSerializer(),
            EmailFolderPath = @"c:\emails",
            SmtFileDirectoryPath = @"c:\smtfile",
            SkipTenantFilterTables = new List<string>()
        };

        /// <summary>
        /// SmtFile directory path
        /// </summary>
        public string SmtFileDirectoryPath { get; set; }

        /// <summary>
        /// Email folder path
        /// </summary>
        public string EmailFolderPath { get; set; }

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
        public ISerializer JsonSerializer { get; set; }

        /// <summary>
        /// Use for serialize response if Header["response"]="protobuf"
        /// </summary>
        public ISerializer BinarySerializer { get; set; }

        /// <summary>
        /// Server version, use for check if client is out of date
        /// </summary>
        public int ServerVersion { get; set; } = 0;

        /// <summary>
        /// Google client id for open id token check
        /// </summary>
        public List<string> GoogleClientIDs { get; set; }

        /// <summary>
        /// Facebook AppSecret for token check
        /// </summary>
        public string FacebookAppSecret { get; set; }

        /// <summary>
        /// Facebook UserInfo EndPoint for token check and email
        /// </summary>
        public string FacebookUserInfoEndPoint { get; set; }

        /// <summary>
        /// Specify list of table which allow get all data without tenantID check (no .Where(p => p.TenantID == TokenModel.TenantID))
        /// </summary>
        public List<string> SkipTenantFilterTables { get; set; }

        /// <summary>
        /// Specify list of table which allow GetAll action (no MaxItemAllowed check)
        /// </summary>
        public List<string> AllowGetAllTables { get; set; }
    }
}
