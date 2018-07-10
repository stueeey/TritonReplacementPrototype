using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Apollo.ServiceBus.Mocks
{
	public class MockMessageSender : IMessageSender
	{
		public ConcurrentQueue<MockMessage> _queue;

		public MockMessageSender(ConcurrentQueue<MockMessage> queue)
		{
			_queue = queue;
		}

		public string ClientId => throw new NotImplementedException();

		public bool IsClosedOrClosing => throw new NotImplementedException();

		public string Path => throw new NotImplementedException();

		public TimeSpan OperationTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public ServiceBusConnection ServiceBusConnection => throw new NotImplementedException();

		public IList<ServiceBusPlugin> RegisteredPlugins => throw new NotImplementedException();

		public Task CancelScheduledMessageAsync(long sequenceNumber)
		{
			throw new NotImplementedException();
		}

		public async Task CloseAsync()
		{
			await Task.Delay(1);
		}

		public void RegisterPlugin(ServiceBusPlugin serviceBusPlugin)
		{
			throw new NotImplementedException();
		}

		public Task<long> ScheduleMessageAsync(Message message, DateTimeOffset scheduleEnqueueTimeUtc)
		{
			throw new NotImplementedException();
		}

		public Task SendAsync(Message message)
		{
			return Task.Run(() => _queue.Enqueue(new MockMessage { Message = message }));
		}

		public Task SendAsync(IList<Message> messageList)
		{
			return Task.Run(() =>
			{
				foreach (var message in messageList)
					_queue.Enqueue(new MockMessage { Message = message });
			});
		}

		public void UnregisterPlugin(string serviceBusPluginName)
		{
			throw new NotImplementedException();
		}
	}
}
