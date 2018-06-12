using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.ServiceBus.Communication
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
					_serverJobsListenTask = Task.Run(async () =>
					{
						while (!_serverJobsListenCancellationToken.IsCancellationRequested)
						{
							var message = await ServerQueueListener.Value.ReceiveAsync();
							if (message != null)
							{
								await Task.Run(() => InvokeMessageHandlers(
									ServerQueueListener.Value, 
									_serverJobsMessageReceivedDelegate,
									new ServiceBusMessage(message), 
									_serverJobsListenCancellationToken.Token, 
									OnServerJobReceived));
							}
						}
					});
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

		public async Task SendToServerAsync(params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of messages to the server");
			if (messages.All(m => m is ServiceBusMessage))
				await ServerQueueSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage)m).InnerMessage).ToArray());
			else
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
		}
	}
}
