using System;
using System.IO;
using System.Reflection;
using Apollo.Common;
using Apollo.Common.Abstractions;
using Apollo.Common.Plugins;
using Apollo.Mocks;
using Apollo.ServerWorker.Plugins;
using Apollo.ServiceBus;
using Apollo.ServiceBus.Ninject;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ninject;

namespace Apollo.Server.Kubernetes
{
	public class Startup
	{
		public IApolloServer ServerWorker { get; private set; }

		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			XmlConfigurator.Configure(logRepository, new FileInfo("Logging.config"));
		}

		private static StandardKernel SetupIoc()
		{
			var connectionKey = Environment.GetEnvironmentVariable(ApolloConstants.ConnectionKey, EnvironmentVariableTarget.Process) ??
								Environment.GetEnvironmentVariable(ApolloConstants.ConnectionKey, EnvironmentVariableTarget.User) ??
								Environment.GetEnvironmentVariable(ApolloConstants.ConnectionKey, EnvironmentVariableTarget.Machine) ??
								throw new ArgumentException($"Environment variable '{ApolloConstants.ConnectionKey}' is not configured");

			var serverIdentifier = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" 
				? $"{Environment.MachineName} {Environment.GetEnvironmentVariable("APOLLO_SERVER_ID")}" // Machine name will be the ID of the container if running in docker
				: (Environment.GetEnvironmentVariable("APOLLO_SERVER_ID") ?? $"Server {Guid.NewGuid()}");

			var configuration = new ServiceBusConfiguration
			(
				new ServiceBusConnectionStringBuilder(connectionKey), 
				serverIdentifier
			);
			var implementations = new ApolloServiceBusImplementations(configuration)
			{
				ServerPlugins = new ApolloPlugin[]
				{
					new EchoListenerPlugin(),
					new MessageCounterPlugin()
				}
			};
			var container = new StandardKernel(implementations);
			container.Bind<IRegistrationStorage>().To<InMemoryRegistrationStorage>().InSingletonScope();
			return container;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddMvc()
				.AddJsonOptions(options =>
				{
					options.SerializerSettings.Formatting = Formatting.Indented;
					options.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
				});
			var container = SetupIoc();
			services.AddSingleton<IKernel>(container);
			ServerWorker = container.Get<IApolloServer>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			app.UseMvc();
		}
	}
}
