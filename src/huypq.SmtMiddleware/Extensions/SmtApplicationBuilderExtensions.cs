using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace huypq.SmtMiddleware
{
    public static class SmtApplicationBuilderExtensions
    {
        /// <summary>
        /// Extension methods for <see cref="IApplicationBuilder"/> to add Smt to the request execution pipeline.
        /// All config can be set in singleton SmtSettings.Instance.
        /// Dependence services: Routing
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="applicationNamespace">the namespace of Startup class</param>
        /// <returns></returns>
        public static IApplicationBuilder UseSmt<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType>(
            this IApplicationBuilder app, string applicationNamespace)
            where TenantEntityType : class, SmtITenant, new()
            where UserEntityType : class, SmtIUser, new()
            where UserClaimEntityType : class, SmtIUserClaim
            where ContextType : DbContext, SmtIDbContext<TenantEntityType, UserEntityType, UserClaimEntityType>
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (string.IsNullOrEmpty(applicationNamespace))
            {
                throw new ArgumentNullException(nameof(applicationNamespace));
            }

            var entryAssembly = Assembly.Load(new AssemblyName(applicationNamespace));

             var controllerNamespacePattern = string.Format("{0}.Controllers.{{0}}Controller, {1}",
                entryAssembly.FullName.Split(',')[0], entryAssembly.FullName);
            
            var smtRouter = new SmtRouter<ContextType, TenantEntityType, UserEntityType, UserClaimEntityType>(app, controllerNamespacePattern);
            
            var routeBuilder = new RouteBuilder(app, smtRouter.GetRouteHandler());

            routeBuilder.MapRoute(
                name: "default",
                template: "{controller}/{action}");

            return app.UseRouter(routeBuilder.Build());
        }
    }
}
