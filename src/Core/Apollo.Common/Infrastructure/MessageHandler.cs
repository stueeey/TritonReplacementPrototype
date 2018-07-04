using Apollo.Common.Abstractions;
using System;

namespace Apollo.Common.Infrastructure
{
	public delegate MessageStatus MessageReceivedDelegate(IServiceCommunicator communicator, IMessage message);
	public delegate void MessageReceivedErrorDelegate(IServiceCommunicator communicator, IMessage message, Exception error);

	public class MessageHandler
    {
		public MessageHandler(MessageReceivedDelegate onMessageReceived)
		{
			OnMessageReceived = onMessageReceived ?? throw new ArgumentNullException(nameof(onMessageReceived));
		}

		public MessageHandler(MessageReceivedDelegate onMessageReceived, string messageLabel)
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
			catch (Exception)
			{
				// We don't really have a good way to log this here
				// just treat exceptions as failing the filter
				return false; 
			}
		}

		public MessageStatus HandleMessage(IServiceCommunicator communicator, IMessage message)
		{
			if (OnMessageReceived == null)
				return MessageStatus.Unhandled;
			try
			{
				return OnMessageReceived.Invoke(communicator, message);
			}
			catch (Exception ex)
			{
				OnError?.Invoke(communicator, message, ex);
				throw;
			}
		}

		public MessageReceivedDelegate OnMessageReceived { get; set; }
		public Predicate<IMessage> MessageFilter { get; set; }
		public MessageReceivedErrorDelegate OnError { get; set; }
	}
}
