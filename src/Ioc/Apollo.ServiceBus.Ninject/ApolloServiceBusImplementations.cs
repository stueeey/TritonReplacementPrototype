using System;
using Apollo.Common;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.ServiceBus.Communication;
using Ninject.Modules;

namespace Apollo.ServiceBus.Ninject
{
	public class ApolloServiceBusImplementations : NinjectModule
    {
	    private readonly ServiceBusConfiguration _configuration;
	    public ApolloPlugin[] ServerPlugins { get; set; } = {};
	    public ApolloPlugin[] ClientPlugins { get; set;} = {};

	    public ApolloServiceBusImplementations(ServiceBusConfiguration configuration)
	    {
		    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	    }

	    #region Overrides of NinjectModule

	    public override void Load()
	    {
		    Bind<ServiceBusConfiguration>().ToConstant(_configuration);
		    Bind<IServiceBusImplementations>().To<DefaultServiceBusImplementations>();
		    Bind<IServiceCommunicator>().To<ServiceBusCommunicator>();
		    Bind<PluginCollection>()
			    .ToConstant(new PluginCollection(ClientPlugins))
			    .WhenInjectedInto<IApolloClient>();
		    Bind<PluginCollection>()
			    .ToConstant(new PluginCollection(ServerPlugins))
			    .WhenInjectedInto<IApolloServer>();
		    Bind<IApolloClient>().To<ApolloClient>().InSingletonScope();
		    Bind<IApolloServer>().To<ApolloServer>().InSingletonScope();
	    }

	    #endregion
    }
}
