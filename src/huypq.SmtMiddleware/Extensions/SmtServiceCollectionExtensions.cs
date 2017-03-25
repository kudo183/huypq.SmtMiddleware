using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;

namespace huypq.SmtMiddleware
{
    public static class SmtServiceCollectionExtensions
    {
        /// <summary>
        /// With Token Authentication and sql Trusted connection
        /// </summary>
        /// <typeparam name="ContextType"></typeparam>
        /// <typeparam name="UserEntityType"></typeparam>
        /// <typeparam name="UserClaimEntityType"></typeparam>
        /// <param name="services"></param>
        /// <param name="dbName">Sql connection string to store user login info</param>
        /// <param name="tokenEncryptKeyDirectoryPath">Directory to store key use to encrypt Token</param>
        /// <returns></returns>
        public static IServiceCollection AddSmtWithTrustedConnection<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType>(
            this IServiceCollection services, string dbName, string tokenEncryptKeyDirectoryPath)
            where TenantEntityType : class, SmtITenant
            where UserEntityType : class, SmtIUser
            where UserClaimEntityType : class, SmtIUserClaim
            where ContextType : DbContext, SmtIDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
        {
            var connection = string.Format(@"Server=.;Database={0};Trusted_Connection=True;", dbName);

            return AddSmt<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType>(services, connection, tokenEncryptKeyDirectoryPath);
        }

        /// <summary>
        /// With Token Authentication
        /// </summary>
        /// <typeparam name="ContextType"></typeparam>
        /// <typeparam name="UserEntityType"></typeparam>
        /// <typeparam name="UserClaimEntityType"></typeparam>
        /// <param name="services"></param>
        /// <param name="connection">Sql connection string to store user login info</param>
        /// <param name="tokenEncryptKeyDirectoryPath">Directory to store key use to encrypt Token</param>
        /// <returns></returns>
        public static IServiceCollection AddSmt<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType>(
            this IServiceCollection services, string connection, string tokenEncryptKeyDirectoryPath)
            where TenantEntityType : class, SmtITenant
            where UserEntityType : class, SmtIUser
            where UserClaimEntityType : class, SmtIUserClaim
            where ContextType : DbContext, SmtIDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
        {
            services.AddDbContext<ContextType>(options => options.UseSqlServer(connection), ServiceLifetime.Scoped);
            services.AddDataProtection()
                .PersistKeysToFileSystem(new System.IO.DirectoryInfo(tokenEncryptKeyDirectoryPath))
                .ProtectKeysWithDpapi();
            return AddSmt(services);
        }        

        /// <summary>
        /// without authentication
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        private static IServiceCollection AddSmt(this IServiceCollection services)
        {
            services.AddRouting();

            return services;
        }
    }
}
