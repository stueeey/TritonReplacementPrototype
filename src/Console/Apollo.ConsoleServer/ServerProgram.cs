using System;
using System.IO;
using System.Reflection;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.ServiceBus;
using Apollo.ServiceBus.Ninject;
using log4net;
using log4net.Config;
using Microsoft.Azure.ServiceBus;
using Ninject;

namespace Apollo.ConsoleServer
{
	class ServerProgram
    {
        static void Main(string[] args)
        {
	        Console.ForegroundColor = ConsoleColor.Green;
	        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
	        XmlConfigurator.Configure(logRepository, new FileInfo("Logging.config"));
	        var container = SetupIoc();
	        container.Bind<IRegistrationStorage>().To<InMemoryRegistrationStorage>().InSingletonScope();
	        var server = container.Get<ITritonServer>();
	        Console.Title = $"Server Console [{server.Identifier}]";
	        Console.ReadKey();
        }

	    private static StandardKernel SetupIoc()
	    {
		    var configuration = new ServiceBusConfiguration
		    (
			    new ServiceBusConnectionStringBuilder(Environment.GetEnvironmentVariable(TritonConstants.ConnectionKey) ?? throw new ArgumentException($"Environment variable '{TritonConstants.ConnectionKey}' is not configured")),
			    $"Server {Guid.NewGuid()}"
		    );
		    var implementations = new TritonServiceBusImplementations(configuration)
		    {
			    ServerPlugins = new TritonPluginBase[]
			    {
				    new EchoListenerPlugin()
			    }
		    };
		    var container = new StandardKernel(implementations);
		    return container;
	    }
    }
}
