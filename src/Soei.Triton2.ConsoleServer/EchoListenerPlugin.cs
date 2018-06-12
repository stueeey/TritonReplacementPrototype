using System;
using System.Threading.Tasks;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.ConsoleServer
{
	public class EchoListenerPlugin : TritonPluginBase
	{
		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.ServerJobReceived += OnServerJobReceived;
		}

		#region Overrides of TritonPluginBase

		public override void OnUninitialized()
		{
			base.OnUninitialized();
			Communicator.ServerJobReceived -= OnServerJobReceived;
		}

		#endregion

		private void OnServerJobReceived(IMessage m, ref MessageReceivedEventArgs e)
		{
			if (m.Label != "Echo") return;
			var reply = MessageFactory.CreateReply(m);
			reply.Label = m.Label;
			reply.Properties["Echo"] = m.Properties["Echo"];
			Console.WriteLine($"Echoing {reply.Properties["Echo"]}");
			Communicator.SendToClientsAsync(reply);
			e.Status = MessageStatus.Complete;
		}
	}
}