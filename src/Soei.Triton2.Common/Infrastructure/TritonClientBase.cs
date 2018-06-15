using System;
using System.Collections.Generic;
using System.Linq;
using Soei.Triton2.Common.Abstractions;

namespace Soei.Triton2.Common.Infrastructure
{
    public class TritonClientBase : ITritonClientBase
    {
	    protected IServiceCommunicator Communicator { get; set; }

	    protected IDictionary<Type, TritonPluginBase> Plugins { get; set; } = new Dictionary<Type, TritonPluginBase>();

	    public T GetPlugin<T>() where T : TritonPluginBase
	    {
		    lock (Plugins)
		    {
			    var typeOfT = typeof(T);
			    return Plugins.ContainsKey(typeOfT)
				    ? (T) Plugins[typeOfT]
				    : (T) Plugins.FirstOrDefault(p => p.Key.IsInstanceOfType(typeOfT)).Value;
		    }
	    }

	    protected TritonClientBase(IServiceCommunicator communicator)
	    {
		    Communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
	    }

	    protected TritonClientBase(IServiceCommunicator communicator, params TritonPluginBase[] tritonPluginsBase)
	    {
		    Communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
		    LoadPlugins(tritonPluginsBase);
	    }

	    public void LoadPlugins(params TritonPluginBase[] tritonPluginsBase)
	    {
		    lock (Plugins)
		    {
			    foreach (var plugin in tritonPluginsBase)
			    {
				    Plugins.Add(plugin.GetType(), plugin);
				    plugin.SetCommunicator(Communicator);
			    }
		    }
	    }

	    public string Identifier => Communicator.GetState<string>(nameof(Identifier));

	    #region IDisposable

	    public void Dispose()
	    {
		    foreach (var plugin in Plugins)
			    plugin.Value.OnUninitialized();
		    Communicator?.Dispose();
	    }

	    #endregion
    }
}
