using System;
using Soei.Apollo.Common.Infrastructure;

namespace Soei.Apollo.Common.Abstractions
{
    public interface ITritonClientBase : IDisposable
    {
	    T GetPlugin<T>() where T : TritonPluginBase;
	    void LoadPlugins(params TritonPluginBase[] tritonPluginsBase);
	    string Identifier { get; }

	    IServiceCommunicator Communicator { get; }
    }
}
