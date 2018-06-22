using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;
using Soei.Apollo.Common.Plugins;

namespace Soei.Apollo.Common
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

	    public Task<string> RegisterAsync(IDictionary<string, string> metadata = null, TimeSpan? timeout = null)
	    {
		    return GetPlugin<ClientCorePlugin>().RegisterAsync(metadata, timeout);
	    }

	    #endregion
    }
}
