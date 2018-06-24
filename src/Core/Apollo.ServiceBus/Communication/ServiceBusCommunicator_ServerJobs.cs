﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.ServiceBus.Communication
{
	public partial class ServiceBusCommunicator
	{
		private Task _serverJobsListenTask;
		private OnMessageReceivedDelegate _serverJobsMessageReceivedDelegate;
		private CancellationTokenSource _serverJobsListenCancellationToken;
		private bool _listenForServerJobs;
		private readonly object _listenForServerJobsToken = new object();

		void OnServerJobReceived(IMessage m, ref MessageReceivedEventArgs e)
		{
			Logger.Debug($"Received a new server job with a label of {m.Label}");
			CheckIfAnyoneIsWaitingForMessage(m, e);
		}

		private void HandleListenForServerJobsChanged(bool enabled)
		{
			lock (_listenForServerJobsToken)
			{
				if (_listenForServerJobs == enabled)
					return;
				_listenForServerJobs = enabled;
				if (enabled)
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

		private async Task StartListeningForServerJobs(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var message = await ServerQueueListener.Value.ReceiveAsync();
				if (message != null)
				{
					var sbMessage = new ServiceBusMessage(message);
					OnMessageReceived(sbMessage, ApolloQueue.ServerRequests);
					await Task.Run(() => InvokeMessageHandlers(
						ServerQueueListener.Value,
						_serverJobsMessageReceivedDelegate,
						sbMessage,
						_serverJobsListenCancellationToken.Token,
						OnServerJobReceived), cancellationToken);
				}
			}
		}

		public async Task SendToServerAsync(IMessage message) => await SendToServerAsync(new[] { message });

		public async Task SendToServerAsync(params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of messages to the server");
			if (messages.All(m => m is ServiceBusMessage))
			{
				foreach (var message in messages)
					OnMessageSent(message, ApolloQueue.ServerRequests);
				await ServerQueueSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
			}
			else
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
		}
	}
}
