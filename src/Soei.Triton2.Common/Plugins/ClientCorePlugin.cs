using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Plugins
{
    public class ClientCorePlugin : TritonPluginBase
    {
	    public class PingStats
	    {
		    public DateTime TimeRequestSentUtc { get; set; }
		    public DateTime TimeRequestEnqueuedUtc { get; set; }
		    public DateTime TimeRequestReceivedUtc { get; set; }
		    public DateTime TimeResponseSentUtc { get; set; }
		    public DateTime TimeResponseEnqueuedUtc { get; set; }
		    public DateTime TimeResponseRecievedUtc { get; set; }
		    public string TargetQueue { get; set; }
		    public string TargetSession { get; set; }

		    public TimeSpan RoundTripTime => TimeResponseRecievedUtc - TimeRequestSentUtc;
		    public TimeSpan QueuedTime => (TimeRequestReceivedUtc - TimeRequestEnqueuedUtc) + (TimeResponseRecievedUtc - TimeResponseEnqueuedUtc);
		    public TimeSpan RequestDuration => TimeRequestReceivedUtc - TimeRequestSentUtc;
		    public TimeSpan ResponseDuration => TimeResponseRecievedUtc - TimeResponseSentUtc;
	    }

		private const string DesiredAliasKey = "Desired Alias";
		private const string AliasTokenKey = "Alias Token";
		private const string RegisteredEventName = "Registered";
		private const string RequestAliasKey = "Request Alias Ownership";

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();

			// Or we won't get our replies
			Communicator.ClientSessionMessageReceived += SubscribeButDoNothing;
		}

		public async Task<string> RegisterAsync(IDictionary<string, string> metadata = null, TimeSpan? timeOut = null)
	    {
		    var registrationMessage = Communicator.MessageFactory.CreateNewMessage(TritonConstants.RegistrationKey);
		    if (metadata != null)
		    {
			    foreach (var entry in metadata)
				    registrationMessage[entry.Key] = entry.Value;
		    }

		    if (timeOut.HasValue)
				registrationMessage.TimeToLive = timeOut.Value;
		    await Communicator.SendRegistrationMessageAsync(registrationMessage);
		    var response = await Communicator.WaitForReplyTo(registrationMessage);
		    if (response == null || response.Label != TritonConstants.PositiveAcknowledgement)
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
		    if (response.Label != TritonConstants.PositiveAcknowledgement || !response.Properties.ContainsKey(AliasTokenKey))
		    {
			    Logger.Warn($"Failed to get ownership of {alias} ({response["Reason"] ?? "Unknown"})");
			    return Guid.Empty;
		    }
		    Logger.Warn($"Successfully got ownership of {alias}");
		    var responseToken = Guid.Parse(response.GetProperty(AliasTokenKey));
		    Communicator.State[alias] = responseToken;
		    return responseToken;
	    }

	    public async Task<PingStats> PingServer()
	    {
		    return new PingStats();
		    
	    }
    }
}
