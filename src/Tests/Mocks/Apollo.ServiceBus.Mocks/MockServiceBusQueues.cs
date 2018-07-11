using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Apollo.Common.Abstractions;

namespace Apollo.ServiceBus.Mocks
{
    public class MockServiceBusQueues
    {
	    public Dictionary<ApolloQueue, ConcurrentQueue<MockMessage>> NormalQueues = new Dictionary<ApolloQueue, ConcurrentQueue<MockMessage>>
	    {
		    { ApolloQueue.Aliases, new ConcurrentQueue<MockMessage>() },
		    { ApolloQueue.Registrations, new ConcurrentQueue<MockMessage>() },
		    { ApolloQueue.ServerRequests, new ConcurrentQueue<MockMessage>() }
	    };

	    public Dictionary<ApolloQueue, ConcurrentDictionary<string, ConcurrentQueue<MockMessage>>> SessionQueues = new Dictionary<ApolloQueue, ConcurrentDictionary<string, ConcurrentQueue<MockMessage>>>()
	    {
		    { ApolloQueue.ClientSessions, new ConcurrentDictionary<string, ConcurrentQueue<MockMessage>>(StringComparer.OrdinalIgnoreCase) }
	    };

	    public ConcurrentQueue<MockMessage> GetQueue(ApolloQueue queueType, string targetSession)
	    {
		    if (NormalQueues.TryGetValue(queueType, out var queue))
		    {
				if (targetSession != null)
					throw new InvalidOperationException("Tried to send a message to a session in a queue without sessions enabled");
				return queue;
		    }
		    return GetSessionQueue(queueType, targetSession);
	    }

	    public ConcurrentQueue<MockMessage> GetSessionQueue(ApolloQueue queueType, string identifier)
	    {
		    return SessionQueues.TryGetValue(queueType, out var queue) 
			    ? queue.GetOrAdd(identifier, new ConcurrentQueue<MockMessage>()) 
			    : null;
	    }
	}
}
