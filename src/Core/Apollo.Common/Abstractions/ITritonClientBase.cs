using System;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Abstractions
{
    public interface ITritonClientBase : IDisposable
    {
	    T GetPlugin<T>() where T : TritonPluginBase;
	    void LoadPlugins(params TritonPluginBase[] tritonPluginsBase);
	    string Identifier { get; }

	    IServiceCommunicator Communicator { get; }
    }
}
