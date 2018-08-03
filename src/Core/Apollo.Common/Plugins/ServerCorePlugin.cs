using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;

namespace Apollo.Common.Plugins
{

	public class ServerCorePlugin : CorePlugin
    {
		private const string DesiredAliasKey = "Desired Alias";
		private const string AliasTokenKey = "Alias Token";

		private const string RequestOwnershipLabel = "Request Alias Ownership";
	    private const string TakeAliasLabel = "Demand Alias Ownership";
		private readonly IApolloServerRepository _storage;
	    public ServerCorePlugin(IApolloServerRepository storage)
	    {
			_storage = storage;
	    }

	    protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AddHandler(ApolloQueue.Registrations, new MessageHandler(this, ApolloConstants.RegistrationKey, OnRegistrationReceived));
			Communicator.AddHandler(ApolloQueue.Registrations, new MessageHandler(this, RequestOwnershipLabel, AliasOwnershipRequestReceived));
			Communicator.AddHandler(ApolloQueue.Registrations, new MessageHandler(this, TakeAliasLabel, AliasOwnershipClaimReceived, OnClaimOwnershipError));
			Communicator.AddHandler(ApolloQueue.Aliases, new MessageHandler(this, ForwardAliasMessage));
			Communicator.AddHandler(ApolloQueue.ServerRequests, PingHandler);
			Communicator.AddHandler(ApolloQueue.ClientSessions, PingHandler);
		}

	    private MessageStatus ForwardAliasMessage(ApolloQueue queue, IMessage m, CancellationToken? cancelToken)
	    {
		    var targetAlias = m.GetStringProperty(ApolloConstants.TargetAliasKey);
		    var owner = _storage.GetAliasOwner(targetAlias);
		    if (owner == null)
			    Communicator.SendToClientsAsync(MessageFactory.CreateNegativeAcknowledgment(m, $"Alias '{targetAlias ?? "<Alias not specified>"}' is not owned or invalid"));
		    else
		    {
			    var forwardedMessage = MessageFactory.CloneMessage(m);
			    forwardedMessage.TargetSession = owner;
			    Communicator.SendToClientsAsync(forwardedMessage);
		    }
			return MessageStatus.Complete;
	    }

	    private MessageStatus OnRegistrationReceived(ApolloQueue queue, IMessage m, CancellationToken? cancelToken)
	    {
		    Logger.Info($"Received registration request from {m.Identifier}");
			if (_storage.SaveRegistration(m.ReplyToSession, m.Properties.ToDictionary(p => p.Key, p => p.Value.ToString())))
				Communicator.SendToClientsAsync(Communicator.MessageFactory.CreateAcknowledgment(m));
			return MessageStatus.Complete;
		}

	    private MessageStatus AliasOwnershipRequestReceived(ApolloQueue queue, IMessage m, CancellationToken? cancelToken)
	    {
			if (_storage.CheckOwnership(m.GetStringProperty(DesiredAliasKey), Guid.Parse(m.GetStringProperty(AliasTokenKey)), m.ReplyToSession))
			{
				var reply = MessageFactory.CreateAcknowledgment(m);
				reply.CopyPropertiesFrom(m);
				Communicator.SendToClientsAsync(reply);
				Logger.Info($"{m.Identifier} granted ownership of alias '{m.GetStringProperty(DesiredAliasKey)}'");
			}
			else
			{
				Logger.Info($"{m.Identifier} denied ownership of alias '{m.GetStringProperty(DesiredAliasKey)}'");
				Communicator.SendToClientsAsync(MessageFactory.CreateNegativeAcknowledgment(m, $"Token did not match the one registered for {m.GetStringProperty(DesiredAliasKey)}"));
			}
			return MessageStatus.Complete;
		}

		private MessageStatus AliasOwnershipClaimReceived(ApolloQueue queue, IMessage m, CancellationToken? cancelToken)
	    {
			var oldOwner = _storage.TakeOwnership(m.GetStringProperty(DesiredAliasKey), Guid.Parse(m.GetStringProperty(AliasTokenKey)), m.ReplyToSession);
			if (oldOwner != null)
			{
				var lostOwnershipMessage = MessageFactory.CreateNewMessage("Lost Alias Ownership");
				lostOwnershipMessage.TargetSession = oldOwner;
				lostOwnershipMessage[DesiredAliasKey] = m.GetStringProperty(DesiredAliasKey);
				Communicator.SendToClientsAsync(lostOwnershipMessage);
			}
			var reply = MessageFactory.CreateAcknowledgment(m);
			reply.CopyPropertiesFrom(m);
			Communicator.SendToClientsAsync(reply);
			return MessageStatus.Complete;
	    }

		private void OnClaimOwnershipError(IMessage m, Exception ex)
		{
			Logger.Error($"Failed to grant ownership of {m.GetStringProperty(DesiredAliasKey)}", ex);
			Communicator.SendToClientsAsync(MessageFactory.CreateNegativeAcknowledgment(m, "Encountered an error processing the request"));
		}
	}
}
