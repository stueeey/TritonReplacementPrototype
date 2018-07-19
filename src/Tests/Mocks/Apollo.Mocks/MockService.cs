using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Apollo.Common.Abstractions;

namespace Apollo.Mocks
{
	public class MockService
	{
		public int MessageCounter;

		public Dictionary<ApolloQueue, ConcurrentQueue<IMessage>> NormalQueues = new Dictionary<ApolloQueue, ConcurrentQueue<IMessage>>
		{
			{ ApolloQueue.Aliases, new ConcurrentQueue<IMessage>() },
			{ ApolloQueue.Registrations, new ConcurrentQueue<IMessage>() },
			{ ApolloQueue.ServerRequests, new ConcurrentQueue<IMessage>() }
		};

		public Dictionary<ApolloQueue, ConcurrentDictionary<string, ConcurrentQueue<IMessage>>> SessionQueues = new Dictionary<ApolloQueue, ConcurrentDictionary<string, ConcurrentQueue<IMessage>>>()
		{
			{ ApolloQueue.ClientSessions, new ConcurrentDictionary<string, ConcurrentQueue<IMessage>>(StringComparer.OrdinalIgnoreCase) }
		};

		private int _pendingMessages;

		public ConcurrentQueue<IMessage> GetQueue(ApolloQueue queueType, string targetSession)
		{
			return !NormalQueues.TryGetValue(queueType, out var queue) 
				? GetSessionQueue(queueType, targetSession) 
				: queue;
		}

		private ConcurrentQueue<IMessage> GetSessionQueue(ApolloQueue queueType, string identifier)
		{
			return SessionQueues.TryGetValue(queueType, out var queue) 
				? queue.GetOrAdd(identifier, new ConcurrentQueue<IMessage>()) 
				: null;
		}

		public int PendingMessages
		{
			get => _pendingMessages;
			private set => _pendingMessages = value;
		}

		public void Enqueue(IMessage message, ApolloQueue queueType, string session)
		{
			GetQueue(queueType, session).Enqueue(message);
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

		public ManualResetEventSlim QueuesEmpty { get; } = new ManualResetEventSlim();
	}
}
