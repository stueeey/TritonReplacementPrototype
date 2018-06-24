using System;
using System.IO;
using System.Reflection;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.Common.Plugins;
using Apollo.ServerWorker.Plugins;
using Apollo.ServerWorker.Services;
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
using Ninject.Infrastructure.Disposal;

namespace Apollo.ServerWorker
{
    public class Startup
    {
	    public ITritonServer ServerWorker { get; private set; }

	    public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
	        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
	        XmlConfigurator.Configure(logRepository, new FileInfo("Logging.config"));
	        

        }

	    private static StandardKernel SetupIoc()
	    {
			var connectionKey = Environment.GetEnvironmentVariable(TritonConstants.ConnectionKey, EnvironmentVariableTarget.Process) ?? 
			                    Environment.GetEnvironmentVariable(TritonConstants.ConnectionKey, EnvironmentVariableTarget.User) ??
			                    Environment.GetEnvironmentVariable(TritonConstants.ConnectionKey, EnvironmentVariableTarget.Machine);
		    var configuration = new ServiceBusConfiguration
		    (
			    new ServiceBusConnectionStringBuilder(connectionKey ?? throw new ArgumentException($"Environment variable '{TritonConstants.ConnectionKey}' is not configured")),
			    $"Server {Guid.NewGuid()}"
		    );
		    var implementations = new TritonServiceBusImplementations(configuration)
		    {
			    ServerPlugins = new TritonPluginBase[]
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
	        ServerWorker = container.Get<ITritonServer>();
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
