using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Apollo.Common;

namespace Apollo.ServiceBus.Communication
{
	public partial class __ServiceBusCommunicator : IServiceCommunicator
	{
		protected class MessageWaitJob
		{
			public MessageWaitJob(DateTime expiryTimeUtc)
			{
				ExpiryTimeUtc = expiryTimeUtc;
			}

			public ManualResetEventSlim WaitHandle { get; } = new ManualResetEventSlim();
			public List<IMessage> Messages { get; set; }
			public DateTime ExpiryTimeUtc { get; set; }
		}

		private readonly IDictionary<ApolloQueue, ICollection<MessageHandler>> _handlers = new Dictionary<ApolloQueue, ICollection<MessageHandler>>();

		private static readonly ILog ClassLogger = LogManager.GetLogger(Assembly.GetEntryAssembly(), $"{ApolloConstants.LoggerInternalsPrefix}.{MethodBase.GetCurrentMethod().DeclaringType.Name}");
		private static readonly ILog TraceLogger = LogManager.GetLogger(Assembly.GetEntryAssembly(), $"{ApolloConstants.LoggerInternalsPrefix}.Tracing");
		protected ILog Logger { get; private set; }
		protected ServiceBusConfiguration Configuration { get; }

		private IServiceBusImplementations Impl { get; }
		protected Lazy<IMessageReceiver> RegistrationListener => Impl.RegistrationListener;
		protected Lazy<IMessageSender> RegistrationSender => Impl.RegistrationSender;
		protected Lazy<IMessageReceiver> ServerQueueListener => Impl.ServerQueueListener;
		protected Lazy<IMessageSender> ServerQueueSender => Impl.ServerQueueSender;
		protected Lazy<ISessionClient> ClientSessionListener => Impl.ClientSessionListener;
		protected Lazy<IMessageSender> ClientSessionSender => Impl.ClientSessionSender;
		protected Lazy<IMessageReceiver> AliasQueueListener => Impl.AliasQueueListener;
		protected Lazy<IMessageSender> AliasQueueSender => Impl.AliasQueueSender;

		protected ConcurrentDictionary<string, MessageWaitJob> ReplyWaitList { get; } = new ConcurrentDictionary<string, MessageWaitJob>();

		private async Task InvokeMessageHandlers(IReceiverClient receiver, ApolloQueue queue, ServiceBusMessage message, CancellationToken? token)
		{
			if (message == null)
				return;

			if (_handlers.TryGetValue(queue, out var handlers))
			{
				var status = MessageStatus.Unhandled;
				foreach (var handler in handlers.Where(h => h.PassesFilter(message)))
				{
					try
					{
						if (token?.IsCancellationRequested ?? false)
							return;
						status = handler.HandleMessage(queue, message, token);
						if (status.HasFlag(MessageStatus.Handled))
							break;
					}
					catch (Exception ex)
					{
						Logger.Error($"Encountered an error in {handler.OnMessageReceived.Method.DeclaringType?.Name ?? "<Unknown>"}.{handler.OnMessageReceived.Method.Name} while handling a message labelled {message.Label}", ex);
					}
				}
				if (status.HasFlag(MessageStatus.MarkedForDeletion))
					await receiver.CompleteAsync(message.InnerMessage.SystemProperties.LockToken);
				else if (string.IsNullOrWhiteSpace(message.ResponseTo))
					await receiver.DeadLetterAsync(message.InnerMessage.SystemProperties.LockToken, $"{State[ApolloConstants.RegisteredAsKey]} does not have a plugin which can handle this message");
				else
					await receiver.DeadLetterAsync(message.InnerMessage.SystemProperties.LockToken, $"{State[ApolloConstants.RegisteredAsKey]} is not expecting or longer waiting for this response");
			}
			else
				Debug.Assert(false, "Received a message without having any handlers!");
		}

		private bool CheckIfAnyoneIsWaitingForMessage(IMessage m)
		{
			if (string.IsNullOrEmpty(m.ResponseTo) || !ReplyWaitList.TryGetValue(m.ResponseTo, out var job)) 
				return false;
			job.Messages.Add(m);
			job.WaitHandle.Set();
			return true;
		}

		#region Public

		public ConcurrentDictionary<string, object> State { get; } = new ConcurrentDictionary<string, object>();

		public IMessageFactory MessageFactory { get; }

		public void SetLogger(ILog log = null) => Logger = log ?? ClassLogger;

		public __ServiceBusCommunicator(ServiceBusConfiguration configuration, IServiceBusImplementations serviceBusImplementations = null)
		{
			SetLogger();

			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			State[ApolloConstants.RegisteredAsKey] = configuration.Identifier;
			MessageFactory = new ServiceBusMessageFactory(ServiceBusConstants.DefaultRegisteredClientsQueue, configuration.Identifier);
			Impl = serviceBusImplementations ?? new DefaultServiceBusImplementations(configuration);
			DiagnosticListener.AllListeners.Subscribe(delegate (DiagnosticListener listener)
			{
				// subscribe to the Service Bus DiagnosticSource
				if (listener.Name == "Microsoft.Azure.ServiceBus")
				{
					// receive event from Service Bus DiagnosticSource
					listener.Subscribe(delegate (KeyValuePair<string, object> @event)
					{
						// Log operation details once it's done
						if (!@event.Key.EndsWith("Stop")) 
							return;
						var currentActivity = Activity.Current;
						TraceLogger.Debug($"{currentActivity.OperationName} Duration: {currentActivity.Duration}\n\t{string.Join("\n\t", currentActivity.Tags)}");
						
					});
				}
			});
			Logger.Info("Connecting to service bus with the following settings:");
			Logger.Info($"Endpoint: {Configuration.ConnectionStringBuilder.Endpoint}");
			Logger.Info($"Transport: {Configuration.ConnectionStringBuilder.TransportType}");
			Logger.Info($"Using SAS Key: {Configuration.ConnectionStringBuilder.SasKeyName}");
			Logger.Info($"To Entity: {Configuration.ConnectionStringBuilder.EntityPath}");
			Logger.Info($"As: {Configuration.Identifier}");

			var systemMessageHandler = new MessageHandler(null, OnMessageFirstReceived);
			foreach (ApolloQueue queueType in Enum.GetValues(typeof(ApolloQueue)))
				AddHandler(queueType, systemMessageHandler);
		}

