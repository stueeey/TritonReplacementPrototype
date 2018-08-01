using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Apollo.ServiceBus.Mocks
{
	public class MockMessageSession : IMessageSession
	{
		public ConcurrentQueue<MockMessage> Queue;

		public MockMessageSession(ConcurrentQueue<MockMessage> queue)
		{
			Queue = queue;
		}

		#region Implementation of IClientEntity

		public Task CloseAsync()
		{
			return Task.Delay(1);
		}

		public void RegisterPlugin(ServiceBusPlugin serviceBusPlugin)
		{
			throw new NotImplementedException();
		}

		public void UnregisterPlugin(string serviceBusPluginName)
		{
			throw new NotImplementedException();
		}

		public string ClientId { get; }
		public bool IsClosedOrClosing { get; }
		public string Path { get; }
		public TimeSpan OperationTimeout { get; set; }
		public ServiceBusConnection ServiceBusConnection { get; }
		public IList<ServiceBusPlugin> RegisteredPlugins { get; }

		#endregion

		#region Implementation of IReceiverClient

		public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, Func<ExceptionReceivedEventArgs, Task> exceptionReceivedHandler)
		{
			throw new NotImplementedException();
		}

		public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, MessageHandlerOptions messageHandlerOptions)
		{
			throw new NotImplementedException();
		}

		public Task CompleteAsync(string lockToken)
		{
			throw new NotImplementedException();
		}

		public Task AbandonAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
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

		public int PrefetchCount { get; set; }
		public ReceiveMode ReceiveMode { get; }

		#endregion

		#region Implementation of IMessageReceiver

		public Task<Message> ReceiveAsync()
		{
			throw new NotImplementedException();
		}

		public Task<Message> ReceiveAsync(TimeSpan operationTimeout)
		{
			throw new NotImplementedException();
		}

		public Task<IList<Message>> ReceiveAsync(int maxMessageCount)
		{
			throw new NotImplementedException();
		}

		public Task<IList<Message>> ReceiveAsync(int maxMessageCount, TimeSpan operationTimeout)
		{
			throw new NotImplementedException();
		}

		public Task<Message> ReceiveDeferredMessageAsync(long sequenceNumber)
		{
			throw new NotImplementedException();
		}

		public Task<IList<Message>> ReceiveDeferredMessageAsync(IEnumerable<long> sequenceNumbers)
		{
			throw new NotImplementedException();
		}

		public Task CompleteAsync(IEnumerable<string> lockTokens)
		{
			throw new NotImplementedException();
		}

		public Task DeferAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
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

		public long LastPeekedSequenceNumber { get; }

		#endregion

		#region Implementation of IMessageSession

		public Task<byte[]> GetStateAsync()
		{
			throw new NotImplementedException();
		}

		public Task SetStateAsync(byte[] sessionState)
		{
			throw new NotImplementedException();
		}

		public Task RenewSessionLockAsync()
		{
			throw new NotImplementedException();
		}

		public string SessionId { get; }
		public DateTime LockedUntilUtc { get; }

		#endregion
	}
}
