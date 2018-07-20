using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using Xunit.Abstractions;


namespace Apollo.Mocks
{
	public class MockServiceCommunicator : IServiceCommunicator
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

		private readonly MockService _service;
		private readonly string _identifier;
		private readonly ITestOutputHelper _logger;

		public MockServiceCommunicator(string identifier, MockService service, ITestOutputHelper logger = null)
		{
			_logger = logger;
			MessageFactory = new MockMessageFactory("ClientSessions", identifier, service);
			_service = service;
			_identifier = identifier;

			var systemMessageHandler = new MessageHandler(null, OnMessageFirstReceived);
			foreach (ApolloQueue queueType in Enum.GetValues(typeof(ApolloQueue)))
				AddHandler(queueType, systemMessageHandler);
			if (logger != null)
			{
				AnyMessageSent += (m, q) => _logger.WriteLine($"{_identifier.PadRight(18)}  ==>  {m.Identifier.PadRight(7)} | {q.ToString().PadRight(14)} | {(m.TargetSession ?? "").PadRight(18)} | {m.Label.PadRight(50)}");
				AnyMessageReceived += (m, q) => _logger.WriteLine($"{_identifier.PadRight(18)}  <==  {m.Identifier.PadRight(7)}");
			}
		}

		protected virtual MessageStatus OnMessageFirstReceived(ApolloQueue sourceQueue, IMessage message, CancellationToken? token)
		{
			OnMessageReceived(message, sourceQueue);
			return CheckIfAnyoneIsWaitingForMessage(message) 
				? MessageStatus.Complete 
				: MessageStatus.Unhandled;
		}

		#region Implementation of IDisposable

		public void Dispose()
		{
			ListeningForAliasMessages = false;
			ListeningForClientSessionMessages = false;
			ListeningForRegistrations = false;
			ListeningForServerJobs = false;
		}

		#endregion

		#region Implementation of IServiceCommunicator

		public ConcurrentDictionary<string, object> State { get; } = new ConcurrentDictionary<string, object>();

		protected ConcurrentDictionary<string, MessageWaitJob> ReplyWaitList { get; } = new ConcurrentDictionary<string, MessageWaitJob>();

		public T GetState<T>(string key)
		{
			return State.TryGetValue(key, out var value)
				? (T)value
				: default(T);
		}

		public void SignalPluginEvent(string eventName, object state = null)
		{
			PluginEvent?.Invoke(eventName, state);
		}

		private readonly IDictionary<ApolloQueue, ICollection<MessageHandler>> _handlers = new Dictionary<ApolloQueue, ICollection<MessageHandler>>();
		private bool _listeningForClientSessionMessages;
		private bool _listeningForRegistrations;
		private bool _listeningForServerJobs;
		private bool _listeningForAliasMessages;