		private void ListenToQueue(ApolloQueue queueType, bool listen)
		{
			switch (queueType)
			{
				case ApolloQueue.Registrations:
					ListeningForRegistrations = listen;
					break;
				case ApolloQueue.ServerRequests:
					ListeningForServerJobs = listen;
					break;
				case ApolloQueue.Aliases:
					ListeningForAliasMessages = listen;
					break;
				case ApolloQueue.ClientSessions:
					ListeningForClientSessionMessages = listen;
					break;
			}
		}

		protected virtual MessageStatus OnMessageFirstReceived(ApolloQueue sourceQueue, IMessage message, CancellationToken? token)
		{
			OnMessageReceived(message, sourceQueue);
			return CheckIfAnyoneIsWaitingForMessage(message) 
				? MessageStatus.Complete 
				: MessageStatus.Unhandled;
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

		public void RemoveListenersForPlugin(ApolloPlugin plugin)
		{
			foreach (var queueType in _handlers)
			{
				foreach (var itemToRemove in queueType.Value.Where(h => h.Plugin == plugin).ToArray())
					RemoveHandler(queueType.Key, itemToRemove);
			}
		}

		public bool ListeningForClientSessionMessages
		{
			get => _listenForClientSessionMessages;
			private set => HandleListenForClientMessagesChanged(value);
		}

		public bool ListeningForRegistrations
		{
			get => _listenForRegistrations;
			private set => HandleListenForRegistrationsChanged(value);
		}

		public bool ListeningForServerJobs
		{
			get => _listenForServerJobs;
			private set => HandleListenForServerJobsChanged(value);
		}

		public bool ListeningForAliasMessages
		{
			get => _listenForAliasSessionMessages;
			private set => HandleListenForAliasMessagesChanged(value);
		}

		public event PluginEventDelegate PluginEvent;

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
		public Task<ICollection<IMessage>> WaitForRepliesAsync(ReplyOptions options)
		{
			throw new NotImplementedException();
		}

		public Task<List<IMessage>> WaitForRepliesTo(IMessage message, CancellationToken? token = null, TimeSpan? timeout = null, Predicate<IMessage> shouldStopWaiting = null)
		{
			return Task.Run(() =>
			{
				if (message == null)
					throw new ArgumentNullException(nameof(message));
				var replyQueue = GetReplyQueueForMessage(message) ?? throw new ArgumentException($"Cannot wait for a reply to a message which does not have a valid {nameof(IMessage.ReplyToEntity)}");
				var calculatedTimeout = (timeout ?? message.TimeToLive);
				if (calculatedTimeout > ApolloConstants.MaximumReplyWaitTime)
					calculatedTimeout = ApolloConstants.MaximumReplyWaitTime;
				calculatedTimeout = calculatedTimeout == TimeSpan.Zero
					? TimeSpan.FromSeconds(10)
					: calculatedTimeout;
				var job = new MessageWaitJob(DateTime.UtcNow + calculatedTimeout);
				ReplyWaitList.TryAdd(message.Identifier, job);
				var replyHandlerToForceListening = MessageHandler.CreateFakeHandler();
				AddHandler(replyQueue, replyHandlerToForceListening);
				try
				{
					if (token.HasValue)
						job.WaitHandle.Wait(calculatedTimeout, token.Value);
					else
						job.WaitHandle.Wait(calculatedTimeout);
					return ReplyWaitList.TryGetValue(message.Identifier, out var reply)
						? reply.Messages
						: null;
				}
				catch (Exception ex)
				{
					Logger.Warn("Encountered an error while waiting for a reply message", ex);
				}
				finally
				{
					ReplyWaitList.TryRemove(message.Identifier, out _);
					RemoveHandler(replyQueue, replyHandlerToForceListening);
				}
				return null;
			});
		}

		private ApolloQueue? GetReplyQueueForMessage(IMessage message)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, Configuration.ClientAliasesQueue))
				return ApolloQueue.Aliases;
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, Configuration.RegisteredClientsQueue))
				return ApolloQueue.ClientSessions;
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, Configuration.RegistrationQueue))
				return ApolloQueue.Registrations;
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, Configuration.ServerRequestsQueue))
				return ApolloQueue.ServerRequests;
			return null;
		}

		public void AddHandler(ApolloQueue queueType, MessageHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			lock (_handlers)
			{
				if (_handlers.ContainsKey(queueType))
					_handlers[queueType].Add(handler);
				else
					_handlers.Add(queueType, new List<MessageHandler> { handler });
				if (_handlers[queueType].Count > 1)
					ListenToQueue(queueType, true);
			}
		}

		public void RemoveHandler(ApolloQueue queueType, MessageHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			lock (_handlers)
			{
				if (!_handlers.ContainsKey(queueType))
					return;
				_handlers[queueType].Remove(handler);
				if (!_handlers[queueType].Any())
					_handlers.Remove(queueType);
				else if (_handlers[queueType].Count <= 1)
					ListenToQueue(queueType, false);
			}
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			ListeningForClientSessionMessages = false;
			ListeningForRegistrations = false;
			ListeningForServerJobs = false;
			Configuration.Connection?.CloseAsync().Wait();
		}

		#endregion
	}
}
