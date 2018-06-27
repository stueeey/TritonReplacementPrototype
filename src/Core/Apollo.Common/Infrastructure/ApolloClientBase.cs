using System;
using System.Collections.Generic;
using System.Linq;
using Apollo.Common.Abstractions;

namespace Apollo.Common.Infrastructure
{
    public class ApolloClientBase : IApolloClientBase
    {
	    public IServiceCommunicator Communicator { get; protected set; }

	    protected IDictionary<Type, ApolloPluginBase> Plugins { get; set; } = new Dictionary<Type, ApolloPluginBase>();

	    public T GetPlugin<T>() where T : ApolloPluginBase
	    {
		    lock (Plugins)
		    {
			    var typeOfT = typeof(T);
			    return Plugins.ContainsKey(typeOfT)
				    ? (T) Plugins[typeOfT]
				    : (T) Plugins.FirstOrDefault(p => p.Key.IsInstanceOfType(typeOfT)).Value;
		    }
	    }

	    protected ApolloClientBase(IServiceCommunicator communicator)
	    {
		    Communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
	    }

	    protected ApolloClientBase(IServiceCommunicator communicator, params ApolloPluginBase[] ApolloPluginsBase)
	    {
		    Communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
		    LoadPlugins(ApolloPluginsBase);
	    }

	    public void LoadPlugins(params ApolloPluginBase[] ApolloPluginsBase)
	    {
		    lock (Plugins)
		    {
			    foreach (var plugin in ApolloPluginsBase)
			    {
				    Plugins.Add(plugin.GetType(), plugin);
				    plugin.SetCommunicator(Communicator);
			    }
		    }
	    }

	    public string Identifier => Communicator.GetState<string>(ApolloConstants.RegisteredAsKey);

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
