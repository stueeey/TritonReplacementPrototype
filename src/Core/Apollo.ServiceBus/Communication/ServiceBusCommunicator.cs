using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Apollo.ServiceBus.Infrastructure;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Apollo.ServiceBus.Communication
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

		private IServiceBusImplementations Impl { get; }
		protected Lazy<IMessageReceiver> RegistrationListener => Impl.RegistrationListener;
		protected Lazy<IMessageSender> RegistrationSender => Impl.RegistrationSender;
		protected Lazy<IMessageReceiver> ServerQueueListener => Impl.ServerQueueListener;
		protected Lazy<IMessageSender> ServerQueueSender => Impl.ServerQueueSender;
		protected Lazy<ISessionClient> ClientSessionListener => Impl.ClientSessionListener;
		protected Lazy<IMessageSender> ClientSessionSender => Impl.ClientSessionSender;
		protected Lazy<IMessageReceiver> AliasQueueListener => Impl.AliasQueueListener;
		protected Lazy<IMessageSender> AliasQueueSender => Impl.AliasQueueSender;

		private static bool RemoveListenersForPlugin(TritonPluginBase plugin, ref OnMessageReceivedDelegate eventHandlers)
		{
			var handlers = eventHandlers.GetInvocationList().Where(h => h.Target == plugin);
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var handler in handlers)
				eventHandlers = (OnMessageReceivedDelegate) Delegate.Remove(eventHandlers, handler);
			return !eventHandlers?.GetInvocationList().Any() ?? true;
		}

		protected ConcurrentDictionary<string, MessageWaitJob> ReplyWaitList { get; } = new ConcurrentDictionary<string, MessageWaitJob>();

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
				await receiver.DeadLetterAsync(message.InnerMessage.SystemProperties.LockToken);
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

		public ConcurrentDictionary<string, object> State { get; } = new ConcurrentDictionary<string, object>();

		public IMessageFactory MessageFactory { get; }

		public void SetLogger(ILog log = null) => Logger = log ?? ClassLogger;

		public ServiceBusCommunicator(ServiceBusConfiguration configuration, IServiceBusImplementations serviceBusImplementations = null)
		{
			SetLogger();

			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));
			State[TritonConstants.RegisteredAsKey] = configuration.ClientIdentifier;
			MessageFactory = new ServiceBusMessageFactory(ServiceBusConstants.DefaultRegisteredClientsQueue, configuration.ClientIdentifier);
			Impl = serviceBusImplementations ?? new DefaultServiceBusImplementations(configuration);
		}

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

		public void RemoveListenersForPlugin(TritonPluginBase plugin)
		{
			if (RemoveListenersForPlugin(plugin, ref _registrationMessageReceivedDelegate))
				ListenForRegistrations = false;
			if (RemoveListenersForPlugin(plugin, ref _aliasMessageReceivedDelegate))
				ListenForAliasMessages = false;
			if (RemoveListenersForPlugin(plugin, ref _clientSessionMessageReceivedDelegate))
				ListenForClientSessionMessages = false;
			if (RemoveListenersForPlugin(plugin, ref _serverJobsMessageReceivedDelegate))
				ListenForServerJobs = false;
		}

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

		public bool ListenForAliasMessages
		{
			get => _listenForAliasSessionMessages;
			protected set => HandleListenForAliasMessagesChanged(value);
		}

		public event PluginEventDelegate PluginEvent;
		public event OnMessageReceivedDelegate AliasMessageReceived
		{
			add
			{
				_aliasMessageReceivedDelegate = (OnMessageReceivedDelegate)Delegate.Combine(_aliasMessageReceivedDelegate, value);
				ListenForAliasMessages = true;
			}
			remove
			{
				_aliasMessageReceivedDelegate = (OnMessageReceivedDelegate)Delegate.Remove(_aliasMessageReceivedDelegate, value);
				if (!_aliasMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListenForAliasMessages = false;
			}
		}

		private void OnMessageSent(IMessage message, ApolloQueue queue)
		{
			try
			{
				AnyMessageSent?.Invoke(message, queue);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to run handlers subscribed to {nameof(OnMessageSent)} as part of sending to the {queue} queue", ex);
			}
		}

		private void OnMessageReceived(IMessage message, ApolloQueue queue)
		{
			try
			{
				AnyMessageReceived?.Invoke(message, queue);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to run handlers subscribed to {nameof(OnMessageReceived)} as part of receiving a message from the {queue} queue", ex);
			}
		}

		public event OnMessageSent AnyMessageSent;
		public event OnMessageReceived AnyMessageReceived;

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
				if (calculatedTimeout > TritonConstants.MaximumReplyWaitTime)
					calculatedTimeout = TritonConstants.MaximumReplyWaitTime;
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

		public Task SendToAliasAsync(params IMessage[] messages)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
