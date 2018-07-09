using System;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Plugins
{
    public abstract class CorePlugin : ApolloPluginBase
    {
		private const string pingResponse = "Ping Response";
		private const string ping = "Ping";
		protected MessageHandler PingHandler;

		protected CorePlugin()
		{
			PingHandler = new MessageHandler(this, ping, HandlePing);
		}

	    protected virtual MessageStatus HandlePing(ApolloQueue targetQueue, IMessage message, CancellationToken? token)
	    {
			var response = MessageFactory.CreateReply(message, pingResponse);
			response[nameof(PingStats.ServedBy)] = Communicator.GetState<string>(ApolloConstants.RegisteredAsKey);
			response[nameof(PingStats.TimeRequestEnqueuedUtc)] = message.EnqueuedTimeUtc;
			Communicator.SendToClientAsync(response);
			return MessageStatus.Complete;
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
		    try
		    {
			    var message = MessageFactory.CreateNewMessage(ping);
			    var startTime = DateTime.UtcNow;
			    message.TimeToLive = timeOut ?? TimeSpan.FromSeconds(30);
			    await sendMessage(message);
			    var response = await Communicator.WaitForReplyTo(message, cancellationToken);
			    switch (response?.Label)
			    {
				    case ApolloConstants.NegativeAcknowledgement:
					    retVal.Result = PingStats.PingResult.AddresseeNotFound;
					    break;
				    case pingResponse:
					    retVal.RoundTripTime = DateTime.UtcNow - startTime;
					    retVal.TimeResponseEnqueuedUtc = response.EnqueuedTimeUtc;
					    retVal.TimeRequestEnqueuedUtc = (DateTime) (response[nameof(PingStats.TimeRequestEnqueuedUtc)] ?? DateTime.MinValue);
					    retVal.ServedBy = response.GetStringProperty(nameof(PingStats.ServedBy));

					    break;
				    default:
						retVal.Result = PingStats.PingResult.Timeout;
					    break;
			    }
		    }
		    catch (Exception ex)
		    {
			    retVal.Result = PingStats.PingResult.Exception;
				retVal.Exception = ex;
		    }
		    return retVal;
	    }
    }
}
