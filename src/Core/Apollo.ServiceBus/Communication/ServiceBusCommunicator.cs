﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Reactive;

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

		private IDictionary<ApolloQueue, ICollection<MessageHandler>> Handlers = new Dictionary<ApolloQueue, ICollection<MessageHandler>>();

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
					listener.Subscribe(delegate (KeyValuePair<string, object> evnt)
					{
						// Log operation details once it's done
						if (!evnt.Key.EndsWith("Stop")) 
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

		protected virtual MessageStatus OnMessageFirstReceived(IServiceCommunicator serviceCommunicator, IMessage message)
		{
			if (ReplyWaitList.TryGetValue(message.Identifier, out var waiter))
			{
				waiter.WaitHandle.Set();
				return MessageStatus.Complete;
			}
			return MessageStatus.Unhandled;
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

		public void RemoveListenersForPlugin(ApolloPluginBase plugin)
		{
			foreach (var queueType in Handlers)
			{
				foreach (var itemToRemove in queueType.Value.Where(h => h.Plugin == plugin).ToArray())
					RemoveHandler(queueType.Key, itemToRemove);
			}
		}

		public bool ListeningForClientSessionMessages
		{
			get => _listenForClientSessionMessages;
			protected set => HandleListenForClientMessagesChanged(value);
		}

		public bool ListeningForRegistrations
		{
			get => _listenForRegistrations;
			protected set => HandleListenForRegistrationsChanged(value);
		}

		public bool ListeningForServerJobs
		{
			get => _listenForServerJobs;
			protected set => HandleListenForServerJobsChanged(value);
		}

		public bool ListeningForAliasMessages
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
				ListeningForAliasMessages = true;
			}
			remove
			{
				_aliasMessageReceivedDelegate = (OnMessageReceivedDelegate)Delegate.Remove(_aliasMessageReceivedDelegate, value);
				if (!_aliasMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListeningForAliasMessages = false;
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
				ListeningForClientSessionMessages = true;
			}
			remove
			{
				_clientSessionMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Remove(_clientSessionMessageReceivedDelegate, value);
				if (!_clientSessionMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListeningForClientSessionMessages = false;
			}
		}

		public event OnMessageReceivedDelegate RegistrationReceived
		{
			add
			{
				_registrationMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Combine(_registrationMessageReceivedDelegate, value);
				ListeningForRegistrations = true;
			}
			remove
			{
				_registrationMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Remove(_registrationMessageReceivedDelegate, value);
				if (!_registrationMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListeningForRegistrations = false;
			}
		}

		public event OnMessageReceivedDelegate ServerJobReceived
		{
			add
			{
				_serverJobsMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Combine(_serverJobsMessageReceivedDelegate, value);
				ListeningForServerJobs = true;
			}
			remove
			{
				_serverJobsMessageReceivedDelegate = (OnMessageReceivedDelegate) Delegate.Remove(_serverJobsMessageReceivedDelegate, value);
				if (!_serverJobsMessageReceivedDelegate?.GetInvocationList().Any() ?? false)
					ListeningForServerJobs = false;
			}
		}

		public Task<IMessage> WaitForReplyTo(IMessage message, CancellationToken? token = null, TimeSpan? timeout = null)
		{
			return Task.Run(() =>
			{
				var calculatedTimeout = (timeout ?? message.TimeToLive);
				if (calculatedTimeout > ApolloConstants.MaximumReplyWaitTime)
					calculatedTimeout = ApolloConstants.MaximumReplyWaitTime;
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

		public void AddHandler(ApolloQueue queueType, MessageHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			lock (Handlers)
			{
				if (Handlers.ContainsKey(queueType))
					Handlers[queueType].Add(handler);
				else
					Handlers.Add(queueType, new List<MessageHandler> { handler });
			}
		}

		public void RemoveHandler(ApolloQueue queueType, MessageHandler handler)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			lock (Handlers)
			{
				if (!Handlers.ContainsKey(queueType))
					return;
				Handlers[queueType].Remove(handler);
				if (!Handlers[queueType].Any())
					Handlers.Remove(queueType);
			}
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			_clientSessionMessageReceivedDelegate = null;
			_serverJobsMessageReceivedDelegate = null;
			_registrationMessageReceivedDelegate = null;
			ListeningForClientSessionMessages = false;
			ListeningForRegistrations = false;
			ListeningForServerJobs = false;
			Configuration.Connection?.CloseAsync().Wait();
		}

		#endregion
	}
}
