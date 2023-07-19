using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CloudNative.CloudEvents.NewtonsoftJson;
using System;
using CloudNative.CloudEvents;

namespace Contoso.Expenses.Function
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.Run(async (context) =>
            {
                Console.WriteLine("context.Request.Path: {0}", context.Request.Path);

                if (context.Request.Path != "/")
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("404 - Not Found");
                    return;
                }

                Console.WriteLine("context.Request.Method: {0}", context.Request.Method);
                if (context.Request.Method != "POST")
                {
                    context.Response.StatusCode = 405;
                    await context.Response.WriteAsync("405 - Only POST method allowed");
                    return;
                }

                try
                {
                    var (status, text) = await new EmailHandler().Handle(context.Request);
                    context.Response.StatusCode = status;
                    if (!string.IsNullOrEmpty(text))
                        await context.Response.WriteAsync(text);
                }
                catch (NotImplementedException nie)
                {
                    Console.WriteLine(nie.ToString());
                    context.Response.StatusCode = 501;
                    await context.Response.WriteAsync(nie.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync(ex.ToString());
                }
            });
        }
    }
}
