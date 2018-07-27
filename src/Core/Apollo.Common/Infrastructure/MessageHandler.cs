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
		private MessageReceivedDelegate _onMessageReceived;
		private Predicate<IMessage> _messageFilter;

		public static MessageHandler CreateFakeHandler() => new MessageHandler(null, (q, m, c) => MessageStatus.Unhandled) { MessageFilter = (m) => false };

		public ApolloPluginBase Plugin { get; }
		public MessageHandler(ApolloPluginBase plugin, MessageReceivedDelegate onMessageReceived, MessageReceivedErrorDelegate onError = null)
		{
			Plugin = plugin;
			OnMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
		}

		public MessageHandler(ApolloPluginBase plugin, string messageLabel, MessageReceivedDelegate onMessageReceived, MessageReceivedErrorDelegate onError = null)
		{
			OnMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
			MessageFilter = m => m.LabelMatches(messageLabel);
			FilterName = $"Label == {messageLabel}";
		}

		public string FilterName { get; set; }
		public string ActionName { get; set; }

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

		public MessageReceivedDelegate OnMessageReceived 
		{ 
			get => _onMessageReceived; 
			set
			{ 
				_onMessageReceived = value; 
				ActionName = value?.Method?.Name;
			} 
		}
		public Predicate<IMessage> MessageFilter
		{ 
			get => _messageFilter; 
			set
			{ 
				_messageFilter = value; 
				FilterName = value?.Method?.Name;
			} 
		}
		public MessageReceivedErrorDelegate OnError { get; set; }

		public override string ToString()
		{
			return $"{Plugin?.GetType().Name ?? "<System>"} {FilterName ?? "<Unknown Filter>"} {ActionName ?? "<Unknown Action>"}";
		}
	}
}
