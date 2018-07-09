using Apollo.Common.Abstractions;
using log4net.Core;
using System;
using System.Threading;

namespace Apollo.Common.Infrastructure
{
	public delegate MessageStatus MessageReceivedDelegate(ApolloQueue sourceQueue, IMessage message, CancellationToken? cancelToken);
	public delegate void MessageReceivedErrorDelegate(IMessage message, Exception error);

	public class MessageHandler
    {
		public ApolloPluginBase Plugin { get; }
		public MessageHandler(ApolloPluginBase Plugin, MessageReceivedDelegate onMessageReceived, MessageReceivedErrorDelegate onError = null)
		{
			OnMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
		}

		public MessageHandler(ApolloPluginBase Plugin, string messageLabel, MessageReceivedDelegate onMessageReceived, MessageReceivedErrorDelegate onError = null)
		{
			OnMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
			MessageFilter = m => StringComparer.OrdinalIgnoreCase.Equals(m.Label.Trim(), messageLabel);
		}

		public bool PassesFilter(IMessage message)
		{
			try
			{
				return MessageFilter?.Invoke(message) ?? true;
			}
			catch (Exception ex)
			{
				var logger = Plugin?.GetLogger();
				logger?.Error($"Encountered an error while checking if a message with label {message?.Label ?? "<Blank>"} ({ex.Message})");
				logger?.Debug(ex);
				return false; 
			}
		}

		public MessageStatus HandleMessage(ApolloQueue queue, IMessage message, CancellationToken? cancelToken)
		{
			if (OnMessageReceived == null)
				return MessageStatus.Unhandled;
			try
			{
				return OnMessageReceived.Invoke(queue, message, cancelToken);
			}
			catch (Exception ex)
			{
				var logger = Plugin?.GetLogger();
				logger?.Error($"Encountered an error while processing a message with label {message.Label} ({ex.Message})");
				logger?.Debug(ex);
				OnError?.Invoke(message, ex);
				throw;
			}
		}

		public MessageReceivedDelegate OnMessageReceived { get; set; }
		public Predicate<IMessage> MessageFilter { get; set; }
		public MessageReceivedErrorDelegate OnError { get; set; }
	}
}
