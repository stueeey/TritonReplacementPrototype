using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Infrastructure;
using Soei.Triton2.ServiceBus.Infrastructure;

namespace Soei.Triton2.ServiceBus.Communication
{
	public partial class ServiceBusCommunicator : IServiceCommunicator
	{
		protected class MessageWaitJob
		{
			public MessageWaitJob(DateTime expiryTimeUtc)
			{
				ExpiryTimeUtc = expiryTimeUtc;
			}

			public ManualResetEventSlim WaitHandle { get; } = new ManualResetEventSlim();
			public IMessage Message { get; set; }
			public DateTime StartTimeUtc { get; } = DateTime.UtcNow;
			public DateTime ExpiryTimeUtc { get; set; }
		}

		private static readonly ILog ClassLogger = LogManager.GetLogger(Assembly.GetEntryAssembly(), $"{TritonConstants.LoggerInternalsPrefix}.{MethodBase.GetCurrentMethod().DeclaringType.Name}");
		protected ILog Logger { get; private set; }

		public ConcurrentDictionary<string, object> State { get; } = new ConcurrentDictionary<string, object>();

		private IServiceBusImplementations Impl { get; }
		protected Lazy<IMessageReceiver> RegistrationListener => Impl.RegistrationListener;
		protected Lazy<IMessageSender> RegistrationSender => Impl.RegistrationSender;
		protected Lazy<IMessageReceiver> ServerQueueListener => Impl.ServerQueueListener;
		protected Lazy<IMessageSender> ServerQueueSender => Impl.ServerQueueSender;
		protected Lazy<ISessionClient> ClientSessionListener => Impl.ClientSessionListener;
		protected Lazy<IMessageSender> ClientSessionSender => Impl.ClientSessionSender;

		protected ConcurrentDictionary<string, MessageWaitJob> ReplyWaitList { get; } = new ConcurrentDictionary<string, MessageWaitJob>();

		public T GetState<T>(string key)
		{
			return State.TryGetValue(key, out var value)
				? (T)value
				: default(T);
		}

		public void SignalPluginEvent(string eventName, object state)
		{
			PluginEvent?.Invoke(eventName, state);
		}

		private async Task InvokeMessageHandlers(IReceiverClient receiver, Delegate handlers, ServiceBusMessage message, CancellationToken token, OnMessageReceivedDelegate baseHandler)
		{
			if (message == null)
				return;
			var e = new MessageReceivedEventArgs(this, token);
			baseHandler.Invoke(message, ref e);
			foreach (var handler in handlers?.GetInvocationList() ?? new Delegate[] { })
			{
				if (e.Status.HasFlag(MessageStatus.Handled))
					break;
				try
				{
					((OnMessageReceivedDelegate) handler).Invoke(message, ref e);
				}
				catch (Exception ex)
				{
					Logger.Error($"Encountered an error in {handler.Method.DeclaringType?.Name ?? "<Unknown>"}.{handler.Method.Name} while handling a message labelled {message.Label}", ex);
				}
			}
			if (e.Status.HasFlag(MessageStatus.MarkedForDeletion))
				await receiver.CompleteAsync(message.InnerMessage.SystemProperties.LockToken);
			else
				await receiver.AbandonAsync(message.InnerMessage.SystemProperties.LockToken);
		}

		public IMessageFactory MessageFactory { get; }

		public void SetLogger(ILog log = null) => Logger = log ?? ClassLogger;

		public ServiceBusCommunicator(ServiceBusConfiguration configuration, IServiceBusImplementations serviceBusImplementations = null)
		{
			SetLogger();

			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));
			State["Identifier"] = configuration.ClientIdentifier;
			MessageFactory = new ServiceBusMessageFactory(ServiceBusConstants.RegisteredClientsQueue, configuration.ClientIdentifier);
			Impl = serviceBusImplementations ?? new DefaultServiceBusImplementations(configuration);
		}

		private void CheckIfAnyoneIsWaitingForMessage(IMessage m, MessageReceivedEventArgs e)
		{
			if (string.IsNullOrEmpty(m.ResponseTo) || !ReplyWaitList.TryGetValue(m.ResponseTo, out var job)) 
				return;
			job.Message = m;
			job.WaitHandle.Set();
			e.Status = MessageStatus.Complete;
		}

		#region Public

		public bool ListenForClientSessionMessages
		{
			get => _listenForClientSessionMessages;
			protected set => HandleListenForClientMessagesChanged(value);
		}

		public bool ListenForRegistrations
		{
			get => _listenForRegistrations;
			protected set => HandleListenForRegistrationsChanged(value);
		}

		public bool ListenForServerJobs
		{
			get => _listenForServerJobs;
			protected set => HandleListenForServerJobsChanged(value);
		}

		public event PluginEventDelegate PluginEvent;
		public event OnMessageReceivedDelegate ClientSessionMessageReceived
		{
			add
			{
				_clientSessionMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Combine(_clientSessionMessageReceivedDelegate, value);
				ListenForClientSessionMessages = true;
			}
			remove
			{
				_clientSessionMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Remove(_clientSessionMessageReceivedDelegate, value);
				if (!_clientSessionMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListenForClientSessionMessages = false;
			}
		}

		public event OnMessageReceivedDelegate RegistrationReceived
		{
			add
			{
				_registrationMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Combine(_registrationMessageReceivedDelegate, value);
				ListenForRegistrations = true;
			}
			remove
			{
				_registrationMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Remove(_registrationMessageReceivedDelegate, value);
				if (!_registrationMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListenForRegistrations = false;
			}
		}

		public event OnMessageReceivedDelegate ServerJobReceived
		{
			add
			{
				_serverJobsMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Combine(_serverJobsMessageReceivedDelegate, value);
				ListenForServerJobs = true;
			}
			remove
			{
				_serverJobsMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Remove(_serverJobsMessageReceivedDelegate, value);
				if (!_serverJobsMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListenForServerJobs = false;
			}
		}

		public Task<IMessage> WaitForReplyTo(IMessage message, CancellationToken? token = null, TimeSpan? timeout = null)
		{
			return Task.Run(() =>
			{
				var calculatedTimeout = (timeout ?? message.TimeToLive);
				calculatedTimeout = calculatedTimeout == TimeSpan.Zero
					? TimeSpan.FromSeconds(10)
					: calculatedTimeout;
				var job = new MessageWaitJob(DateTime.UtcNow + calculatedTimeout);
				ReplyWaitList.TryAdd(message.Identifier, job);
				try
				{
					if (token.HasValue)
						job.WaitHandle.Wait(calculatedTimeout, token.Value);
					else
						job.WaitHandle.Wait(calculatedTimeout);
					return ReplyWaitList.TryGetValue(message.Identifier, out var reply)
						? reply.Message
						: null;
				}
				catch (Exception ex)
				{
					Logger.Warn("Encountered an error while waiting for a reply message", ex);
				}
				finally
				{
					ReplyWaitList.TryRemove(message.Identifier, out _);
				}
				return null;
			});
			
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			_clientSessionMessageReceivedDelegate = null;
			_serverJobsMessageReceivedDelegate = null;
			_registrationMessageReceivedDelegate = null;
			ListenForClientSessionMessages = false;
			ListenForRegistrations = false;
			ListenForServerJobs = false;
		}

		#endregion
	}
}
