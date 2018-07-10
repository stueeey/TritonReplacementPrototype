using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Apollo.ServiceBus.Mocks
{
	public class MockSessionClient : ISessionClient
	{
		public ConcurrentQueue<MockMessage> Queue;

		public MockSessionClient(ConcurrentQueue<MockMessage> queue, string clientId)
		{
			Queue = queue;
			ClientId = clientId;
		}

		public string EntityPath => throw new NotImplementedException();

		public string ClientId { get; }

		public bool IsClosedOrClosing => throw new NotImplementedException();

		public string Path => throw new NotImplementedException();

		public TimeSpan OperationTimeout { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public ServiceBusConnection ServiceBusConnection => throw new NotImplementedException();

		public IList<ServiceBusPlugin> RegisteredPlugins => throw new NotImplementedException();

		public Task<IMessageSession> AcceptMessageSessionAsync()
		{
			throw new NotImplementedException();
		}

		public Task<IMessageSession> AcceptMessageSessionAsync(TimeSpan serverWaitTime)
		{
			throw new NotImplementedException();
		}

		public Task<IMessageSession> AcceptMessageSessionAsync(string sessionId)
		{
			throw new NotImplementedException();
		}

		public Task<IMessageSession> AcceptMessageSessionAsync(string sessionId, TimeSpan serverWaitTime)
		{
			throw new NotImplementedException();
		}

		public Task CloseAsync()
		{
			throw new NotImplementedException();
		}

		public void RegisterPlugin(ServiceBusPlugin serviceBusPlugin)
		{
			throw new NotImplementedException();
		}

		public void UnregisterPlugin(string serviceBusPluginName)
		{
			throw new NotImplementedException();
		}
	}
}
