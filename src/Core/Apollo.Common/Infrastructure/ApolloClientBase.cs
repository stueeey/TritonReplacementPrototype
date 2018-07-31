using System;
using System.Collections.Generic;
using System.Linq;
using Apollo.Common.Abstractions;
using log4net;

namespace Apollo.Common.Infrastructure
{
    public class ApolloClientBase : IApolloClientBase
    {
	    public IServiceCommunicator Communicator { get; protected set; }

	    protected IDictionary<Type, ApolloPlugin> Plugins { get; set; } = new Dictionary<Type, ApolloPlugin>();

	    public ApolloPlugin[] GetPlugins() => Plugins.Values.ToArray();

	    public T GetPlugin<T>() where T : ApolloPlugin
	    {
		    lock (Plugins)
		    {
			    var typeOfT = typeof(T);
			    return Plugins.ContainsKey(typeOfT)
				    ? (T) Plugins[typeOfT]
				    : (T) Plugins.FirstOrDefault(p => p.Key.IsInstanceOfType(typeOfT)).Value;
		    }
	    }

	    private ILog _overrideLogger;
		// Used for diagnostics in unit tests
	    public ILog OverrideLogger
	    {
		    set
		    {
			    _overrideLogger = value;
			    foreach (var plugin in Plugins)
				    plugin.Value.SetLogger(value);
		    }
	    }

	    protected ApolloClientBase(IServiceCommunicator communicator)
	    {
		    Communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
	    }

	    protected ApolloClientBase(IServiceCommunicator communicator, params ApolloPlugin[] apolloPlugins)
	    {
		    Communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
		    LoadPlugins(apolloPlugins);
	    }

	    public void LoadPlugins(params ApolloPlugin[] apolloPlugins)
	    {
		    lock (Plugins)
		    {
			    foreach (var plugin in apolloPlugins)
			    {
				    Plugins.Add(plugin.GetType(), plugin);
				    plugin.SetCommunicator(Communicator);
				    if (_overrideLogger != null)
					    plugin.SetLogger(_overrideLogger);
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
