using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Plugins
{
    public class ClientCorePlugin : CorePlugin
    {
		private const string DesiredAliasKey = "Desired Alias";
		private const string AliasTokenKey = "Alias Token";

		private const string RegisteredEventName = "Registered";

		private const string RequestOwnershipLabel = "Request Alias Ownership";
	    private const string DemandAliasLabel = "Demand Alias Ownership";
	    private const string LostOwnershipLabel = "Lost Alias Ownership";

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AddHandler(ApolloQueue.ClientSessions, PingHandler);
			Communicator.AddHandler(ApolloQueue.ClientSessions, new MessageHandler(this, LostOwnershipLabel, LostOwnershipNotificationReceived));
		}

	    public event Action<string> LostOwnershipOfAlias;

	    private MessageStatus LostOwnershipNotificationReceived(ApolloQueue queue, IMessage m, CancellationToken? cancelToken)
	    {
		    var alias = m.GetStringProperty(DesiredAliasKey);
		    if (!string.IsNullOrWhiteSpace(alias))
		    {
			    Logger.Warn($"Lost ownership of alias '{alias}'");
			    LostOwnershipOfAlias?.Invoke(alias);
		    }
		    return MessageStatus.Complete;
	    }

		public async Task<string> RegisterAsync(IDictionary<string, string> metadata = null, TimeSpan? timeOut = null)
	    {
		    var registrationMessage = Communicator.MessageFactory.CreateNewMessage(ApolloConstants.RegistrationKey);
		    if (metadata != null)
		    {
			    foreach (var entry in metadata)
				    registrationMessage[entry.Key] = entry.Value;
		    }

		    if (timeOut.HasValue)
				registrationMessage.TimeToLive = timeOut.Value;
		    await Communicator.SendRegistrationMessageAsync(registrationMessage);
		    var response = await Communicator.WaitForSingleReplyAsync(registrationMessage);
		    if (response?.Label != ApolloConstants.PositiveAcknowledgement)
			    return string.Empty;
		    Logger.Info($"Received confirmation of registration as {response.TargetSession}");
			Communicator.SignalPluginEvent(RegisteredEventName, response.TargetSession);
		    return response.TargetSession;
	    }

	    public async Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    Logger.Info($"Requesting ownership of alias '{alias}'");
		    var message = MessageFactory.CreateNewMessage(RequestOwnershipLabel);
		    message.Properties[DesiredAliasKey] = alias;
		    message.Properties[AliasTokenKey] = token.ToString();
		    message.TimeToLive = TimeSpan.FromSeconds(30);
		    await Communicator.SendRegistrationMessageAsync(message);
		    var response = await Communicator.WaitForSingleReplyAsync(message);
		    if (response?.Label != ApolloConstants.PositiveAcknowledgement || !response.Properties.ContainsKey(AliasTokenKey))
		    {
				Logger.Warn($"Failed to get ownership of {alias} ({response?["Reason"] ?? "No response"})");
			    return Guid.Empty;
		    }
		    Logger.Warn($"Successfully got ownership of {alias}");
		    var responseToken = Guid.Parse(response.GetStringProperty(AliasTokenKey));
		    Communicator.State[alias] = responseToken;
		    return responseToken;
	    }


	    public async Task<Guid> DemandOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    Logger.Info($"Taking ownership of alias '{alias}'");
		    var message = MessageFactory.CreateNewMessage(DemandAliasLabel);
		    message.Properties[DesiredAliasKey] = alias;
		    message.Properties[AliasTokenKey] = token.ToString();
		    message.TimeToLive = TimeSpan.FromSeconds(30);
		    await Communicator.SendRegistrationMessageAsync(message);
		    var response = await Communicator.WaitForSingleReplyAsync(message);
		    if (response?.Label != ApolloConstants.PositiveAcknowledgement || !response.Properties.ContainsKey(AliasTokenKey))
		    {
			    Logger.Warn($"Failed to take ownership of {alias} ({response?["Reason"] ?? "No response"})");
			    return Guid.Empty;
		    }
		    Logger.Warn($"Successfully took ownership of {alias}");
		    var responseToken = Guid.Parse(response.GetStringProperty(AliasTokenKey));
		    Communicator.State[alias] = responseToken;
		    return responseToken;
	    }
    }
}
