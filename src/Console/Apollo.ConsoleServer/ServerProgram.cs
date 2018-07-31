using System;
using System.IO;
using System.Reflection;
using Apollo.Common;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.Mocks;
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
	        var server = container.Get<IApolloServer>();
	        Console.Title = $"Server Console [{server.Identifier}]";
	        Console.ReadKey();
        }

	    private static StandardKernel SetupIoc()
	    {
		    var connectionKey = Environment.GetEnvironmentVariable(ApolloConstants.ConnectionKey, EnvironmentVariableTarget.Process) ?? 
		                        Environment.GetEnvironmentVariable(ApolloConstants.ConnectionKey, EnvironmentVariableTarget.User) ??
		                        Environment.GetEnvironmentVariable(ApolloConstants.ConnectionKey, EnvironmentVariableTarget.Machine) ??
		                        throw new ArgumentException($"Environment variable '{ApolloConstants.ConnectionKey}' is not configured");
		    var configuration = new ServiceBusConfiguration
		    (
			    new ServiceBusConnectionStringBuilder(connectionKey),
			    $"Server {Guid.NewGuid()}"
		    );
		    var implementations = new ApolloServiceBusImplementations(configuration)
		    {
			    ServerPlugins = new ApolloPlugin[]
			    {
				    new EchoListenerPlugin()
			    }
		    };
		    var container = new StandardKernel(implementations);
		    return container;
	    }
    }
}
