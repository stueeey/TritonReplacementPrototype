using Apollo.Common.Abstractions;
using Apollo.ServiceBus.Communication;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apollo.ServiceBus.Mocks
{
	public class MockServiceBusImplementations : IServiceBusImplementations
	{
		public Dictionary<ApolloQueue, ConcurrentQueue<MockMessage>> Queues = new Dictionary<ApolloQueue, ConcurrentQueue<MockMessage>>
		{
			{ ApolloQueue.Aliases, new ConcurrentQueue<MockMessage>() },
			{ ApolloQueue.ClientSessions, new ConcurrentQueue<MockMessage>() },
			{ ApolloQueue.Registrations, new ConcurrentQueue<MockMessage>() },
			{ ApolloQueue.ServerRequests, new ConcurrentQueue<MockMessage>() }
		};

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
				RegistrationListener = new Lazy<IMessageReceiver>(() => new MockMessageReceiver(Queues[ApolloQueue.Registrations]));
				ServerQueueListener = new Lazy<IMessageReceiver>(() => new MockMessageReceiver(Queues[ApolloQueue.ServerRequests]));
				//ClientSessionListener = new Lazy<IMessageReceiver>(() => new MockMessageReceiver(Queues[ApolloQueue.ClientSessions]));
				AliasQueueListener = new Lazy<IMessageReceiver>(() => new MockMessageReceiver(Queues[ApolloQueue.Aliases]));
			});
			
		}
	}
}
