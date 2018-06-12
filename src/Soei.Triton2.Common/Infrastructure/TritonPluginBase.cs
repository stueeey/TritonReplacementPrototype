using System;
using System.Reflection;
using System.Threading.Tasks;
using log4net;

namespace Soei.Triton2.Common.Infrastructure
{
	public abstract class TritonPluginBase
	{
		protected static ILog ClassLogger = LogManager.GetLogger(Assembly.GetEntryAssembly(), $"{TritonConstants.LoggerPluginsPrefix}.{MethodBase.GetCurrentMethod().DeclaringType.Name}");
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

		}
	}
}