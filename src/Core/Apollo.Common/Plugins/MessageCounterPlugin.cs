using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;

namespace Soei.Apollo.Common.Plugins
{
    public class MessageCounterPlugin : TritonPluginBase
    {
		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AnyMessageReceived += (m, queue) =>
			{
				switch (queue)
				{
					case ApolloQueue.Registrations:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromRegistrations, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ServerRequests:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromServerRequests, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.Aliases:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromAliases, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ClientSessions:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromClientSessions, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
				}
			};

			Communicator.AnyMessageSent += (m, queue) =>
			{
				switch (queue)
				{
					case ApolloQueue.Registrations:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToRegistrations, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ServerRequests:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToServerRequests, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.Aliases:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToAliases, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ClientSessions:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToClientSessions, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
				}
			};
		}
	}
}
