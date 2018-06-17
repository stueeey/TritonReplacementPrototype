using System;
using System.Linq;
using System.Threading.Tasks;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Plugins
{
    public class ServerCorePlugin : TritonPluginBase
    {
	    private IRegistrationStorage _storage;
	    public ServerCorePlugin(IRegistrationStorage storage)
	    {
			_storage = storage;
	    }

	    protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.RegistrationReceived += OnRegistrationReceived;
			Communicator.RegistrationReceived += AliasOwnershipRequestReceived;
			Communicator.RegistrationReceived += AliasOwnershipClaimReceived;
		}

		public override void OnUninitialized()
		{
			base.OnUninitialized();
			Communicator.RegistrationReceived -= OnRegistrationReceived;
			Communicator.RegistrationReceived -= AliasOwnershipRequestReceived;
			Communicator.RegistrationReceived -= AliasOwnershipClaimReceived;
		}

		private void OnRegistrationReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != TritonConstants.Registration) 
				return;
		    Console.WriteLine($"Received client message {m.Identifier} labelled {m.Label}");
		    e.Status = MessageStatus.Complete;
			if (_storage.SaveRegistration(Guid.Parse(m.ReplyToSession), m.Properties.ToDictionary(p => p.Key, p => p.Value.ToString())))
				Communicator.SendToClientsAsync(Communicator.MessageFactory.CreateAcknowledgment(m));
	    }

	    private void AliasOwnershipRequestReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != "Request Alias Ownership") 
			    return;
		    e.Status = MessageStatus.Complete;
		    if (_storage.CheckOwnership(m.GetProperty("Desired Alias"), Guid.Parse(m.GetProperty("Alias Token")), Guid.Parse(m.ReplyToSession)))
		    {
			    var reply = MessageFactory.CreateAcknowledgment(m);
			    reply.CopyPropertiesFrom(m);
			    Communicator.SendToClientsAsync(reply);
		    }
			else
			    Communicator.SendToClientsAsync(MessageFactory.CreateNegativeAcknowledgment(m));
	    }

		private void AliasOwnershipClaimReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != "Claim Alias Ownership") 
			    return;
		    e.Status = MessageStatus.Complete;
		    if (_storage.TakeOwnership(m.GetProperty("Desired Alias"), Guid.Parse(m.GetProperty("Alias Token")), Guid.Parse(m.ReplyToSession)))
		    {
			    var reply = MessageFactory.CreateAcknowledgment(m);
			    reply.CopyPropertiesFrom(m);
			    Communicator.SendToClientsAsync(reply);
		    }
			else
			    Communicator.SendToClientsAsync(MessageFactory.CreateNegativeAcknowledgment(m));
	    }
    }
}
