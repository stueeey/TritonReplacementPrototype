using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.Common.Plugins;

namespace Apollo.Common
{
    public class ApolloServer : ApolloClientBase, IApolloServer
    {
	    public ApolloServer(IServiceCommunicator communicator, IApolloServerRepository storage) : base(communicator)
	    {
		    LoadCorePlugin(storage);
	    }

	    private void LoadCorePlugin(IApolloServerRepository storage)
	    {
		    if (GetPlugin<ServerCorePlugin>() == null) 
			    LoadPlugins(new ServerCorePlugin(storage));
	    }

	    public ApolloServer(IServiceCommunicator communicator, IApolloServerRepository storage, params ApolloPlugin[] apolloPlugins) : base(communicator, apolloPlugins)
	    {
		    LoadCorePlugin(storage);
	    }

	    public ApolloServer(IServiceCommunicator communicator, IApolloServerRepository storage, PluginCollection ApolloPluginsBase) : base(communicator, ApolloPluginsBase.ToArray())
	    {
		    LoadCorePlugin(storage);
	    }
    }
}
