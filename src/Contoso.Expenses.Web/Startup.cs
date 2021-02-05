using Contoso.Expenses.Common.Models;
using Contoso.Expenses.Web.Models;
using Contoso.Expenses.Web.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Contoso.Expenses.Web
{
    public class Startup
    {
        private readonly IHostingEnvironment _env;
        public IConfiguration Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(env.ContentRootPath)
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                            .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }

            _env = env;

            Configuration = builder.Build();
            //Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });
            services.AddMetrics();

            services.AddJaegerTracing(options => {
                //options.JaegerAgentHost = Configuration["JAEGER_AGENT_HOST"];
                //options.JaegerAgentHost = Configuration["JAEGER_AGENT_HOST"];
                options.JaegerAgentHost = "jaeger-agent.tracing";
                options.ServiceName = "conexp";
                options.LoggerFactory = (ILoggerFactory)new LoggerFactory();
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            // see https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-2.2
            string connectionString = Configuration["ConnectionStrings:DBConnectionString"];
            services.AddDbContext<ContosoExpensesWebContext>(options =>
                    options.UseMySql(connectionString));

            services.Configure<ConfigValues>(Configuration.GetSection("ConfigValues"));

            services.AddSingleton<QueueInfo>(queueInfo =>
            {
                return new QueueInfo()
                {
                    ConnectionString = Configuration["ConnectionStrings:NATSConnectionString"],
                    QueueName = Configuration["TopicName"]
                };
            });

            services.AddSingleton<IHostingEnvironment>(_env);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ContosoExpensesWebContext context, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            context.Database.EnsureCreated();

            //app.UseDefaultFiles();
            //app.UseStaticFiles();
            //app.UseCookiePolicy();
            //app.UseMvc();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
