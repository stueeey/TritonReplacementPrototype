using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.ServiceBus.Communication
{
	public partial class __ServiceBusCommunicator
	{
		private Task _serverJobsListenTask;
		private CancellationTokenSource _serverJobsListenCancellationToken;
		private bool _listenForServerJobs;
		private readonly object _listenForServerJobsToken = new object();

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
				var messages = await ServerQueueListener.Value.ReceiveAsync(5, TimeSpan.FromSeconds(5));
				if (messages != null)
					foreach (var message in messages)
						await InvokeMessageHandlers(ServerQueueListener.Value, ApolloQueue.ServerRequests, new ServiceBusMessage(message), cancellationToken);
			}
		}

		public Task SendToServerAsync(IMessage message, CancellationToken? token = null) => SendToServerAsync(token, message);
		public Task SendToServerAsync(params IMessage[] messages) => SendToServerAsync(null, messages);
		public async Task SendToServerAsync(CancellationToken? token, params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of messages to the server");
			if (messages.All(m => m is ServiceBusMessage))
			{
				foreach (var message in messages)
					OnMessageSent(message, ApolloQueue.ServerRequests);
				try
				{
					await ServerQueueSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
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
