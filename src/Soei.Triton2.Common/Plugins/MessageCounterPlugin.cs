using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Plugins
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
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromRegistrations, 1, (key, existingValue) => (ulong) existingValue + 1);
						break;
					case ApolloQueue.ServerRequests:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromServerRequests, 1, (key, existingValue) => (ulong) existingValue + 1);
						break;
					case ApolloQueue.Aliases:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromAliases, 1, (key, existingValue) => (ulong) existingValue + 1);
						break;
					case ApolloQueue.ClientSessions:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesReceivedFromClientSessions, 1, (key, existingValue) => (ulong) existingValue + 1);
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
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToRegistrations, 1, (key, existingValue) => (ulong) existingValue + 1);
						break;
					case ApolloQueue.ServerRequests:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToServerRequests, 1, (key, existingValue) => (ulong) existingValue + 1);
						break;
					case ApolloQueue.Aliases:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToAliases, 1, (key, existingValue) => (ulong) existingValue + 1);
						break;
					case ApolloQueue.ClientSessions:
						Communicator.State.AddOrUpdate(TritonConstants.NumberOfMessagesSentToClientSessions, 1, (key, existingValue) => (ulong) existingValue + 1);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
				}
			};
		}
	}
}
