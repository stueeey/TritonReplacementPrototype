using System;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Plugins
{
    public abstract class CorePlugin : ApolloPluginBase
    {
	    protected void HandlePing(IMessage message, ref MessageReceivedEventArgs e)
	    {
		    if (message.Label != "Ping")
			    return;
		    var response = MessageFactory.CreateReply(message);
		    response.Label = "Ping Response";
		    response[nameof(PingStats.ServedBy)] = Communicator.GetState<string>(ApolloConstants.RegisteredAsKey);
		    response[nameof(PingStats.TimeRequestEnqueuedUtc)] = message.EnqueuedTimeUtc;
		    Communicator.SendToClientAsync(response);
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
			    var message = MessageFactory.CreateNewMessage("Ping");
			    var startTime = DateTime.UtcNow;
			    message.TimeToLive = timeOut ?? TimeSpan.FromSeconds(30);
			    await sendMessage(message);
			    var response = await Communicator.WaitForReplyTo(message, cancellationToken);
			    switch (response?.Label)
			    {
				    case ApolloConstants.NegativeAcknowledgement:
					    retVal.Result = PingStats.PingResult.AddresseeNotFound;
					    break;
				    case "Ping Response":
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
