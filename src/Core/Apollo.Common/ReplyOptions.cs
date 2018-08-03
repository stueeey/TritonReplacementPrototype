using System;
using System.Threading;
using Apollo.Common.Abstractions;

namespace Apollo.Common
{
	public class ReplyOptions
	{
		public ReplyOptions(IMessage message) : this(message?.ReplyToEntity, message?.Identifier, message?.TimeToLive ?? TimeSpan.MaxValue)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));
		}

		public ReplyOptions(string replyQueue, string messageIdentifier, TimeSpan timeout)
		{
			ReplyQueue = replyQueue;
			MessageIdentifier = messageIdentifier;
			Timeout = timeout;
		}

		public static ReplyOptions WaitForSingleReply(IMessage message) => new ReplyOptions(message) { MaxRepliesToWaitFor = 1 };
		public static ReplyOptions WaitForMultipleReplies(IMessage message, int maxMessages) => new ReplyOptions(message) { MaxRepliesToWaitFor = maxMessages };
		public static ReplyOptions WaitForTerminatingMessage(IMessage message) => new ReplyOptions(message);
		public static ReplyOptions WaitForTerminatingMessage(IMessage message, Predicate<IMessage> isTerminatingMessage)
		{
			return new ReplyOptions(message)
			{
				IsTerminatingMessage = isTerminatingMessage ?? throw new ArgumentNullException(nameof(isTerminatingMessage))
			};
		}

		public event Action<IMessage> ReplyReceived;
		internal void OnReplyReceived(IMessage message) => ReplyReceived?.Invoke(message);
		public int MaxRepliesToWaitFor { get; set; } = int.MaxValue;
		public TimeSpan Timeout { get; set; }
		public CancellationToken? CancelToken { get; set; }
		public Predicate<IMessage> IsTerminatingMessage { get; set; } = ApolloExtensions.IsTerminatingMessage;
		public string ReplyQueue { get; }
		public string MessageIdentifier { get; }
	}
}