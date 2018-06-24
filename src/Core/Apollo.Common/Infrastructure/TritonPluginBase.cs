using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using log4net;

namespace Apollo.Common.Infrastructure
{
	public class PluginCollection : List<TritonPluginBase>
	{
		public PluginCollection()
		{
		}

		public PluginCollection(IEnumerable<TritonPluginBase> collection) : base(collection)
		{
		}
	}

	public abstract class TritonPluginBase
	{
		protected static ILog ClassLogger = LogManager.GetLogger(Assembly.GetEntryAssembly(), $"{TritonConstants.LoggerPluginsPrefix}.{MethodBase.GetCurrentMethod()?.DeclaringType?.Name ?? "<Unknown>"}");
		protected ILog Logger { get; private set;}
		protected IServiceCommunicator Communicator { get; private set; }
		protected IMessageFactory MessageFactory => Communicator?.MessageFactory;

		public void SetLogger(ILog log = null)
		{
			Logger = log ?? ClassLogger;
		}

		protected TritonPluginBase()
		{
			SetLogger();
		}

		protected void SubscribeButDoNothing(IMessage message, ref MessageReceivedEventArgs e)
		{

		}

		internal void SetCommunicator(IServiceCommunicator communicator)
		{
			Communicator = communicator ?? throw new ArgumentNullException(nameof(communicator));
			OnInitialized();
		}

		protected virtual Task OnInitialized()
		{
			return Task.Run(() =>
			{
				Logger.Debug($"Initializing {GetType().Name}");
			});
		}

		public virtual void OnUninitialized()
		{
			Communicator?.RemoveListenersForPlugin(this);
		}
	}
}