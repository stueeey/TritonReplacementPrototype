using System;
using System.Threading.Tasks;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Plugins
{
    public class ClientCorePlugin : TritonPluginBase
    {
	    private IMessage _registrationMessage;
		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.ClientSessionMessageReceived += OnRegistrationAcknowledgementReceived;
			_registrationMessage = Communicator.MessageFactory.CreateNewMessage(TritonConstants.Registration);
			await Communicator.RegisterAsync(_registrationMessage);
		}

		public override void OnUninitialized()
		{
			base.OnUninitialized();
			Communicator.ClientSessionMessageReceived -= OnRegistrationAcknowledgementReceived;
		}

		private void OnRegistrationAcknowledgementReceived(IMessage m, ref MessageReceivedEventArgs e)
	    {
		    if (m.Label != TritonConstants.PositiveAcknowledgement || m.ResponseTo != _registrationMessage.Identifier) 
			    return;
		    Communicator.SignalPluginEvent("Registered");
		    Logger.Info($"Received confirmation of registration as {m.TargetSession}");
		    e.Status = MessageStatus.Complete;
		    Communicator.ClientSessionMessageReceived -= OnRegistrationAcknowledgementReceived;
	    }

	    public async Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token)
	    {
		    var message = MessageFactory.CreateNewMessage("Request Alias Ownership");
		    message.Properties["Desired Alias"] = alias;
		    message.Properties["Alias Token"] = token.ToString();
		    message.TimeToLive = TimeSpan.FromSeconds(10);
		    await Communicator.SendToServerAsync(message);
		    var response = await Communicator.WaitForReplyTo(message);
		    if (response.Label != TritonConstants.PositiveAcknowledgement || !response.Properties.ContainsKey("Alias Token"))
		    {
			    Logger.Warn($"Failed to get ownership of {alias}");
			    return Guid.Empty;
		    }
		    Logger.Warn($"Successfully got ownership of {alias}");
		    var responseToken = Guid.Parse(response.GetProperty("Alias Token"));
		    Communicator.State[alias] = responseToken;
		    return responseToken;
	    }
    }
}
