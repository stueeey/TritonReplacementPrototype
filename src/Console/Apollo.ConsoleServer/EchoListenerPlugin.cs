using System;
using System.Threading.Tasks;
using Soei.Apollo.Common;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;

namespace Soei.Apollo.ConsoleServer
{
	public class EchoListenerPlugin : TritonPluginBase
	{
		private const string EchoKey = "Echo";

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.ServerJobReceived += OnServerJobReceived;
		}

		private void OnServerJobReceived(IMessage m, ref MessageReceivedEventArgs e)
		{
			if (m.Label != EchoKey) 
				return;
			var reply = MessageFactory.CreateReply(m);
			reply.Label = m.Label;
			reply.Properties[EchoKey] = m.Properties[EchoKey];
			Console.WriteLine($"Echoing {reply.Properties[EchoKey]}");
			Communicator.SendToClientAsync(reply);
			e.Status = MessageStatus.Complete;
		}
	}
}