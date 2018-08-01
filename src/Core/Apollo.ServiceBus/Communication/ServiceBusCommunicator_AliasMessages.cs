using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common;
using Apollo.Common.Abstractions;

namespace Apollo.ServiceBus.Communication
{
	public partial class __ServiceBusCommunicator
	{
		private Task _aliasSessionListenTask;
		private CancellationTokenSource _aliasSessionListenCancellationToken;
		private bool _listenForAliasSessionMessages;
		private readonly object _listenForAliasSessionMessagesToken = new object();

		private void HandleListenForAliasMessagesChanged(bool enabled)
		{
			lock (_listenForAliasSessionMessagesToken)
			{
				if (_listenForAliasSessionMessages == enabled)
					return;
				_listenForAliasSessionMessages = enabled;
				if (enabled)
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

		private async Task StartListeningForAliasMessages(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var messages = await AliasQueueListener.Value.ReceiveAsync(5, TimeSpan.FromSeconds(5));
				if (messages != null)
					foreach (var message in messages)
						await Task.Run(() => InvokeMessageHandlers(AliasQueueListener.Value, ApolloQueue.Aliases, new ServiceBusMessage(message), cancellationToken));
			}
		}


		public Task SendToAliasAsync(string alias, IMessage message, CancellationToken? token = null) => SendToAliasAsync(alias, token, message);
		public Task SendToAliasAsync(string alias, params IMessage[] messages) => SendToAliasAsync(alias, null, messages);
		public async Task SendToAliasAsync(string alias, CancellationToken? token, params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of messages");
			if (messages.All(m => m is ServiceBusMessage))
			{
				foreach (var message in messages)
				{
					message.Properties[ApolloConstants.TargetAliasKey] = alias;
					OnMessageSent(message, ApolloQueue.Aliases);
				}
				try
				{
					await AliasQueueSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
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
