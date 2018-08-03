using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;

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

		/// <summary>
		/// Fired if we lose ownership of an alias
		/// </summary>
	    public event Action<string> LostOwnershipOfAlias;

	    private MessageStatus LostOwnershipNotificationReceived(ApolloQueue queue, IMessage m, CancellationToken? cancelToken)
	    {
		    var alias = m.GetStringProperty(DesiredAliasKey);
		    if (string.IsNullOrWhiteSpace(alias)) 
			    return MessageStatus.Complete;
		    Logger.Warn($"Lost ownership of alias '{alias}'");
		    LostOwnershipOfAlias?.Invoke(alias);
		    return MessageStatus.Complete;
	    }

	    /// <summary>
		/// Registers with the server so that it knows we exist
		/// </summary>
		/// <param name="metadata">Information about this client</param>
		/// <param name="cancellationToken">A token which can be used to cancel waiting</param>
		/// <returns>An awaitable task which will return the client's ID</returns>
		/// <exception cref="TimeoutException">If there is no reply</exception>
		/// <exception cref="NakException">If the server says no</exception>
		public async Task<string> RegisterAsync(IDictionary<string, string> metadata = null, CancellationToken? cancellationToken = null)
	    {
		    var registrationMessage = Communicator.MessageFactory.CreateNewMessage(ApolloConstants.RegistrationKey);
		    if (metadata != null)
		    {
			    foreach (var entry in metadata)
				    registrationMessage[entry.Key] = entry.Value;
		    }

		    await Communicator.SendToRegistrationsAsync(registrationMessage);
		    var response = await Communicator.WaitForSingleReplyAsync(registrationMessage, cancellationToken) ?? throw new TimeoutException("Timed out waiting for confirmation of registration");
		    response.ThrowIfNegativeAcknowledgement();
		    Logger.Info($"Received confirmation of registration as {response.TargetSession}");
			Communicator.SignalPluginEvent(RegisteredEventName, response.TargetSession);
		    return response.TargetSession;
	    }

		/// <summary>
		/// Asks nicely for ownership of an alias
		/// Ownership will only be granted if it is not owned or the token
		/// matches the one on the server
		/// </summary>
		/// <param name="alias">The alias to request ownership of</param>
		/// <param name="token">The token that should be used to prove ownership</param>
		/// <exception cref="TimeoutException">Thrown if no response is received</exception>
		/// <returns>A guid representing the secret for this alias or a blank guid if the request is denied</returns>
	    public async Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    Logger.Info($"Requesting ownership of alias '{alias}'");
		    var message = MessageFactory.CreateNewMessage(RequestOwnershipLabel);
		    message.Properties[DesiredAliasKey] = alias;
		    message.Properties[AliasTokenKey] = token.ToString();
		    message.TimeToLive = TimeSpan.FromSeconds(30);
		    await Communicator.SendToRegistrationsAsync(message);
		    var response = await Communicator.WaitForSingleReplyAsync(message) ?? throw new TimeoutException("Timed out waiting for response");
			
		    if (response.IsPositiveAcknowledgement() && !string.IsNullOrWhiteSpace(response.GetStringProperty(AliasTokenKey)))
		    {
			    Logger.Warn($"Successfully got ownership of {alias}");
			    var responseToken = Guid.Parse(response.GetStringProperty(AliasTokenKey));
			    Communicator.State[alias] = responseToken;
			    return responseToken;
				
		    }
		    Logger.Warn($"Failed to get ownership of {alias} ({response.GetReasonOrPlaceholder()})");
		    return Guid.Empty;
	    }

		/// <summary>
		/// Forcibly takes ownership of an alias
		/// </summary>
		/// <param name="alias">The alias to demand ownership of</param>
		/// <param name="token">The token which should be used to prove ownership in future requests</param>
		/// <exception cref="TimeoutException">Thrown if no response is received</exception>
		/// <returns><see cref="Guid.Empty"/> if the demand is denied, otherwise the ownership token for the alias</returns>
	    public async Task<Guid> DemandOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    Logger.Info($"Taking ownership of alias '{alias}'");
		    var message = MessageFactory.CreateNewMessage(DemandAliasLabel);
		    message.Properties[DesiredAliasKey] = alias;
		    message.Properties[AliasTokenKey] = token.ToString();
		    message.TimeToLive = TimeSpan.FromSeconds(30);
		    await Communicator.SendToRegistrationsAsync(message);
		    var response = await Communicator.WaitForSingleReplyAsync(message) ?? throw new TimeoutException("Timed out waiting for response");
		    if (response.IsPositiveAcknowledgement() && !string.IsNullOrWhiteSpace(response.GetStringProperty(AliasTokenKey)))
		    {
			    Logger.Warn($"Successfully took ownership of {alias}");
			    var responseToken = Guid.Parse(response.GetStringProperty(AliasTokenKey));
			    Communicator.State[alias] = responseToken;
			    return responseToken;
		    }
		    Logger.Warn($"Failed to take ownership of {alias} ({response.GetReasonOrPlaceholder()})");
		    return Guid.Empty;
	    }
    }
}
