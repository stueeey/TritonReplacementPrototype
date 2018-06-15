using System;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Abstractions
{
    public interface ITritonClientBase : IDisposable
    {
	    T GetPlugin<T>() where T : TritonPluginBase;
	    void LoadPlugins(params TritonPluginBase[] tritonPluginsBase);
	    string Identifier { get; }
    }
}
