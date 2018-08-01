using Apollo.Common.Abstractions;
using Apollo.ServiceBus.Communication;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Threading.Tasks;

namespace Apollo.ServiceBus.Mocks
{
	public class MockServiceBusImplementations : IServiceBusImplementations
	{
		private readonly string _clientId;
		private readonly MockServiceBusQueues _service;
		public MockServiceBusImplementations(string clientId, MockServiceBusQueues service)
		{
			_clientId = clientId;
			_service = service;
			Recreate().Wait();
		}

		public Lazy<IMessageReceiver> RegistrationListener { get; private set;} 

		public Lazy<IMessageSender> RegistrationSender { get; private set;} 

		public Lazy<IMessageReceiver> ServerQueueListener { get; private set;} 

		public Lazy<IMessageSender> ServerQueueSender { get; private set;} 

		public Lazy<ISessionClient> ClientSessionListener { get; private set;} 

		public Lazy<IMessageSender> ClientSessionSender { get; private set;} 

		public Lazy<IMessageReceiver> AliasQueueListener { get; private set;} 

		public Lazy<IMessageSender> AliasQueueSender { get; private set;} 

		public Task Recreate()
		{
			return Task.Run(() =>
			{
				RegistrationListener = new Lazy<IMessageReceiver>(() => new MockMessageReceiver(_service.NormalQueues[ApolloQueue.Registrations]));
				ServerQueueListener = new Lazy<IMessageReceiver>(() => new MockMessageReceiver(_service.NormalQueues[ApolloQueue.ServerRequests]));
				ClientSessionListener = new Lazy<ISessionClient>(() => new MockSessionClient(_service.GetSessionQueue(ApolloQueue.ClientSessions, _clientId), _clientId));
				AliasQueueListener = new Lazy<IMessageReceiver>(() => new MockMessageReceiver(_service.NormalQueues[ApolloQueue.Aliases]));

				RegistrationSender = new Lazy<IMessageSender>(() => new MockMessageSender(_service, ApolloQueue.Registrations));
				ServerQueueSender = new Lazy<IMessageSender>(() => new MockMessageSender(_service, ApolloQueue.ServerRequests));
				ClientSessionSender = new Lazy<IMessageSender>(() => new MockMessageSender(_service, ApolloQueue.ClientSessions));
				AliasQueueSender = new Lazy<IMessageSender>(() => new MockMessageSender(_service, ApolloQueue.Aliases));
			});
			
		} 
	}
}
