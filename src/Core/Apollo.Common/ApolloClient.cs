using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.Common.Plugins;

namespace Apollo.Common
{
    public class ApolloClient : ApolloClientBase, IApolloClient
    {
		public ApolloClient(IServiceCommunicator communicator) : base(communicator)
		{
			LoadCorePlugin();
		}

		private void LoadCorePlugin()
	    {
		    if (GetPlugin<ClientCorePlugin>() == null) 
			    LoadPlugins(new ClientCorePlugin());
	    }

	    public ApolloClient(IServiceCommunicator communicator, params ApolloPlugin[] apolloPlugins) : base(communicator, apolloPlugins)
	    {
		    LoadCorePlugin();
	    }

	    public ApolloClient(IServiceCommunicator communicator, PluginCollection apolloPluginsBase) : base(communicator, apolloPluginsBase.ToArray())
	    {
		    LoadCorePlugin();
	    }

	    #region Implementation of IApolloClient

	    public Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    return GetPlugin<ClientCorePlugin>().RequestOwnershipOfAliasAsync(alias, token);
	    }

	    public Task<string> RegisterAsync(IDictionary<string, string> metadata = null, TimeSpan? timeout = null)
	    {
		    return GetPlugin<ClientCorePlugin>().RegisterAsync(metadata, timeout);
	    }

	    public Task<Guid> DemandOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    return GetPlugin<ClientCorePlugin>().DemandOwnershipOfAliasAsync(alias, token);
	    }

	    #endregion

	    
    }
}
