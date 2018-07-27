﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public abstract void Dispose();

		#endregion

		protected bool _listenForClientSessionMessages;
		protected bool _listenForRegistrations;
		protected bool _listenForServerJobs;
		protected bool _listenForAliasSessionMessages;

		#region Implementation of IServiceCommunicator

		protected abstract ILog Logger { get; }

		public abstract ConcurrentDictionary<string, object> State { get; }
		public abstract T GetState<T>(string key);
		public abstract void SignalPluginEvent(string eventName, object state = null);
		public abstract IMessageFactory MessageFactory { get; }
		public abstract void RemoveListenersForPlugin(ApolloPluginBase plugin);
		public abstract Task SendToClientAsync(IMessage message, CancellationToken? token = null);
		public abstract Task SendToClientAsync(CancellationToken? token, params IMessage[] messages);
		public abstract Task SendToClientAsync(params IMessage[] messages);
		public abstract Task SendToServerAsync(IMessage message, CancellationToken? token = null);
		public abstract Task SendToServerAsync(CancellationToken? token, params IMessage[] messages);
		public abstract Task SendToServerAsync(params IMessage[] messages);
		public abstract Task SendRegistrationMessageAsync(IMessage message, CancellationToken? token = null);
		public abstract Task SendRegistrationMessageAsync(CancellationToken? token, params IMessage[] messages);
		public abstract Task SendRegistrationMessageAsync(params IMessage[] messages);
		public abstract Task SendToAliasAsync(string alias, IMessage message, CancellationToken? token = null);
		public abstract Task SendToAliasAsync(string alias, CancellationToken? token, params IMessage[] messages);
		public abstract Task SendToAliasAsync(string alias, params IMessage[] messages);
		public bool ListeningForClientSessionMessages
		{
			get => _listenForClientSessionMessages;
			private set => HandleListenForClientMessagesChanged(value);
		}

		protected abstract void HandleListenForClientMessagesChanged(bool value);

		public bool ListeningForRegistrations
		{
			get => _listenForRegistrations;
			private set => HandleListenForRegistrationsChanged(value);
		}

		protected abstract void HandleListenForRegistrationsChanged(bool value);

		public bool ListeningForServerJobs
		{
			get => _listenForServerJobs;
			private set => HandleListenForServerJobsChanged(value);
		}

		protected abstract void HandleListenForServerJobsChanged(bool value);

		public bool ListeningForAliasMessages
		{
			get => _listenForAliasSessionMessages;
			private set => HandleListenForAliasMessagesChanged(value);
		}

		protected abstract void HandleListenForAliasMessagesChanged(bool value);

		public event PluginEventDelegate PluginEvent;
		public event OnMessageSent AnyMessageSent;
		public event OnMessageReceived AnyMessageReceived;

		public Task<ICollection<IMessage>> WaitForRepliesAsync(ReplyOptions options)
		{
			return Task.Run(() => WaitForReplies(options));
		}

		#endregion

		protected abstract TimeSpan MaxReplyWaitTime { get; }
		protected abstract ApolloQueue? GetReplyQueue(string queueName);

		private readonly IDictionary<ApolloQueue, ICollection<MessageHandler>> _handlers = new Dictionary<ApolloQueue, ICollection<MessageHandler>>();

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
			var replyQueue = GetReplyQueue(options.ReplyQueue) ?? throw new ArgumentException($"Cannot wait for a reply to a message which does not have a valid {nameof(IMessage.ReplyToEntity)}");
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
