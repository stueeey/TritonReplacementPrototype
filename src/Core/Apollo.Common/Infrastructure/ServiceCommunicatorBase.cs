using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using log4net;

namespace Apollo.Common.Infrastructure
{
	public abstract class ServiceCommunicatorBase : IServiceCommunicator
	{
		protected class MessageWaitJob
		{
			public MessageWaitJob(DateTime expiryTimeUtc)
			{
				ExpiryTimeUtc = expiryTimeUtc;
			}

			public ManualResetEventSlim WaitHandle { get; } = new ManualResetEventSlim();
			public ConcurrentQueue<IMessage> Messages { get; set; } = new ConcurrentQueue<IMessage>();
			public DateTime ExpiryTimeUtc { get; set; }
		}

		protected ConcurrentDictionary<string, MessageWaitJob> ReplyWaitList { get; } = new ConcurrentDictionary<string, MessageWaitJob>();

		protected ServiceCommunicatorBase()
		{
			var systemMessageHandler = new MessageHandler(null, OnMessageFirstReceived);
			foreach (ApolloQueue queueType in Enum.GetValues(typeof(ApolloQueue)))
				AddHandler(queueType, systemMessageHandler);
		}

		#region Implementation of IDisposable

		public virtual void Dispose()
		{
			ListeningForClientSessionMessages = false;
			ListeningForRegistrations = false;
			ListeningForServerJobs = false;
			ListeningForAliasMessages = false;
		}

		#endregion



		#region Implementation of IServiceCommunicator

		protected ILog Logger { get; set; }

		public ConcurrentDictionary<string, object> State { get; } = new ConcurrentDictionary<string, object>();
		public IMessageFactory MessageFactory { get; protected set; }
		public abstract Task SendToClientsAsync(params IMessage[] messages);
		public async Task SendToClientAsync(string clientIdentifier, params IMessage[] messages)
		{
			if (messages == null)
				return;
			if (string.IsNullOrWhiteSpace(clientIdentifier))
				throw new ArgumentException("client identifier cannot be blank or null", nameof(clientIdentifier));
			foreach (var message in messages)
				message.TargetSession = clientIdentifier;
			await SendToClientsAsync(messages);
		}
		public abstract Task SendToServerAsync(params IMessage[] messages);
		public abstract Task SendToRegistrationsAsync(params IMessage[] messages);
		public abstract Task SendToAliasAsync(params IMessage[] messages);
		public async Task SendToAliasAsync(string alias, params IMessage[] messages)
		{
			if (messages == null)
				return;
			if (string.IsNullOrWhiteSpace(alias))
				throw new ArgumentException("client identifier cannot be blank or null", nameof(alias));
			foreach (var message in messages)
				message.TargetSession = alias;
			await SendToAliasAsync(messages);
		}

		public bool ListeningForClientSessionMessages
		{
			get => _listenForClientSessionMessages;
			private set
			{
				if (_listenForClientSessionMessages == value)
					return;
				_listenForClientSessionMessages = value;
				HandleListenForClientMessagesChanged(value);
			}
		}

		protected abstract void HandleListenForClientMessagesChanged(bool value);

		public bool ListeningForRegistrations
		{
			get => _listenForRegistrations;
			private set
			{
				if (_listenForRegistrations == value)
					return;
				_listenForRegistrations = value;
				HandleListenForRegistrationsChanged(value);
			}
		}

		protected abstract void HandleListenForRegistrationsChanged(bool value);

		public bool ListeningForServerJobs
		{
			get => _listenForServerJobs;
			private set
			{
				if (_listenForServerJobs == value)
					return;
				_listenForServerJobs = value;
				HandleListenForServerJobsChanged(value);
			}
		}

		protected abstract void HandleListenForServerJobsChanged(bool value);

		public bool ListeningForAliasMessages
		{
			get => _listenForAliasSessionMessages;
			private set
			{
				if (_listenForAliasSessionMessages == value)
					return;
				_listenForAliasSessionMessages = value;
				HandleListenForAliasMessagesChanged(value);
			}
		}

		protected abstract void HandleListenForAliasMessagesChanged(bool value);

		protected bool _listenForClientSessionMessages;
		protected bool _listenForRegistrations;
		protected bool _listenForServerJobs;
		protected bool _listenForAliasSessionMessages;

