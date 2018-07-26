using System;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Abstractions
{
    public interface IApolloClientBase : IDisposable
    {
	    T GetPlugin<T>() where T : ApolloPluginBase;
	    ApolloPluginBase[] GetPlugins();
	    void LoadPlugins(params ApolloPluginBase[] apolloPluginsBase);
	    string Identifier { get; }

	    IServiceCommunicator Communicator { get; }
    }
}
