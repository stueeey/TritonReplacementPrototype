using System;
using System.Linq;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Plugins
{

	public class ServerCorePlugin : CorePlugin
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
			Communicator.AliasMessageReceived += ForwardAliasMessage;
			Communicator.ServerJobReceived += HandlePing;
			Communicator.ClientSessionMessageReceived += HandlePing;
		}

	    private void ForwardAliasMessage(IMessage message, ref MessageReceivedEventArgs e)
	    {
		    var targetAlias = message.GetStringProperty(ApolloConstants.TargetAliasKey);
		    var owner = _storage.GetAliasOwner(targetAlias);
		    if (owner == null)
		    {
			    Communicator.SendToClientAsync(MessageFactory.CreateNegativeAcknowledgment(message, $"Alias '{targetAlias ?? "<Alias not specified>"}' is not owned or invalid"));
		    }
		    else
		    {
			    var forwardedMessage = MessageFactory.CloneMessage(message);
			    forwardedMessage.TargetSession = owner;
			    Communicator.SendToClientAsync(forwardedMessage);
		    }

	    }

	    private void OnRegistrationReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != ApolloConstants.RegistrationKey) 
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
		    if (_storage.CheckOwnership(m.GetStringProperty(DesiredAliasKey), Guid.Parse(m.GetStringProperty(AliasTokenKey)), m.ReplyToSession))
		    {
			    var reply = MessageFactory.CreateAcknowledgment(m);
			    reply.CopyPropertiesFrom(m);
			    Communicator.SendToClientAsync(reply);
		    }
			else
			    Communicator.SendToClientAsync(MessageFactory.CreateNegativeAcknowledgment(m, $"Token did not match the one registered for {m.GetStringProperty(DesiredAliasKey)}"));
	    }

		private void AliasOwnershipClaimReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != ClaimOwnershipLabel) 
			    return;
		    try
		    {
			    var oldOwner = _storage.TakeOwnership(m.GetStringProperty(DesiredAliasKey), Guid.Parse(m.GetStringProperty(AliasTokenKey)), m.ReplyToSession);
			    if (oldOwner != null)
			    {
				    var lostOwnershipMessage = MessageFactory.CreateNewMessage("Lost Alias Ownership");
				    lostOwnershipMessage.TargetSession = oldOwner.ToString();
				    lostOwnershipMessage[DesiredAliasKey] = m.GetStringProperty(DesiredAliasKey);
				    Communicator.SendToClientAsync(lostOwnershipMessage);
			    }
				var reply = MessageFactory.CreateAcknowledgment(m);
			    reply.CopyPropertiesFrom(m);
			    Communicator.SendToClientAsync(reply);
			    e.Status = MessageStatus.Complete;
		    }
		    catch (Exception ex)
		    {
			    Logger.Error($"Failed to grant ownership of {m.GetStringProperty(DesiredAliasKey)}", ex);
			    Communicator.SendToClientAsync(MessageFactory.CreateNegativeAcknowledgment(m, "Encountered an error processing the request"));
		    }
	    }
    }
}
