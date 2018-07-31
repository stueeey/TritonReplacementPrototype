using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.Common.Plugins;

namespace Apollo.Common
{
    public class ApolloServer : ApolloClientBase, IApolloServer
    {
	    public ApolloServer(IServiceCommunicator communicator, IRegistrationStorage storage) : base(communicator)
	    {
		    LoadCorePlugin(storage);
	    }

	    private void LoadCorePlugin(IRegistrationStorage storage)
	    {
		    if (GetPlugin<ServerCorePlugin>() == null) 
			    LoadPlugins(new ServerCorePlugin(storage));
	    }

	    public ApolloServer(IServiceCommunicator communicator, IRegistrationStorage storage, params ApolloPlugin[] apolloPlugins) : base(communicator, apolloPlugins)
	    {
		    LoadCorePlugin(storage);
	    }

	    public ApolloServer(IServiceCommunicator communicator, IRegistrationStorage storage, PluginCollection ApolloPluginsBase) : base(communicator, ApolloPluginsBase.ToArray())
	    {
		    LoadCorePlugin(storage);
	    }
    }
}
