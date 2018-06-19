using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Microsoft.Azure.ServiceBus;
using Ninject;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;
using Soei.Triton2.Common.Plugins;
using Soei.Triton2.ServiceBus;
using Soei.Triton2.ServiceBus.Ninject;

namespace Soei.Triton2.ConsoleClient
{
	public class ClientProgram
    {
	    private static bool CommandEquals(string input, string command)
	    {
		    return StringComparer.InvariantCultureIgnoreCase.Equals(command.Trim(), input.Trim());
	    }

        static void Main(string[] args)
        {
	        Console.ForegroundColor = ConsoleColor.Cyan;
	        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
	        XmlConfigurator.Configure(logRepository, new FileInfo("Logging.config"));
	        var container = SetupIoc();
	        var client = container.Get<ITritonClient>();
	        Console.Title = $"Client Console [{client.Identifier}]";
	        RunClient(client).Wait();
        }

	    private static async Task RunClient(ITritonClient client)
	    {
		    var echoPlugin = client.GetPlugin<EchoPlugin>();
		    await client.RegisterAsync(new Dictionary<string, string>
		    {
			    { "Mood", "Aggressive"}
		    });
		    await client.RequestOwnershipOfAliasAsync("UK123", Guid.NewGuid());
		    
		    while (true)
		    {
				Console.WriteLine("Enter command:");
				var command = Console.ReadLine();
			    if (CommandEquals(command, "exit")) 
				    return;
				if (CommandEquals(command, "ping self"))
				{
					var result = await client.GetPlugin<ClientCorePlugin>().PingClient(client.Identifier);
					Console.WriteLine(result?.ToString() ?? "Ping timed out");
					continue;
				}
				if (CommandEquals(command, "ping server"))
				{
					var result = await client.GetPlugin<ClientCorePlugin>().PingServer();
					Console.WriteLine(result?.ToString() ?? "Ping timed out");
					continue;
				}
				if (command.StartsWith("ping alias"))
				{
					Console.WriteLine("Enter alias");
					command = Console.ReadLine();
					var result = await client.GetPlugin<ClientCorePlugin>().PingAlias(command);
					Console.WriteLine(result?.ToString() ?? "Ping timed out");
					continue;
				}
				await echoPlugin.Echo(command);
		    }
	    }

	    private static StandardKernel SetupIoc()
	    {
		    var credentials = Environment.GetEnvironmentVariable(TritonConstants.ConnectionKey) ??
		                      throw new ArgumentException(
			                      $"Environment variable '{TritonConstants.ConnectionKey}' is not configured");
		    var configuration = new ServiceBusConfiguration
		    (
			    new ServiceBusConnection(credentials),
			    Guid.NewGuid().ToString()
		    );
		    var implementations = new TritonServiceBusImplementations(configuration)
		    {
			    ClientPlugins = new TritonPluginBase[]
			    {
				    new EchoPlugin()
			    }
		    };
		    var container = new StandardKernel(implementations);
		    return container;
	    }
    }
}
