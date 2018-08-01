using System;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;

namespace Apollo.Common.Plugins
{
    public abstract class CorePlugin : ApolloPlugin
    {
		private const string PingLabel = "Ping";
		protected MessageHandler PingHandler;

		protected CorePlugin()
		{
			PingHandler = new MessageHandler(this, PingLabel, HandlePing);
		}

	    protected virtual MessageStatus HandlePing(ApolloQueue targetQueue, IMessage message, CancellationToken? token)
	    {
			var response = MessageFactory.CreateAcknowledgment(message);
			response[nameof(PingStats.ServedBy)] = Communicator.GetState<string>(ApolloConstants.RegisteredAsKey);
			response[nameof(PingStats.TimeRequestEnqueuedUtc)] = message.EnqueuedTimeUtc;
			Communicator.SendToClientsAsync(response);
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
			    await Communicator.SendToClientsAsync(m);
		    });
	    }

	    public Task<PingStats> PingAlias(string alias, TimeSpan? timeOut = null, CancellationToken? cancellationToken = null)
	    {
		    return SendPingMessage(timeOut, cancellationToken, m => Communicator.SendToAliasAsync(m));
	    }

	    private async Task<PingStats> SendPingMessage(TimeSpan? timeOut, CancellationToken? cancellationToken, Func<IMessage, Task> sendMessage)
	    {
		    var retVal = new PingStats();
		    try
		    {
			    var message = MessageFactory.CreateNewMessage(PingLabel);
			    var startTime = DateTime.UtcNow;
			    message.TimeToLive = timeOut ?? TimeSpan.FromSeconds(30);
			    await sendMessage(message);
			    var response = await Communicator.WaitForSingleReplyAsync(message, cancellationToken);
			    switch (response?.Label)
			    {
				    case ApolloConstants.NegativeAcknowledgement:
					    retVal.Result = PingStats.PingResult.AddresseeNotFound;
					    break;
				    case ApolloConstants.PositiveAcknowledgement:
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
