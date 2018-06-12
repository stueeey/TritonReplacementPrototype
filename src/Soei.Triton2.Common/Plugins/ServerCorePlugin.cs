using System;
using System.Threading.Tasks;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Plugins
{
    public class ServerCorePlugin : TritonPluginBase
    {
		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.RegistrationReceived += OnRegistrationReceived;
			Communicator.ServerJobReceived += ServerJobReceived;
		}

		public override void OnUninitialized()
		{
			base.OnUninitialized();
			Communicator.RegistrationReceived -= OnRegistrationReceived;
			Communicator.ServerJobReceived -= ServerJobReceived;
		}

		private void OnRegistrationReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != TritonConstants.Registration) return;
		    Console.WriteLine($"Received client message {m.Identifier} labelled {m.Label}");
		    e.Status = MessageStatus.Complete;
		    Communicator.SendToClientsAsync(Communicator.MessageFactory.CreateAcknowledgment(m));
	    }

	    private void ServerJobReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != "Request Alias Ownership") 
			    return;
		    var reply = MessageFactory.CreateAcknowledgment(m);
		    reply.CopyPropertiesFrom(m, "Desired Alias", "Alias Token");
		    Communicator.SendToClientsAsync(reply);
		    e.Status = MessageStatus.Complete;
	    }
    }
}
