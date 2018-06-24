using System.Threading;
using Apollo.Common.Abstractions;

namespace Apollo.Common.Infrastructure
{
	public class MessageReceivedEventArgs
	{
		public MessageReceivedEventArgs(IServiceCommunicator communicator, CancellationToken cancelToken)
		{
			Communicator = communicator;
			CancelToken = cancelToken;
		}

		public CancellationToken CancelToken { get; }
		public IServiceCommunicator Communicator { get; }
		public MessageStatus Status { get; set; } = MessageStatus.Unhandled;
	}
}