		public event PluginEventDelegate PluginEvent;
		public event OnMessageSent AnyMessageSent;
		public event OnMessageReceived AnyMessageReceived;

		public Task<ICollection<IMessage>> WaitForRepliesAsync(ReplyOptions options)
		{
			return Task.Run(() => WaitForReplies(options));
		}

		#endregion

		protected abstract TimeSpan MaxReplyWaitTime { get; }
		protected abstract ApolloQueue? ParseQueue(string queueName);

		protected readonly IDictionary<ApolloQueue, ICollection<MessageHandler>> Handlers = new Dictionary<ApolloQueue, ICollection<MessageHandler>>();

		public void SignalPluginEvent(string eventName, object state)
		{
			PluginEvent?.Invoke(eventName, state);
		}

		public void RemoveListenersForPlugin(ApolloPlugin plugin)
		{
			foreach (var queueType in Handlers)
			{
				foreach (var itemToRemove in queueType.Value.Where(h => h.Plugin == plugin).ToArray())
					RemoveHandler(queueType.Key, itemToRemove);
			}
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
				if (Handlers[queueType].Count > 1)
					ListenToQueue(queueType, true);
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
				else if (Handlers[queueType].Count <= 1)
					ListenToQueue(queueType, false);
			}
		}

		protected void ListenToQueue(ApolloQueue queueType, bool listen)
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


		protected void OnMessageSent(IMessage message, ApolloQueue queue)
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

		protected virtual MessageStatus OnMessageFirstReceived(ApolloQueue sourceQueue, IMessage message, CancellationToken? token)
		{
			OnMessageReceived(message, sourceQueue);
			return CheckIfAnyoneIsWaitingForMessage(message) 
				? MessageStatus.Complete 
				: MessageStatus.Unhandled;
		}

		private bool CheckIfAnyoneIsWaitingForMessage(IMessage m)
		{
			if (string.IsNullOrEmpty(m.ResponseTo) || !ReplyWaitList.TryGetValue(m.ResponseTo, out var job)) 
				return false;
			job.Messages.Enqueue(m);
			job.WaitHandle.Set();
			return true;
		}

		private ICollection<IMessage> WaitForReplies(ReplyOptions options)
		{
			var replyQueue = ParseQueue(options.ReplyQueue) ?? throw new ArgumentException($"Cannot wait for a reply to a message which does not have a valid {nameof(IMessage.ReplyToEntity)}");
			var calculatedTimeout = options.Timeout;
			calculatedTimeout = calculatedTimeout == TimeSpan.Zero
				? TimeSpan.FromSeconds(10)
				: calculatedTimeout;
			if (calculatedTimeout > MaxReplyWaitTime)
				calculatedTimeout = MaxReplyWaitTime;
			var job = new MessageWaitJob(DateTime.UtcNow + calculatedTimeout);
			ReplyWaitList.TryAdd(options.MessageIdentifier, job);
			var replyHandlerToForceListening = MessageHandler.CreateFakeHandler();
			AddHandler(replyQueue, replyHandlerToForceListening);
			var retVal = new List<IMessage>();
			try
			{
				while (true)
				{
					if (options.CancelToken.HasValue)
						job.WaitHandle.Wait(calculatedTimeout, options.CancelToken.Value);
					else
						job.WaitHandle.Wait(calculatedTimeout);
					if ((job?.Messages?.Count ?? 0) == 0)
						return retVal;

					// ReSharper disable once PossibleNullReferenceException - not true, see above
					while (job.Messages.TryDequeue(out var message))
					{
						retVal.Add(message);
						options.OnReplyReceived(message);
						if (retVal.Count >= options.MaxRepliesToWaitFor || options.IsTerminatingMessage != null && options.IsTerminatingMessage(message))
							return retVal;
					}
					if (DateTime.UtcNow >= job.ExpiryTimeUtc || options.CancelToken.HasValue && options.CancelToken.Value.IsCancellationRequested)
						return retVal;
				}
			}
			catch (Exception ex)
			{
				Logger.Warn("Encountered an error while waiting for a reply message", ex);
			}
			finally
			{
				ReplyWaitList.TryRemove(options.MessageIdentifier, out _);
				RemoveHandler(replyQueue, replyHandlerToForceListening);
			}

			return null;
		}
	}
}
