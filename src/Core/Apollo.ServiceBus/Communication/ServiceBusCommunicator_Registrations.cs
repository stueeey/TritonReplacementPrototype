using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;

namespace Apollo.ServiceBus.Communication
{
	public partial class __ServiceBusCommunicator
	{
		private Task _registrationsListenTask;
		private CancellationTokenSource _registrationsListenCancellationToken;
		private bool _listenForRegistrations;
		private readonly object _listenForRegistrationsToken = new object();

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
				await InvokeMessageHandlers(RegistrationListener.Value,	ApolloQueue.Registrations, new ServiceBusMessage(message), cancellationToken);
			}
		}

		public Task SendRegistrationMessageAsync(IMessage message, CancellationToken? token = null) => SendRegistrationMessageAsync(token, message);
		public Task SendToRegistrationsAsync(params IMessage[] messages) => SendRegistrationMessageAsync(null, messages);
		public async Task SendRegistrationMessageAsync(CancellationToken? token, params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of registration messages");
			if (!messages.All(m => m is ServiceBusMessage))
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
			try
			{
				await RegistrationSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
			}
			catch (Exception ex)
			{
				Logger.Warn(ex);
				throw;
			}
			foreach (var message in messages)
				OnMessageSent(message, ApolloQueue.Registrations);
		}
	}
}
