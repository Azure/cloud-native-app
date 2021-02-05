using Jaeger;
using Jaeger.Reporters;
using Jaeger.Samplers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTracing;
using OpenTracing.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Contoso.Expenses.Web.Tracing
{
	public static class JaegerTracingServiceCollectionExtensions
	{
		public static IServiceCollection AddJaegerTracing(
			this IServiceCollection services,
			Action<JaegerTracingOptions> setupAction = null)
		{
			if (setupAction != null) services.ConfigureJaegerTracing(setupAction);

			services.AddSingleton<ITracer>(cli =>
			{
				var options = cli.GetService<IOptions<JaegerTracingOptions>>().Value;

				var senderConfig = new Jaeger.Configuration.SenderConfiguration(options.LoggerFactory)
					.WithAgentHost(options.JaegerAgentHost)
					.WithAgentPort(options.JaegerAgentPort);

				var reporter = new RemoteReporter.Builder()
					.WithLoggerFactory(options.LoggerFactory)
					.WithSender(senderConfig.GetSender())
					.Build();

				var sampler = new GuaranteedThroughputSampler(options.SamplingRate, options.LowerBound);

				var tracer = new Tracer.Builder(options.ServiceName)
					.WithLoggerFactory(options.LoggerFactory)
					.WithReporter(reporter)
					.WithSampler(sampler)
					.Build();

				// Allows code that can't use dependency injection to have access to the tracer.
				if (!GlobalTracer.IsRegistered())
					GlobalTracer.Register(tracer);

				return tracer;
			});

			services.AddOpenTracing(builder => {
				builder.ConfigureAspNetCore(options => {
					options.Hosting.IgnorePatterns.Add(x => {
						return x.Request.Path == "/health";
					});
					options.Hosting.IgnorePatterns.Add(x => {
						return x.Request.Path == "/metrics";
					});
				});
			});

			return services;
		}

		public static void ConfigureJaegerTracing(
			this IServiceCollection services,
			Action<JaegerTracingOptions> setupAction)
		{
			services.Configure<JaegerTracingOptions>(setupAction);
		}
	}
}
