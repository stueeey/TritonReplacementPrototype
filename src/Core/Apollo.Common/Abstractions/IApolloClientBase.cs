using System;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Abstractions
{
    public interface IApolloClientBase : IDisposable
    {
	    T GetPlugin<T>() where T : ApolloPlugin;
	    ApolloPlugin[] GetPlugins();
	    void LoadPlugins(params ApolloPlugin[] apolloPlugins);
	    string Identifier { get; }

	    IServiceCommunicator Communicator { get; }
    }
}
