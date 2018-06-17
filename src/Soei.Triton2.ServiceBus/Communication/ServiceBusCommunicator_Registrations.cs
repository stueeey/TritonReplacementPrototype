using System.Threading;
using System.Threading.Tasks;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.ServiceBus.Communication
{
	public partial class ServiceBusCommunicator
	{
		private Task _registrationsListenTask;
		private OnMessageReceivedDelegate _registrationMessageReceivedDelegate;
		private CancellationTokenSource _registrationsListenCancellationToken;
		private bool _listenForRegistrations;
		private readonly object _listenForRegistrationsToken = new object();

		void OnRegistrationReceived(IMessage m, ref MessageReceivedEventArgs e)
		{
			Logger.Debug($"Received a new registration message with a callback session of {m.ReplyToSession}");
			CheckIfAnyoneIsWaitingForMessage(m, e);
		}

		private void HandleListenForRegistrationsChanged(bool enabled)
		{
			lock (_listenForRegistrationsToken)
			{
				if (_listenForRegistrations == enabled)
					return;
				_listenForRegistrations = enabled;

				if (enabled)
				{
					Logger.Debug("Listening for registration messages");
					_registrationsListenCancellationToken = new CancellationTokenSource();
					_registrationsListenTask = StartListeningForRegistrations(_registrationsListenCancellationToken.Token);
				}
				else if (_registrationsListenTask != null)
				{
					Logger.Debug("Stopped listening for registration messages");
					_registrationsListenCancellationToken.Cancel();
					_registrationsListenTask.Wait();
					_registrationsListenTask = null;
					RegistrationListener.Value.CloseAsync().Wait();
				}
			}
		}

		private async Task StartListeningForRegistrations(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var message = await RegistrationListener.Value.ReceiveAsync();
				if (message != null)
				{
					await Task.Run(() => InvokeMessageHandlers(
						RegistrationListener.Value,
						_registrationMessageReceivedDelegate,
						new ServiceBusMessage(message),
						_registrationsListenCancellationToken.Token,
						OnRegistrationReceived));
				}
			}
		}

		public async Task RegisterAsync(IMessage message)
		{
			if (message is ServiceBusMessage busMessage)
				await RegistrationSender.Value.SendAsync(busMessage.InnerMessage);
		}
	}
}
