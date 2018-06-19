﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.ServiceBus.Communication
{
	public partial class ServiceBusCommunicator
	{
		private Task _aliasSessionListenTask;
		private OnMessageReceivedDelegate _aliasMessageReceivedDelegate;
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
					_aliasSessionListenTask = StartListeningForAliasMessages(_clientSessionListenCancellationToken.Token);
				}
				else if (AliasQueueListener.IsValueCreated)
				{
					Logger.Debug("Stopped listening for alias messages");
					_aliasSessionListenCancellationToken.Cancel();
					_clientSessionListenTask.Wait();
					_clientSessionListenTask = null;
					AliasQueueListener.Value.CloseAsync().Wait();
				}
			}
		}

		private void OnAliasMessageReceived(IMessage m, ref MessageReceivedEventArgs e)
		{
			Logger.Debug($"Received a new alias message with label {m.Label} for {m.TargetSession}");
			CheckIfAnyoneIsWaitingForMessage(m, e);
		}

		private async Task StartListeningForAliasMessages(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var message = await AliasQueueListener.Value.ReceiveAsync();
				if (message != null)
				{
					await Task.Run(() => InvokeMessageHandlers(
						AliasQueueListener.Value,
						_aliasMessageReceivedDelegate,
						new ServiceBusMessage(message),
						_aliasSessionListenCancellationToken.Token,
						OnAliasMessageReceived), cancellationToken);
				}
			}
		}

		public async Task SendToAliasAsync(string alias, IMessage message) => await SendToAliasAsync(alias, new[] { message });

		public async Task SendToAliasAsync(string alias, params IMessage[] messages)
		{
			if (!messages.Any())
				throw new InvalidOperationException("Tried to send an empty array of messages");
			if (messages.All(m => m is ServiceBusMessage))
			{
				foreach (var message in messages)
					message.Properties[TritonConstants.TargetAliasKey] = alias;
				await AliasQueueSender.Value.SendAsync(messages.Select(m => ((ServiceBusMessage) m).InnerMessage).ToArray());
			}
			else
				throw new InvalidOperationException($"{GetType().Name} cannot send messages which do not inherit from {nameof(ServiceBusMessage)}");
		}
	}
}
