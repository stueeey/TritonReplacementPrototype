using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using log4net;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Apollo.ServiceBus.Communication
{
	public class ServiceBusCommunicator : ServiceCommunicatorBase
	{
		private static readonly ILog ClassLogger = LogManager.GetLogger(Assembly.GetEntryAssembly(), $"{ApolloConstants.LoggerInternalsPrefix}.{MethodBase.GetCurrentMethod().DeclaringType.Name}");
		private static readonly ILog TraceLogger = LogManager.GetLogger(Assembly.GetEntryAssembly(), $"{ApolloConstants.LoggerInternalsPrefix}.Tracing");
		protected ServiceBusConfiguration Configuration { get; }

		private IServiceBusImplementations Impl { get; }
		protected Lazy<IMessageReceiver> RegistrationListener => Impl.RegistrationListener;
		protected Lazy<IMessageSender> RegistrationSender => Impl.RegistrationSender;
		protected Lazy<IMessageReceiver> ServerQueueListener => Impl.ServerQueueListener;
		protected Lazy<IMessageSender> ServerQueueSender => Impl.ServerQueueSender;
		protected Lazy<ISessionClient> ClientSessionListener => Impl.ClientSessionListener;
		protected Lazy<IMessageSender> ClientSessionSender => Impl.ClientSessionSender;
		protected Lazy<IMessageReceiver> AliasQueueListener => Impl.AliasQueueListener;
		protected Lazy<IMessageSender> AliasQueueSender => Impl.AliasQueueSender;

		public int ServerMessagesPerReceive { get; set; } = 5;
		public int ClientMessagesPerReceive { get; set; } = 1;
		public int RegistrationMessagesPerReceive { get; set; } = 5;
		public int AliasMessagesPerReceive { get; set; } = 5;

		public void SetLogger(ILog log = null) => Logger = log ?? ClassLogger;

		private async Task InvokeMessageHandlers(IReceiverClient receiver, ApolloQueue queue, ServiceBusMessage message, CancellationToken? token)
		{
			if (message == null)
				return;

			if (Handlers.TryGetValue(queue, out var handlers))
			{
				var status = MessageStatus.Unhandled;
				foreach (var handler in handlers.Where(h => h.PassesFilter(message)))
				{
					try
					{
						if (token?.IsCancellationRequested ?? false)
							return;
						status = handler.HandleMessage(queue, message, token);
						if (status.HasFlag(MessageStatus.Handled))
							break;
					}
					catch (Exception ex)
					{
						Logger.Error($"Encountered an error in {handler.OnMessageReceived.Method.DeclaringType?.Name ?? "<Unknown>"}.{handler.OnMessageReceived.Method.Name} while handling a message labelled {message.Label}", ex);
					}
				}
				if (status.HasFlag(MessageStatus.MarkedForDeletion))
					await receiver.CompleteAsync(message.InnerMessage.SystemProperties.LockToken);
				else if (string.IsNullOrWhiteSpace(message.ResponseTo))
					await receiver.DeadLetterAsync(message.InnerMessage.SystemProperties.LockToken, $"{State[ApolloConstants.RegisteredAsKey]} does not have a plugin which can handle this message");
				else
					await receiver.DeadLetterAsync(message.InnerMessage.SystemProperties.LockToken, $"{State[ApolloConstants.RegisteredAsKey]} is not expecting or longer waiting for this response");
			}
			else
				Logger.Error($"Received a message on queue {queue} without having any handlers registered for that queue! (This should not be possible and implies that something has gone wrong)");
		}

		public ServiceBusCommunicator(ServiceBusConfiguration configuration, IServiceBusImplementations serviceBusImplementations = null)
		{
			SetLogger();

			Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			State[ApolloConstants.RegisteredAsKey] = configuration.Identifier;
			MessageFactory = new ServiceBusMessageFactory(ServiceBusConstants.DefaultRegisteredClientsQueue, configuration.Identifier);
			Impl = serviceBusImplementations ?? new DefaultServiceBusImplementations(configuration);
			SubscribeToServiceBusTrace();
			LogServiceBusConfiguration();
		}

		private void LogServiceBusConfiguration()
		{
			Logger.Info("Connecting to service bus with the following settings:");
			Logger.Info($"Endpoint: {Configuration.ConnectionStringBuilder.Endpoint}");
			Logger.Info($"Transport: {Configuration.ConnectionStringBuilder.TransportType}");
			Logger.Info($"Using SAS Key: {Configuration.ConnectionStringBuilder.SasKeyName}");
			Logger.Info($"To Entity: {Configuration.ConnectionStringBuilder.EntityPath}");
			Logger.Info($"As: {Configuration.Identifier}");
		}

		private static void SubscribeToServiceBusTrace()
		{
			DiagnosticListener.AllListeners.Subscribe(delegate(DiagnosticListener listener)
			{
				// subscribe to the Service Bus DiagnosticSource
				if (listener.Name == "Microsoft.Azure.ServiceBus")
				{
					// receive event from Service Bus DiagnosticSource
					listener.Subscribe(delegate(KeyValuePair<string, object> @event)
					{
						// Log operation details once it's done
						if (!@event.Key.EndsWith("Stop"))
							return;
						var currentActivity = Activity.Current;
						TraceLogger.Debug($"{currentActivity.OperationName} Duration: {currentActivity.Duration}\n\t{string.Join("\n\t", currentActivity.Tags)}");
					});
				}
			});
		}

		#region IServiceCommunicator

		private async Task SendMessageAsync(ApolloQueue queue, Lazy<IMessageSender> sender, IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException($"Tried to send an empty array of messages to {queue}");
			if (!messages.All(m => m is ServiceBusMessage))
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
			try
			{
				await sender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
				foreach (var message in messages)
					OnMessageSent(message, queue);
			}
			catch (Exception ex)
			{
				Logger.Warn(ex);
				throw;
			}
		}

		#region Sending
		public override Task SendToServerAsync(params IMessage[] messages) => SendMessageAsync(ApolloQueue.ServerRequests, ServerQueueSender, messages);
		public override async Task SendToClientsAsync(params IMessage[] messages)
		{
			if (messages == null)
				return;
			if (messages.Any(message => string.IsNullOrWhiteSpace(message.TargetSession)))
				throw new ArgumentException("Tried to send a client message without a target session specified, make sure you set message.TargetSession to the id of the target client", nameof(messages));
			await SendMessageAsync(ApolloQueue.ClientSessions, ClientSessionSender, messages);
		}

		public override Task SendToRegistrationsAsync(params IMessage[] messages) => SendMessageAsync(ApolloQueue.Registrations, RegistrationSender, messages);
		public override async Task SendToAliasAsync(params IMessage[] messages)
		{
			if (messages == null)
				return;
			if (messages.Any(message => string.IsNullOrWhiteSpace(message.GetTargetAlias())))
				throw new ArgumentException("Tried to send an alias message without an alias specified, call message.SetTargetAlias(<alias>)", nameof(messages));
			await SendMessageAsync(ApolloQueue.Aliases, AliasQueueSender, messages);
		}
		

		#endregion

		#region Listening

		#region Clients
		private IMessageSession _activeClientSession;
		private Task _clientSessionListenTask;
		private CancellationTokenSource _clientSessionListenCancellationToken;
		private readonly object _listenForClientSessionMessagesToken = new object();

		private async Task StartListeningForClientMessages(CancellationToken cancelToken)
		{
			_activeClientSession = await ClientSessionListener.Value.AcceptMessageSessionAsync(State[ApolloConstants.RegisteredAsKey].ToString(), TimeSpan.FromMinutes(30));
			while (_activeClientSession != null && _clientSessionListenCancellationToken != null && !_clientSessionListenCancellationToken.Token.IsCancellationRequested)
			{
				try
				{
					var messages = await _activeClientSession.ReceiveAsync(ClientMessagesPerReceive);
					if (messages == null) 
						continue;
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
					catch (ServiceBusCommunicationException ex)
					{
						Logger.Debug(ex.Message);
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

		protected override void HandleListenForClientMessagesChanged(bool value)
		{
			lock (_listenForClientSessionMessagesToken)
			{
				if (value)
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
		#endregion

		#region Registration
		private Task _registrationsListenTask;
		private CancellationTokenSource _registrationsListenCancellationToken;
		private readonly object _listenForRegistrationsToken = new object();

		private async Task StartListeningForRegistrations(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var messages = await RegistrationListener.Value.ReceiveAsync(RegistrationMessagesPerReceive);
				if (messages == null) 
					continue;
				foreach (var message in messages)
					await InvokeMessageHandlers(RegistrationListener.Value, ApolloQueue.Registrations, new ServiceBusMessage(message), cancellationToken);
			}
		}
		protected override void HandleListenForRegistrationsChanged(bool value)
		{
			lock (_listenForRegistrationsToken)
			{
				if (value)
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
		#endregion

		#region Server
		private Task _serverJobsListenTask;
		private CancellationTokenSource _serverJobsListenCancellationToken;
		private readonly object _listenForServerJobsToken = new object();

		private async Task StartListeningForServerJobs(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var messages = await ServerQueueListener.Value.ReceiveAsync(ServerMessagesPerReceive);
				if (messages == null) 
					continue;
				foreach (var message in messages)
					await InvokeMessageHandlers(ServerQueueListener.Value, ApolloQueue.ServerRequests, new ServiceBusMessage(message), cancellationToken);
			}
		}

		protected override void HandleListenForServerJobsChanged(bool value)
		{
			lock (_listenForServerJobsToken)
			{
				if (value)
				{
					Logger.Debug("Listening for server jobs");
					_serverJobsListenCancellationToken = new CancellationTokenSource();
					_serverJobsListenTask = StartListeningForServerJobs(_serverJobsListenCancellationToken.Token);
				}
				else if (_serverJobsListenTask != null)
				{
					Logger.Debug("Stopped listening for server jobs");
					_serverJobsListenCancellationToken.Cancel();
					_serverJobsListenTask.Wait();
					_serverJobsListenTask = null;
					ServerQueueListener.Value.CloseAsync().Wait();
				}
			}
		}
		#endregion

		private Task _aliasSessionListenTask;
		private CancellationTokenSource _aliasSessionListenCancellationToken;
		private readonly object _listenForAliasSessionMessagesToken = new object();

		private async Task StartListeningForAliasMessages(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var messages = await AliasQueueListener.Value.ReceiveAsync(AliasMessagesPerReceive);
				if (messages == null) 
					continue;
				foreach (var message in messages)
					await InvokeMessageHandlers(AliasQueueListener.Value, ApolloQueue.Aliases, new ServiceBusMessage(message), cancellationToken);
			}
		}
		protected override void HandleListenForAliasMessagesChanged(bool value)
		{
			lock (_listenForAliasSessionMessagesToken)
			{
				if (value)
				{
					_aliasSessionListenCancellationToken = new CancellationTokenSource();
					Logger.Debug("Listening for alias messages");
					_aliasSessionListenTask = StartListeningForAliasMessages(_aliasSessionListenCancellationToken.Token);
				}
				else if (AliasQueueListener.IsValueCreated)
				{
					Logger.Debug("Stopped listening for alias messages");
					_aliasSessionListenCancellationToken.Cancel();
					_aliasSessionListenTask.Wait();
					_aliasSessionListenTask = null;
					AliasQueueListener.Value.CloseAsync().Wait();
				}
			}
		}
		#endregion

		protected override TimeSpan MaxReplyWaitTime => ApolloConstants.MaximumReplyWaitTime;
		protected override ApolloQueue? ParseQueue(string queueName)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, Configuration.ClientAliasesQueue))
				return ApolloQueue.Aliases;
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, Configuration.RegisteredClientsQueue))
				return ApolloQueue.ClientSessions;
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, Configuration.RegistrationQueue))
				return ApolloQueue.Registrations;
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, Configuration.ServerRequestsQueue))
				return ApolloQueue.ServerRequests;
			return null;
		}

		#endregion

		public override void Dispose()
		{
			base.Dispose();
			Configuration.Connection?.CloseAsync().Wait();
		}
	}
}