		public IMessageFactory MessageFactory { get; }
		public void RemoveListenersForPlugin(ApolloPluginBase plugin)
		{
			foreach (var queueType in _handlers)
			{
				foreach (var itemToRemove in queueType.Value.Where(h => h.Plugin == plugin).ToArray())
					RemoveHandler(queueType.Key, itemToRemove);
			}
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

		private void OnMessageSent(IMessage message, ApolloQueue queue)
		{
			AnyMessageSent?.Invoke(message, queue);
		}

		private void OnMessageReceived(IMessage message, ApolloQueue queue)
		{
			AnyMessageReceived?.Invoke(message, queue);
		}

		public Task SendToClientAsync(IMessage message, CancellationToken? token = null)
		{
			return Task.Run(() =>
			{
				OnMessageSent(message, ApolloQueue.ClientSessions);
				_service.Enqueue(message, ApolloQueue.ClientSessions, message.TargetSession);
			});
		}

		public async Task SendToClientAsync(CancellationToken? token, params IMessage[] messages)
		{
			foreach (var message in messages)
				await SendToClientAsync(message, token);
		}

		public async Task SendToClientAsync(params IMessage[] messages)
		{
			await SendToClientAsync(null, messages);
		}

		public Task SendToServerAsync(IMessage message, CancellationToken? token = null)
		{
			return Task.Run(() =>
			{
				OnMessageSent(message, ApolloQueue.ServerRequests);
				_service.Enqueue(message, ApolloQueue.ServerRequests, null);
			});
		}

		public async Task SendToServerAsync(CancellationToken? token, params IMessage[] messages)
		{
			foreach (var message in messages)
				await SendToServerAsync(message, token);
		}

		public async Task SendToServerAsync(params IMessage[] messages)
		{
			await SendToServerAsync(null, messages);
		}

		public Task SendRegistrationMessageAsync(IMessage message, CancellationToken? token = null)
		{
			return Task.Run(() =>
			{
				OnMessageSent(message, ApolloQueue.Registrations);
				_service.Enqueue(message, ApolloQueue.Registrations, null);
			});
		}

		public async Task SendRegistrationMessageAsync(CancellationToken? token, params IMessage[] messages)
		{
			foreach (var message in messages)
				await SendRegistrationMessageAsync(message, token);
		}

		public async Task SendRegistrationMessageAsync(params IMessage[] messages)
		{
			await SendRegistrationMessageAsync(null, messages);
		}

		public Task SendToAliasAsync(string alias, IMessage message, CancellationToken? token = null)
		{
			message.Properties[ApolloConstants.TargetAliasKey] = alias;
			OnMessageSent(message, ApolloQueue.Aliases);
			return Task.Run(() => _service.Enqueue(message, ApolloQueue.Aliases, null));
		}

		public async Task SendToAliasAsync(string alias, CancellationToken? token, params IMessage[] messages)
		{
			foreach (var message in messages)
				await SendToAliasAsync(alias, message, token);
		}

		public async Task SendToAliasAsync(string alias, params IMessage[] messages)
		{
			await SendToAliasAsync(alias, null, messages);
		}

		private void InvokeMessageHandlers(ApolloQueue queue, IMessage message, CancellationToken? token)
		{
			if (message == null)
				return;

			try
			{
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
							_service.AsyncListeningExceptions.Add(ExceptionDispatchInfo.Capture(ex));
							_logger?.WriteLine($"{ex.Message} (This would normally be suppressed by the system)");
							throw;
						}
					}

					if (status == MessageStatus.Unhandled)
						throw new Exception($"{_identifier} received a message from {queue} with label '{message.Label}' which no handler could handle");
					if (!status.HasFlag(MessageStatus.MarkedForDeletion))
						_service.Enqueue(message, queue, _identifier);

				}
				else
					Debug.Assert(false, "Received a message without having any handlers!");
			}
			catch (Exception ex)
			{
				_logger?.WriteLine($"{ex.Message} (This would normally be suppressed by the system)");
				_service.AsyncListeningExceptions.Add(ExceptionDispatchInfo.Capture(ex));
			}
		}

		private bool CheckIfAnyoneIsWaitingForMessage(IMessage m)
		{
			if (string.IsNullOrEmpty(m.ResponseTo) || !ReplyWaitList.TryGetValue(m.ResponseTo, out var job)) 
				return false;
			job.Message = m;
			job.WaitHandle.Set();
			return true;
		}

		private CancellationTokenSource _listenForClientMessagesCancelToken;
		public bool ListeningForClientSessionMessages
		{
			get => _listeningForClientSessionMessages;
			private set
			{
				if (_listeningForClientSessionMessages == value)
					return;
				_listeningForClientSessionMessages = value;
				if (value)
				{
					_listenForClientMessagesCancelToken = new CancellationTokenSource();
					StartListening(ApolloQueue.ClientSessions, _listenForClientMessagesCancelToken.Token, () => ListeningForClientSessionMessages);
				}
				else
					_listenForClientMessagesCancelToken?.Cancel();
			}
		}

		private void StartListening(ApolloQueue queueType, CancellationToken token, Func<bool> isListening)
		{
			Task.Run(() =>
			{
				while (true)
				{
					var queue = _service.GetQueue(queueType, _identifier);
					SpinWait.SpinUntil(() => _service.AsyncListeningExceptions.Count > 0 || !isListening() || !queue.IsEmpty);
					if (!isListening())
						break;
					InvokeMessageHandlers(queueType, _service.Dequeue(queueType, _identifier), token);
				}
			}, token);
		}

		private CancellationTokenSource _listenForRegistrationMessagesCancelToken;
		public bool ListeningForRegistrations
		{
			get => _listeningForRegistrations;
			private set
			{
				if (_listeningForRegistrations == value)
					return;
				_listeningForRegistrations = value;
				if (value)
				{
					_listenForRegistrationMessagesCancelToken = new CancellationTokenSource();
					StartListening(ApolloQueue.Registrations, _listenForRegistrationMessagesCancelToken.Token, () => ListeningForRegistrations);
				}
				else
					_listenForRegistrationMessagesCancelToken?.Cancel();
			}
		}

		private CancellationTokenSource _listenForServerMessagesCancelToken;
		public bool ListeningForServerJobs
		{
			get => _listeningForServerJobs;
			private set
			{
				if (_listeningForServerJobs == value)
					return;
				_listeningForServerJobs = value;
				if (value)
				{
					_listenForServerMessagesCancelToken = new CancellationTokenSource();
					StartListening(ApolloQueue.ServerRequests, _listenForServerMessagesCancelToken.Token, () => ListeningForServerJobs);
				}
				else
					_listenForServerMessagesCancelToken?.Cancel();
			}
		}

		private CancellationTokenSource _listenForAliasMessagesCancelToken;
		public bool ListeningForAliasMessages
		{
			get => _listeningForAliasMessages;
			private set
			{
				if (_listeningForAliasMessages == value)
					return;
				_listeningForAliasMessages = value;
				if (value)
				{
					_listenForAliasMessagesCancelToken = new CancellationTokenSource();
					StartListening(ApolloQueue.Aliases, _listenForAliasMessagesCancelToken.Token, () => ListeningForAliasMessages);
				}
				else
					_listenForAliasMessagesCancelToken?.Cancel();
			}
		}

		public event PluginEventDelegate PluginEvent;
		public event OnMessageSent AnyMessageSent;
		public event OnMessageReceived AnyMessageReceived;

		public const string ClientQueueName = "ClientSessions";
		public const string ServerQueueName = "ServerQueue";
		public const string RegistrationQueueName = "RegistrationQueue";
		public const string AliasQueueName = "AliasQueue";

		private ApolloQueue? GetReplyQueueForMessage(IMessage message)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, AliasQueueName))
				return ApolloQueue.Aliases;
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, ClientQueueName))
				return ApolloQueue.ClientSessions;
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, RegistrationQueueName))
				return ApolloQueue.Registrations;
			if (StringComparer.OrdinalIgnoreCase.Equals(message.ReplyToEntity, ServerQueueName))
				return ApolloQueue.ServerRequests;
			return null;
		}

		public Task<IMessage> WaitForReplyTo(IMessage message, CancellationToken? token = null, TimeSpan? timeout = null)
		{
			return Task.Run(() =>
			{
				if (message == null)
					throw new ArgumentNullException(nameof(message));
				var replyQueue = GetReplyQueueForMessage(message) ?? throw new ArgumentException($"Cannot wait for a reply to a message which does not have a valid {nameof(IMessage.ReplyToEntity)}");
				var calculatedTimeout = (timeout ?? message.TimeToLive);
				if (calculatedTimeout > ApolloConstants.MaximumReplyWaitTime)
					calculatedTimeout = ApolloConstants.MaximumReplyWaitTime;
				calculatedTimeout = calculatedTimeout > TimeSpan.FromSeconds(1)
					? TimeSpan.FromSeconds(1)
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
					if (ReplyWaitList.TryGetValue(message.Identifier, out var reply) && reply.Message != null)
					{
						return reply.Message;
					}
					else
					{
						_logger?.WriteLine($"No response to {message.Identifier}");
						return null;
					}
				}
				catch (Exception ex)
				{
					_logger?.WriteLine($"{ex.Message} (This would normally be suppressed by the system)");
					throw;
				}
				finally
				{
					ReplyWaitList.TryRemove(message.Identifier, out _);
					RemoveHandler(replyQueue, replyHandlerToForceListening);
				}
			});
			
		}

		#endregion
	}
}
