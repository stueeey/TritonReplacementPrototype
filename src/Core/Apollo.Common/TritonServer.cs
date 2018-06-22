using System;
using System.Collections.Generic;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;
using Soei.Apollo.Common.Plugins;

namespace Soei.Apollo.Common
{
    public class TritonServer : TritonClientBase, ITritonServer
    {
	    public TritonServer(IServiceCommunicator communicator, IRegistrationStorage storage) : base(communicator)
	    {
		    LoadCorePlugin(storage);
	    }

	    private void LoadCorePlugin(IRegistrationStorage storage)
	    {
		    if (GetPlugin<ServerCorePlugin>() == null) 
			    LoadPlugins(new ServerCorePlugin(storage));
	    }

	    public TritonServer(IServiceCommunicator communicator, IRegistrationStorage storage, params TritonPluginBase[] tritonPluginsBase) : base(communicator, tritonPluginsBase)
	    {
		    LoadCorePlugin(storage);
	    }

	    public TritonServer(IServiceCommunicator communicator, IRegistrationStorage storage, PluginCollection tritonPluginsBase) : base(communicator, tritonPluginsBase.ToArray())
	    {
		    LoadCorePlugin(storage);
	    }
    }
}
