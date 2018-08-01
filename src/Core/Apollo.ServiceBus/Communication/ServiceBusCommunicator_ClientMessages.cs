using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common;
using Apollo.Common.Abstractions;
using Microsoft.Azure.ServiceBus;

namespace Apollo.ServiceBus.Communication
{
	public partial class __ServiceBusCommunicator
	{
		private IMessageSession _activeClientSession;
		private Task _clientSessionListenTask;
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
					_clientSessionListenTask = StartListeningForClientMessages(_clientSessionListenCancellationToken.Token);
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

		private async Task StartListeningForClientMessages(CancellationToken cancelToken)
		{
			_activeClientSession = await ClientSessionListener.Value.AcceptMessageSessionAsync(State[ApolloConstants.RegisteredAsKey].ToString(), TimeSpan.FromMinutes(30));
			while (_activeClientSession != null && _clientSessionListenCancellationToken != null && !_clientSessionListenCancellationToken.Token.IsCancellationRequested)
			{
				try
				{
					var messages = await _activeClientSession.ReceiveAsync(5, TimeSpan.FromSeconds(5));
					if (messages != null)
						foreach (var message in messages)
							await InvokeMessageHandlers(_activeClientSession, ApolloQueue.ClientSessions, new ServiceBusMessage(message), cancelToken);
				}
				catch (SessionLockLostException)
				{
					Logger.Info("Renewing session lock");
					if (_activeClientSession == null)
						throw;
					await ReaquireClientSessionLock();
				}
				catch (ServiceBusTimeoutException ex)
				{
					Logger.Warn($"Timed out while trying to get session lock ({ex.Message}). Will retry");
					Logger.Debug(ex);
					// Timed out reconnecting, just try again
				}
				catch (Exception ex)
				{
					Logger.Error("Encountered an exception while trying to receive client session messages", ex);
				}
			}
		}

		private async Task ReaquireClientSessionLock()
		{
			var waitTime = TimeSpan.FromSeconds(0.5);
			while (true)
			{
				try
				{
					await _activeClientSession.RenewSessionLockAsync();
					Logger.Info("Successfully aquired lock");
					break;
				}
				catch (Exception renewException)
				{
					// Sometimes necessary when there are connection issues
					Logger.Warn($"Encountered error while re-aquiring session lock ({renewException.Message}), will create new session lock");

					try
					{
						_activeClientSession = await ClientSessionListener.Value.AcceptMessageSessionAsync(State[ApolloConstants.RegisteredAsKey].ToString(),TimeSpan.FromMinutes(30));
						Logger.Info("Successfully created a new session lock");

						break;
					}
					catch (ServiceBusCommunicationException)
					{
						Logger.Info("Connection to service bus lost, waiting for it to re-establish");
						while (true)
						{
							try
							{
								var entry = await Dns.GetHostEntryAsync(Configuration.ConnectionStringBuilder.Endpoint.Replace("sb://", string.Empty));
								if (entry != null)
								{
									Logger.Info($"Managed to resolve service bus endpoint to {entry.HostName}, will now retry");
									break;
								}
							}
							catch
							{
								// Suppress
							}
							await Task.Delay(300, _clientSessionListenCancellationToken.Token);
						}
						Configuration.Reconnect();
						await Impl.Recreate();
						continue;
					}
					catch (Exception recreateException)
					{
						Logger.Warn($"Encountered error while creating new session lock, will wait and try again in {waitTime.TotalSeconds} seconds");
						Logger.Debug(recreateException);
					}

					await Task.Delay(waitTime, _clientSessionListenCancellationToken.Token);
					if (_clientSessionListenCancellationToken.Token.IsCancellationRequested)
						break;
					waitTime = TimeSpan.FromMilliseconds(Math.Min(waitTime.TotalMilliseconds * 1.5, TimeSpan.FromMinutes(1).TotalMilliseconds));
				}
			}
		}

		public Task SendToClientAsync(IMessage message, CancellationToken? token = null) => SendToClientAsync(token, message);
		public Task SendToClientsAsync(params IMessage[] messages) => SendToClientAsync(null, messages);
		public async Task SendToClientAsync(CancellationToken? token, params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of messages to a client");
			if (messages.All(m => m is ServiceBusMessage))
			{
				foreach (var message in messages)
					OnMessageSent(message, ApolloQueue.ClientSessions);
				try
				{
					await ClientSessionSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
				}
				catch (Exception ex)
				{
					Logger.Warn(ex);
					throw;
				}
			}
			else
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
		}
	}
}
