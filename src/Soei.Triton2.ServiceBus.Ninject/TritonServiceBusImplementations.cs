using Ninject.Modules;
using Ninject.Extensions.Conventions;
using System;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Infrastructure;
using Soei.Triton2.ServiceBus.Communication;

namespace Soei.Triton2.ServiceBus.Ninject
{
    public class TritonServiceBusImplementations : NinjectModule
    {
	    private readonly ServiceBusConfiguration _configuration;
	    public TritonPluginBase[] ServerPlugins { get; set;}
	    public TritonPluginBase[] ClientPlugins { get; set;}

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
		    Bind<TritonPluginBase[]>().ToConstant(ClientPlugins).WhenInjectedInto<ITritonClient>();
		    Bind<TritonPluginBase[]>().ToConstant(ServerPlugins).WhenInjectedInto<ITritonServer>();;
		    Bind<ITritonClient>().To<TritonClient>().InSingletonScope();
		    Bind<ITritonServer>().To<TritonServer>().InSingletonScope();
	    }

	    #endregion
    }
}
