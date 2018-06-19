using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Plugins
{
    public class ClientCorePlugin : TritonPluginBase
    {
		private const string DesiredAliasKey = "Desired Alias";
		private const string AliasTokenKey = "Alias Token";
		private const string RegisteredEventName = "Registered";
		private const string RequestAliasKey = "Request Alias Ownership";

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();

			// Or we won't get our replies
			Communicator.ClientSessionMessageReceived += HandlePing;
		}

		private void HandlePing(IMessage message, ref MessageReceivedEventArgs e)
		{
			if (message.Label != "Ping")
				return;
			var receivedTime = DateTime.UtcNow;
			var response = MessageFactory.CreateReply(message);
			response.Label = "Ping Response";
			response[nameof(PingStats.TimeRequestEnqueuedUtc)] = message.EnqueuedTimeUtc;
			response[nameof(PingStats.TimeRequestReceivedUtc)] = receivedTime;
			response[nameof(PingStats.TimeResponseSentUtc)] = DateTime.UtcNow;
			Communicator.SendToClientAsync(response);
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

	    public Task<PingStats> PingServer(TimeSpan? timeOut = null, CancellationToken? cancellationToken = null)
	    {
			return SendPingMessage(timeOut, cancellationToken, (m) => Communicator.SendToServerAsync(m));
		}

	    public Task<PingStats> PingClient(string identifier, TimeSpan? timeOut = null, CancellationToken? cancellationToken = null)
		{
			return SendPingMessage(timeOut, cancellationToken, async (m) =>
			{
				m.TargetSession = identifier;
				await Communicator.SendToClientAsync(m);
			});
		}

		public Task<PingStats> PingAlias(string alias, TimeSpan? timeOut = null, CancellationToken? cancellationToken = null)
		{
			return SendPingMessage(timeOut, cancellationToken, (m) => Communicator.SendToAliasAsync(alias, m));
		}

		private async Task<PingStats> SendPingMessage(TimeSpan? timeOut, CancellationToken? cancellationToken, Func<IMessage, Task> sendMessage)
		{
			var retVal = new PingStats();
			var message = MessageFactory.CreateNewMessage("Ping");
			var startTime = DateTime.UtcNow;
			message.TimeToLive = timeOut ?? TimeSpan.FromSeconds(30);
			await sendMessage(message);
			var response = await Communicator.WaitForReplyTo(message, cancellationToken);
			if (response.Label != "Ping Response")
			{
				Logger.Warn("Target did not respond to ping within 30 seconds");
				return null;
			}
			retVal.RoundTripTime = DateTime.UtcNow - startTime;
			retVal.TimeResponseEnqueuedUtc = response.EnqueuedTimeUtc;
			retVal.TimeRequestEnqueuedUtc = (DateTime)(response[nameof(PingStats.TimeRequestEnqueuedUtc)] ?? DateTime.MinValue);
			retVal.TimeRequestReceivedUtc = (DateTime)(response[nameof(PingStats.TimeRequestReceivedUtc)] ?? DateTime.MinValue);
			retVal.TimeResponseSentUtc = (DateTime)(response[nameof(PingStats.TimeResponseSentUtc)] ?? DateTime.MinValue);
			return retVal;

		}
	}
}
