using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Apollo.ServiceBus.Mocks
{

	public class MockMessageReceiver : IMessageReceiver
	{
		public ConcurrentQueue<MockMessage> _queue;

		public MockMessageReceiver(ConcurrentQueue<MockMessage> queue)
		{
			_queue = queue;
		}

		public long LastPeekedSequenceNumber => throw new NotImplementedException();

		public int PrefetchCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public ReceiveMode ReceiveMode => throw new NotImplementedException();

		public string ClientId => throw new NotImplementedException();

		public bool IsClosedOrClosing => throw new NotImplementedException();

		public string Path => throw new NotImplementedException();

		public TimeSpan OperationTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public ServiceBusConnection ServiceBusConnection => throw new NotImplementedException();

		public IList<ServiceBusPlugin> RegisteredPlugins => throw new NotImplementedException();

		public Task AbandonAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
		{
			throw new NotImplementedException();
		}

		public Task CloseAsync()
		{
			return Task.Delay(1);
		}

		public Task CompleteAsync(IEnumerable<string> lockTokens)
		{
			throw new NotImplementedException();
		}

		public Task CompleteAsync(string lockToken)
		{
			throw new NotImplementedException();
		}

		public Task DeadLetterAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
		{
			throw new NotImplementedException();
		}

		public Task DeadLetterAsync(string lockToken, string deadLetterReason, string deadLetterErrorDescription = null)
		{
			throw new NotImplementedException();
		}

		public Task DeferAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
		{
			throw new NotImplementedException();
		}

		public Task<Message> PeekAsync()
		{
			throw new NotImplementedException();
		}

		public Task<IList<Message>> PeekAsync(int maxMessageCount)
		{
			throw new NotImplementedException();
		}

		public Task<Message> PeekBySequenceNumberAsync(long fromSequenceNumber)
		{
			throw new NotImplementedException();
		}

		public Task<IList<Message>> PeekBySequenceNumberAsync(long fromSequenceNumber, int messageCount)
		{
			throw new NotImplementedException();
		}

		public async Task<Message> ReceiveAsync()
		{
			return (await ReceiveAsync(1, TimeSpan.FromSeconds(5)))?.FirstOrDefault();
		}

		public async Task<Message> ReceiveAsync(TimeSpan operationTimeout)
		{
			return (await ReceiveAsync(1, operationTimeout))?.FirstOrDefault();
		}

		public Task<IList<Message>> ReceiveAsync(int maxMessageCount)
		{
			return ReceiveAsync(maxMessageCount, TimeSpan.FromSeconds(5));
		}

		public Task<IList<Message>> ReceiveAsync(int maxMessageCount, TimeSpan operationTimeout)
		{
			return Task.Run(() =>
			{
				var expiry = DateTimeOffset.UtcNow.Add(operationTimeout);
				var retVal = new List<Message>();
				while (retVal.Count < maxMessageCount && DateTimeOffset.UtcNow < expiry)
				{
					SpinWait.SpinUntil(() => !_queue.IsEmpty || DateTimeOffset.UtcNow >= expiry);
					if (_queue.TryDequeue(out var msg))
						retVal.Add(msg.Message);
				}
				return retVal.Any()
					? (IList<Message>)retVal
					: null;
			});
		}

		public Task<Message> ReceiveDeferredMessageAsync(long sequenceNumber)
		{
			throw new NotImplementedException();
		}

		public Task<IList<Message>> ReceiveDeferredMessageAsync(IEnumerable<long> sequenceNumbers)
		{
			throw new NotImplementedException();
		}

		public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, Func<ExceptionReceivedEventArgs, Task> exceptionReceivedHandler)
		{
			throw new NotImplementedException();
		}

		public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, MessageHandlerOptions messageHandlerOptions)
		{
			throw new NotImplementedException();
		}

		public void RegisterPlugin(ServiceBusPlugin serviceBusPlugin)
		{
			throw new NotImplementedException();
		}

		public Task RenewLockAsync(Message message)
		{
			throw new NotImplementedException();
		}

		public Task<DateTime> RenewLockAsync(string lockToken)
		{
			throw new NotImplementedException();
		}

		public void UnregisterPlugin(string serviceBusPluginName)
		{
			throw new NotImplementedException();
		}
	}
}
