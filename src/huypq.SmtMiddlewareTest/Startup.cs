using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using huypq.SmtMiddleware;

namespace huypq.SmtMiddlewareTest
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            //services.AddTransient(typeof(IDbContext<ITenant, IUser>), typeof(TestContext));
            services.AddSmtWithTrustedConnection<TestContext, SmtTenant, SmtUser, SmtUserClaim>("Test", @"c:\huypq.SmtMiddleware.key");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            app.UseCors(builder => builder.WithOrigins("http://localhost", "http://luoithepvinhphat.com").AllowAnyHeader().AllowAnyMethod());
            //SmtSettings.Instance.AllowAnonymousActions.Add("user.get");
            app.UseSmt<TestContext, SmtTenant, SmtUser, SmtUserClaim>("huypq.SmtMiddlewareTest");
        }
    }
}
