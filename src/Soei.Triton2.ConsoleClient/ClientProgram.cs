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
					for (var i = 0; i < 5; i++)
					{
						var result = await client.GetPlugin<ClientCorePlugin>().PingClient(client.Identifier, TimeSpan.FromSeconds(3));
						Console.WriteLine(result == null ? "Ping timed out" : result.ToString());
					}
					continue;
				}
				if (CommandEquals(command, "ping server"))
				{
					for (var i = 0; i < 5; i++)
					{
						var result = await client.GetPlugin<ClientCorePlugin>().PingServer(TimeSpan.FromSeconds(3));
						Console.WriteLine(result == null ? "Ping timed out" : result.ToString());
					}

					continue;
				}
				if (command?.StartsWith("ping alias") ?? false)
				{
					Console.WriteLine("Enter alias");
					command = Console.ReadLine();
					for (var i = 0; i < 5; i++)
					{
						var result = await client.GetPlugin<ClientCorePlugin>().PingAlias(command, TimeSpan.FromSeconds(3));
						Console.WriteLine(result == null ? "Ping timed out" : result.ToString());
					}

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
