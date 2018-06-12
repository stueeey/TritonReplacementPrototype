using System;
using System.Threading.Tasks;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.ConsoleClient
{
	public class EchoPlugin : TritonPluginBase
	{
		public async Task Echo(string echoString)
		{
			Console.WriteLine("Sending echo");
			var message = MessageFactory.CreateNewMessage();
			message.Label = "Echo";
			message.Properties["Echo"] = echoString;
			await Communicator.SendToServerAsync(message);
		}

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.ClientSessionMessageReceived += OnEchoRecieved;
		}

		public override void OnUninitialized()
		{
			base.OnUninitialized();
			Communicator.ClientSessionMessageReceived -= OnEchoRecieved;
		}

		private void OnEchoRecieved(IMessage message, ref MessageReceivedEventArgs e)
		{
			if (message.Label != "Echo") 
				return;
			Console.WriteLine($"Server echo'd {message.Properties["Echo"]}");
			e.Status = MessageStatus.Complete;
		}
	}
}