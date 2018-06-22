using System;
using System.Threading.Tasks;
using Soei.Apollo.Common;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;

namespace Soei.Apollo.ConsoleClient
{
	public class EchoPlugin : TritonPluginBase
	{
		private const string EchoKey = "Echo";

		public async Task Echo(string echoString)
		{
			Console.WriteLine("Sending echo");
			var message = MessageFactory.CreateNewMessage();
			message.Label = EchoKey;
			message.Properties[EchoKey] = echoString;
			await Communicator.SendToServerAsync(message);
		}

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.ClientSessionMessageReceived += OnEchoRecieved;
		}

		private void OnEchoRecieved(IMessage message, ref MessageReceivedEventArgs e)
		{
			if (message.Label != EchoKey) 
				return;
			Console.WriteLine($"Server echo'd {message.Properties[EchoKey]}");
			e.Status = MessageStatus.Complete;
		}
	}
}