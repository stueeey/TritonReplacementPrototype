using Apollo.Common.Abstractions;
using Apollo.ServiceBus.Mocks;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Apollo.ServiceBus.Tests
{
    public class ServiceBusMockTests
    {
        [Fact]
        public async Task TestBasicSendReceive()
        {
			var implementations = new MockServiceBusImplementations("SomeDude");
			var message = new Message();
			await implementations.ServerQueueSender.Value.SendAsync(message);
			implementations.Queues[ApolloQueue.ServerRequests].Should().ContainSingle(m => m.Message == message);
	        (await implementations.ServerQueueListener.Value.ReceiveAsync()).Should().Be(message);

	        await implementations.AliasQueueSender.Value.SendAsync(message);
	        implementations.Queues[ApolloQueue.Aliases].Should().ContainSingle(m => m.Message == message);
	        (await implementations.AliasQueueListener.Value.ReceiveAsync()).Should().Be(message);

	        await implementations.RegistrationSender.Value.SendAsync(message);
	        implementations.Queues[ApolloQueue.Registrations].Should().ContainSingle(m => m.Message == message);
	        (await implementations.RegistrationListener.Value.ReceiveAsync()).Should().Be(message);
		}

	    [Fact]
	    public async Task TestSessionSendReceive()
	    {
		    var implementations = new MockServiceBusImplementations("SomeDude");
		    var message = new Message { SessionId = "xxx"};
		    await implementations.ClientSessionSender.Value.SendAsync(message);
		    implementations.Queues[ApolloQueue.ClientSessions].Should().ContainSingle(m => m.Message == message);
		    (await implementations.ServerQueueListener.Value.ReceiveAsync()).Should().Be(message);

		    await implementations.AliasQueueSender.Value.SendAsync(message);
		    implementations.Queues[ApolloQueue.Aliases].Should().ContainSingle(m => m.Message == message);
		    (await implementations.AliasQueueListener.Value.ReceiveAsync()).Should().Be(message);

		    await implementations.RegistrationSender.Value.SendAsync(message);
		    implementations.Queues[ApolloQueue.Registrations].Should().ContainSingle(m => m.Message == message);
		    (await implementations.RegistrationListener.Value.ReceiveAsync()).Should().Be(message);
	    }
	}
}
