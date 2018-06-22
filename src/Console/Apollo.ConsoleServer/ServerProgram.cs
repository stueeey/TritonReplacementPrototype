﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.Azure.ServiceBus;
using Ninject;
using Soei.Apollo.Common;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;
using Soei.Apollo.ServiceBus;
using Soei.Apollo.ServiceBus.Ninject;

namespace Soei.Apollo.ConsoleServer
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
			    new ServiceBusConnection(Environment.GetEnvironmentVariable(TritonConstants.ConnectionKey) ?? throw new ArgumentException($"Environment variable '{TritonConstants.ConnectionKey}' is not configured")),
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