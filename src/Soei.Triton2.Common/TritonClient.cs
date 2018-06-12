using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Soei.Triton2.Common.Infrastructure;
using Soei.Triton2.Common.Plugins;

namespace Soei.Triton2.Common
{
    public class TritonClient : TritonClientBase, ITritonClient
    {
		public TritonClient(IServiceCommunicator communicator) : base(communicator)
		{
			LoadCorePlugin();
		}

		private void LoadCorePlugin()
	    {
		    if (GetPlugin<ClientCorePlugin>() == null) 
			    LoadPlugins(new ClientCorePlugin());
	    }

	    public TritonClient(IServiceCommunicator communicator, params TritonPluginBase[] tritonPluginsBase) : base(communicator, tritonPluginsBase)
	    {
		    LoadCorePlugin();
	    }

	    public TritonClient(IServiceCommunicator communicator, PluginCollection tritonPluginsBase) : base(communicator, tritonPluginsBase.ToArray())
	    {
		    LoadCorePlugin();
	    }

	    #region Implementation of ITritonClient

	    public Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    return GetPlugin<ClientCorePlugin>().RequestOwnershipOfAliasAsync(alias, token);
	    }

	    #endregion
    }
}
