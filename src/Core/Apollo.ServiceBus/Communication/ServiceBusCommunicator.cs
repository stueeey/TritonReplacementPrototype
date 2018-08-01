using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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
						TraceLogger.Debug(
							$"{currentActivity.OperationName} Duration: {currentActivity.Duration}\n\t{string.Join("\n\t", currentActivity.Tags)}");
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
		public override Task SendToClientAsync(params IMessage[] messages) => SendMessageAsync(ApolloQueue.ServerRequests, ServerQueueSender, messages);
		public override Task SendToRegistrationAsync(params IMessage[] messages) => SendMessageAsync(ApolloQueue.ServerRequests, ServerQueueSender, messages);
		public override Task SendToServerAsync(params IMessage[] messages) => SendMessageAsync(ApolloQueue.ServerRequests, ServerQueueSender, messages);
		#endregion

		#endregion
	}
}
