using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Apollo.Common.Abstractions;
using Xunit.Abstractions;

namespace Apollo.Mocks
{
	public class MockQueue : ConcurrentQueue<IMessage>
	{
		public Action MessageArrived { get; set; }

		public void SignalMessageArrived()
		{
			MessageArrived?.Invoke();
		}
	}

	public class MockService : IDisposable
	{
		private ITestOutputHelper _logger;
		public DateTime StartTimeUtc { get; } = DateTime.UtcNow;

		public MockService(ITestOutputHelper logger)
		{
			_logger = logger;
			var headings = $"{"Time".PadRight(9)} | {"SENDER/RECIEVER".PadRight(18)}  <=>  {"MESSAGE".PadRight(7)} | {"QUEUE".PadRight(14)} | {"ADDRESSEE".PadRight(18)} | LABEL";
			logger.WriteLine(headings);
			logger.WriteLine(new string('-', headings.Length));
		}

		public int MessageCounter;

		public Dictionary<ApolloQueue, MockQueue> NormalQueues = new Dictionary<ApolloQueue, MockQueue>
		{
			{ ApolloQueue.Aliases, new MockQueue() },
			{ ApolloQueue.Registrations, new MockQueue() },
			{ ApolloQueue.ServerRequests, new MockQueue() }
		};

		public Dictionary<ApolloQueue, ConcurrentDictionary<string, MockQueue>> SessionQueues = new Dictionary<ApolloQueue, ConcurrentDictionary<string, MockQueue>>()
		{
			{ ApolloQueue.ClientSessions, new ConcurrentDictionary<string, MockQueue>(StringComparer.OrdinalIgnoreCase) }
		};

		private int _pendingMessages;

		public MockQueue GetQueue(ApolloQueue queueType, string targetSession)
		{
			return !NormalQueues.TryGetValue(queueType, out var queue) 
				? GetSessionQueue(queueType, targetSession) 
				: queue;
		}

		private MockQueue GetSessionQueue(ApolloQueue queueType, string identifier)
		{
			return SessionQueues.TryGetValue(queueType, out var queue) 
				? queue.GetOrAdd(identifier, new MockQueue()) 
				: null;
		}

		public int PendingMessages
		{
			get => _pendingMessages;
			private set => _pendingMessages = value;
		}

		public void Enqueue(IMessage message, ApolloQueue queueType, string session)
		{
			var queue = GetQueue(queueType, session);
			queue.Enqueue(message);
			queue.SignalMessageArrived();
			Interlocked.Increment(ref _pendingMessages);
			if (_pendingMessages != 0)
				QueuesEmpty.Reset();

		}

		public IMessage Dequeue(ApolloQueue queueType, string session)
		{
			if (!GetQueue(queueType, session).TryDequeue(out var message)) 
				return null;
			Interlocked.Decrement(ref _pendingMessages);
			if (_pendingMessages == 0)
				QueuesEmpty.Set();
			return message;
		}

		public List<ExceptionDispatchInfo> AsyncListeningExceptions = new List<ExceptionDispatchInfo>();

		public ManualResetEventSlim QueuesEmpty { get; } = new ManualResetEventSlim();

		public event Action Disposed;
		#region IDisposable

		public void Dispose()
		{
			Disposed?.Invoke();
			AsyncListeningExceptions.FirstOrDefault()?.Throw();
		}

		#endregion
	}
}
