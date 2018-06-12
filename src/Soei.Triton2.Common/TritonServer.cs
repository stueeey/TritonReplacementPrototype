using System;
using System.Threading.Tasks;
using Soei.Triton2.Common.Infrastructure;
using Soei.Triton2.Common.Plugins;

namespace Soei.Triton2.Common
{
    public class TritonServer : TritonClientBase, ITritonServer
    {
	    public TritonServer(IServiceCommunicator communicator) : base(communicator)
	    {
		    LoadCorePlugin();
	    }

	    private void LoadCorePlugin()
	    {
		    if (GetPlugin<ServerCorePlugin>() == null) 
			    LoadPlugins(new ServerCorePlugin());
	    }

	    public TritonServer(IServiceCommunicator communicator, params TritonPluginBase[] tritonPluginsBase) : base(communicator, tritonPluginsBase)
	    {
		    LoadCorePlugin();
	    }

	    public TritonServer(IServiceCommunicator communicator, PluginCollection tritonPluginsBase) : base(communicator, tritonPluginsBase.ToArray())
	    {
		    LoadCorePlugin();
	    }
    }
}
