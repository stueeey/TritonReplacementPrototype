﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Soei.Apollo.Common;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;

namespace Soei.Apollo.ServiceBus.Communication
{
	public partial class ServiceBusCommunicator
	{
		private IMessageSession _activeClientSession;
		private Task _clientSessionListenTask;
		private OnMessageReceivedDelegate _clientSessionMessageReceivedDelegate;
		private CancellationTokenSource _clientSessionListenCancellationToken;
		private bool _listenForClientSessionMessages;
		private readonly object _listenForClientSessionMessagesToken = new object();

		private void HandleListenForClientMessagesChanged(bool enabled)
		{
			lock (_listenForClientSessionMessagesToken)
			{
				if (_listenForClientSessionMessages == enabled)
					return;
				_listenForClientSessionMessages = enabled;
				if (enabled)
				{
					_clientSessionListenCancellationToken = new CancellationTokenSource();
					Logger.Debug("Listening for client messages");
					_clientSessionListenTask = StartListeningForClientMessages();
				}
				else if (_activeClientSession != null)
				{
					Logger.Debug("Stopped listening for client messages");
					_clientSessionListenCancellationToken.Cancel();
					_clientSessionListenTask.Wait();
					_clientSessionListenTask = null;
					_activeClientSession.CloseAsync().Wait();
				}
			}
		}

		private void OnClientSessionMessageReceived(IMessage m, ref MessageReceivedEventArgs e)
		{
			Logger.Debug($"Received a new client session message with label {m.Label} and ID {m.Identifier}");
			CheckIfAnyoneIsWaitingForMessage(m, e);
		}

		private async Task StartListeningForClientMessages()
		{
			_activeClientSession = await ClientSessionListener.Value.AcceptMessageSessionAsync(State[TritonConstants.RegisteredAsKey].ToString());
			while (_activeClientSession != null && _clientSessionListenCancellationToken != null &&
			       !_clientSessionListenCancellationToken.Token.IsCancellationRequested)
			{
				try
				{
					var message = await _activeClientSession.ReceiveAsync();
					if (message != null)
					{
						var sbMessage = new ServiceBusMessage(message);
						OnMessageReceived(sbMessage, ApolloQueue.ClientSessions);
						await Task.Run(() => InvokeMessageHandlers(
							_activeClientSession,
							_clientSessionMessageReceivedDelegate,
							sbMessage,
							_clientSessionListenCancellationToken.Token,
							OnClientSessionMessageReceived));
					}
				}
				catch (SessionLockLostException)
				{
					if (_activeClientSession == null)
						throw;
					await _activeClientSession.RenewSessionLockAsync();
				}
				catch (ServiceBusTimeoutException ex)
				{
					Logger.Warn("Timed out while trying to get session lock. Will retry", ex);
					// Timed out reconnecting, just try again
				}
				catch (Exception ex)
				{
					Logger.Error("Encountered an exception while trying to receive client session messages", ex);
				}
			}
		}

		public async Task SendToClientAsync(IMessage message) => await SendToClientAsync(new[] { message });

		public async Task SendToClientAsync(params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of messages to a client");
			if (messages.All(m => m is ServiceBusMessage))
			{
				foreach (var message in messages)
					OnMessageSent(message, ApolloQueue.ClientSessions);
				await ClientSessionSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
			}
			else
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
		}
	}
}