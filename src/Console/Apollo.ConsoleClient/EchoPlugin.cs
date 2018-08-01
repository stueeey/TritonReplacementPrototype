using System.Threading.Tasks;
using Apollo.Common;
using Apollo.Common.Abstractions;

namespace Apollo.ConsoleClient
{
	public class EchoPlugin : ApolloPlugin
	{
		private const string EchoKey = "Echo";

		public async Task Echo(string echoString)
		{
			Logger.Info("Sending echo");
			var message = MessageFactory.CreateNewMessage();
			message.Label = EchoKey;
			message.Properties[EchoKey] = echoString;
			await Communicator.SendToServerAsync(message);
		}

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AddHandler(ApolloQueue.ClientSessions, new MessageHandler(this, EchoKey, (q, m, token) =>
			{
				Logger.Info($"Server echo'd {m.Properties[EchoKey]}");
				return MessageStatus.Complete;
			}));
		}
	}
}