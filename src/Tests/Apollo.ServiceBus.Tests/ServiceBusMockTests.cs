using Apollo.Common.Abstractions;
using Apollo.ServiceBus.Mocks;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using Xunit;

namespace Apollo.ServiceBus.Tests
{
    public class ServiceBusMockTests
    {
        [Fact]
        public async Task TestBasicSendReceive()
        {
	        var queues = new MockServiceBusQueues();
			var implementations = new MockServiceBusImplementations("SomeDude", queues);
			var message = new Message();
			await implementations.ServerQueueSender.Value.SendAsync(message);
	        queues.NormalQueues[ApolloQueue.ServerRequests].Should().ContainSingle(m => m.Message == message);
	        (await implementations.ServerQueueListener.Value.ReceiveAsync()).Should().Be(message);

	        await implementations.AliasQueueSender.Value.SendAsync(message);
	        queues.NormalQueues[ApolloQueue.Aliases].Should().ContainSingle(m => m.Message == message);
	        (await implementations.AliasQueueListener.Value.ReceiveAsync()).Should().Be(message);

	        await implementations.RegistrationSender.Value.SendAsync(message);
	        queues.NormalQueues[ApolloQueue.Registrations].Should().ContainSingle(m => m.Message == message);
	        (await implementations.RegistrationListener.Value.ReceiveAsync()).Should().Be(message);
		}

	    [Fact(Skip = "May be removed, faking service bus is hard")]
	    public async Task TestSessionSendReceive()
	    {
		    var queues = new MockServiceBusQueues();
		    var implementations = new MockServiceBusImplementations("SomeDude", queues);
		    var message = new Message { SessionId = "xxx"};
		    await implementations.ClientSessionSender.Value.SendAsync(message);
		    queues.SessionQueues[ApolloQueue.ClientSessions]["xxx"].Should().ContainSingle(m => m.Message == message);
		    var messageSession = await implementations.ClientSessionListener.Value.AcceptMessageSessionAsync("xxx");
		    (await messageSession.ReceiveAsync()).Should().Be(message);
	    }
	}
}
