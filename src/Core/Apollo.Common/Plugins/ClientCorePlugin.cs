using System;
using System.Collections.Generic;
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
		private const string RequestAliasKey = "Request Alias Ownership";

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AddHandler(ApolloQueue.ClientSessions, PingHandler);
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
		    var response = await Communicator.WaitForReplyTo(registrationMessage);
		    if (response?.Label != ApolloConstants.PositiveAcknowledgement)
			    return string.Empty;
		    Logger.Info($"Received confirmation of registration as {response.TargetSession}");
			Communicator.SignalPluginEvent(RegisteredEventName, response.TargetSession);
		    return response.TargetSession;
	    }

	    public async Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    Logger.Info($"Requesting ownership of alias '{alias}'");
		    var message = MessageFactory.CreateNewMessage(RequestAliasKey);
		    message.Properties[DesiredAliasKey] = alias;
		    message.Properties[AliasTokenKey] = token.ToString();
		    message.TimeToLive = TimeSpan.FromSeconds(30);
		    await Communicator.SendRegistrationMessageAsync(message);
		    var response = await Communicator.WaitForReplyTo(message);
		    if (response?.Label != ApolloConstants.PositiveAcknowledgement || !response.Properties.ContainsKey(AliasTokenKey))
		    {
			    Logger.Warn($"Failed to get ownership of {alias} ({response["Reason"] ?? "Unknown"})");
			    return Guid.Empty;
		    }
		    Logger.Warn($"Successfully got ownership of {alias}");
		    var responseToken = Guid.Parse(response.GetStringProperty(AliasTokenKey));
		    Communicator.State[alias] = responseToken;
		    return responseToken;
	    }

	    
	}
}
