using System;
using System.Linq;
using System.Threading.Tasks;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Plugins
{
    public class ServerCorePlugin : TritonPluginBase
    {
		private const string DesiredAliasKey = "Desired Alias";
		private const string AliasTokenKey = "Alias Token";
		private const string RequestOwnershipLabel = "Request Alias Ownership";
		private const string ClaimOwnershipLabel = "Claim Alias Ownership";
		private readonly IRegistrationStorage _storage;
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

		private void OnRegistrationReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != TritonConstants.RegistrationKey) 
				return;
		    Logger.Info($"Received client message {m.Identifier} labelled {m.Label}");
		    e.Status = MessageStatus.Complete;
			if (_storage.SaveRegistration(m.ReplyToSession, m.Properties.ToDictionary(p => p.Key, p => p.Value.ToString())))
				Communicator.SendToClientAsync(Communicator.MessageFactory.CreateAcknowledgment(m));
	    }

	    private void AliasOwnershipRequestReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != RequestOwnershipLabel) 
			    return;
		    e.Status = MessageStatus.Complete;
		    if (_storage.CheckOwnership(m.GetProperty(DesiredAliasKey), Guid.Parse(m.GetProperty(AliasTokenKey)), m.ReplyToSession))
		    {
			    var reply = MessageFactory.CreateAcknowledgment(m);
			    reply.CopyPropertiesFrom(m);
			    Communicator.SendToClientAsync(reply);
		    }
			else
			    Communicator.SendToClientAsync(MessageFactory.CreateNegativeAcknowledgment(m, $"Token did not match the one registered for {m.GetProperty(DesiredAliasKey)}"));
	    }

		private void AliasOwnershipClaimReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != ClaimOwnershipLabel) 
			    return;
		    try
		    {
			    var oldOwner = _storage.TakeOwnership(m.GetProperty(DesiredAliasKey), Guid.Parse(m.GetProperty(AliasTokenKey)), m.ReplyToSession);
			    if (oldOwner != null)
			    {
				    var lostOwnershipMessage = MessageFactory.CreateNewMessage("Lost Alias Ownership");
				    lostOwnershipMessage.TargetSession = oldOwner.ToString();
				    lostOwnershipMessage[DesiredAliasKey] = m.GetProperty(DesiredAliasKey);
				    Communicator.SendToClientAsync(lostOwnershipMessage);
			    }
				var reply = MessageFactory.CreateAcknowledgment(m);
			    reply.CopyPropertiesFrom(m);
			    Communicator.SendToClientAsync(reply);
			    e.Status = MessageStatus.Complete;
		    }
		    catch (Exception ex)
		    {
			    Logger.Error($"Failed to grant ownership of {m.GetProperty(DesiredAliasKey)}", ex);
			    Communicator.SendToClientAsync(MessageFactory.CreateNegativeAcknowledgment(m, "Encountered an error processing the request"));
		    }
	    }
    }
}
