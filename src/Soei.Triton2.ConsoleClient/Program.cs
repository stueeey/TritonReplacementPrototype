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
using Soei.Triton2.Common.Infrastructure;
using Soei.Triton2.Common.Plugins;
using Soei.Triton2.ServiceBus;
using Soei.Triton2.ServiceBus.Ninject;

namespace Soei.Triton2.ConsoleClient
{
	public class Program
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
	        RunClient(client);
        }

	    private static void RunClient(ITritonClient client)
	    {
		    var echoPlugin = client.GetPlugin<EchoPlugin>();
		    Task.Run(async () => await client.RequestOwnershipOfAliasAsync("UK123", Guid.Empty));
		    Console.WriteLine("Enter echo text:");
		    while (true)
		    {
			    var command = Console.ReadLine();
			    if (CommandEquals(command, "exit")) 
				    continue;
			    var echoContent = Console.ReadLine();
			    Task.Run(async () => await echoPlugin.Echo(echoContent));
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
