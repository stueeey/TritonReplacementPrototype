using Ninject.Modules;
using System;
using Soei.Apollo.Common;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;
using Soei.Apollo.ServiceBus.Communication;

namespace Soei.Apollo.ServiceBus.Ninject
{
	public class TritonServiceBusImplementations : NinjectModule
    {
	    private readonly ServiceBusConfiguration _configuration;
	    public TritonPluginBase[] ServerPlugins { get; set; } = {};
	    public TritonPluginBase[] ClientPlugins { get; set;} = {};

	    public TritonServiceBusImplementations(ServiceBusConfiguration configuration)
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
			    .WhenInjectedInto<ITritonClient>();
		    Bind<PluginCollection>()
			    .ToConstant(new PluginCollection(ServerPlugins))
			    .WhenInjectedInto<ITritonServer>();
		    Bind<ITritonClient>().To<TritonClient>().InSingletonScope();
		    Bind<ITritonServer>().To<TritonServer>().InSingletonScope();
	    }

	    #endregion
    }
}
