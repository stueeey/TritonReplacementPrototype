using System;
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
	        //client.LoadPlugins(new EchoPlugin());
	        Console.Title = $"Client Console [{client.Identifier}]";
	        RunClient(client).Wait();
        }

	    private static async Task RunClient(ITritonClient client)
	    {
		    var echoPlugin = client.GetPlugin<EchoPlugin>();
		    await client.RequestOwnershipOfAliasAsync("UK123", Guid.Empty);
		    Console.WriteLine("Enter echo text:");
		    while (true)
		    {
			    var command = Console.ReadLine();
			    if (CommandEquals(command, "exit")) 
				    return;
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
			    $"Client{Process.GetCurrentProcess().Id}|{TritonHelpers.GetMachineIdentifier()}"
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
