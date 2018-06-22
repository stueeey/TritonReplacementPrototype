using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Soei.Apollo.Common;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;

namespace Soei.Apollo.ServiceBus.Communication
{
	public partial class ServiceBusCommunicator
	{
		private Task _registrationsListenTask;
		private OnMessageReceivedDelegate _registrationMessageReceivedDelegate;
		private CancellationTokenSource _registrationsListenCancellationToken;
		private bool _listenForRegistrations;
		private readonly object _listenForRegistrationsToken = new object();

		private void OnRegistrationReceived(IMessage m, ref MessageReceivedEventArgs e)
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
				if (message == null) 
					continue;
				var sbMessage = new ServiceBusMessage(message);
				OnMessageReceived(sbMessage, ApolloQueue.Registrations);
				await Task.Run(() => InvokeMessageHandlers(
					RegistrationListener.Value,
					_registrationMessageReceivedDelegate,
					sbMessage,
					_registrationsListenCancellationToken.Token,
					OnRegistrationReceived), cancellationToken);
			}
		}

		public async Task SendRegistrationMessageAsync(IMessage message) => await SendRegistrationMessageAsync(new [] { message });

		public async Task SendRegistrationMessageAsync(params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of registration messages");
			if (messages.All(m => m is ServiceBusMessage))
			{
				foreach (var message in messages)
					OnMessageSent(message, ApolloQueue.Registrations);
				await RegistrationSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
			}
			else
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
		}
	}
}
