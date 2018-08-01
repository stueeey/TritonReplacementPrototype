using System.Threading.Tasks;
using Apollo.Common;
using Apollo.Common.Abstractions;

namespace Apollo.ServerWorker.Plugins
{
	public class EchoListenerPlugin : ApolloPlugin
	{
		private const string EchoKey = "Echo";

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AddHandler(ApolloQueue.ClientSessions, new MessageHandler(this, EchoKey, (q, m, token) =>
			{
				var reply = MessageFactory.CreateReply(m);
				reply.Label = m.Label;
				reply.Properties[EchoKey] = m.Properties[EchoKey];
				Logger.Info($"Echoing {reply.Properties[EchoKey]}");
				Communicator.SendToClientsAsync(reply);
				return MessageStatus.Complete;
			}));
		}
	}
}