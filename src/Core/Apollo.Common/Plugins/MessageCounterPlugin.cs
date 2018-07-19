using System;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Plugins
{
    public class MessageCounterPlugin : ApolloPluginBase
    {
		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AnyMessageReceived += (m, queue) =>
			{
				switch (queue)
				{
					case ApolloQueue.Registrations:
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesReceivedFromRegistrations, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ServerRequests:
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesReceivedFromServerRequests, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.Aliases:
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesReceivedFromAliases, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ClientSessions:
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesReceivedFromClientSessions, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
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
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesSentToRegistrations, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ServerRequests:
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesSentToServerRequests, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.Aliases:
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesSentToAliases, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					case ApolloQueue.ClientSessions:
						Communicator.State.AddOrUpdate(ApolloConstants.NumberOfMessagesSentToClientSessions, 1ul, (key, existingValue) => (ulong) existingValue + 1ul);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
				}
			};
		}
	}
}
