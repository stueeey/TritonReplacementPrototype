using System;
using System.Collections.Generic;
using System.Text;

namespace Soei.Triton2.Common.Plugins
{
	public class PingStats
	{
		public DateTime TimeRequestSentUtc { get; set; }
		public DateTime TimeRequestEnqueuedUtc { get; set; }
		public DateTime TimeRequestReceivedUtc { get; set; }
		public DateTime TimeResponseSentUtc { get; set; }
		public DateTime TimeResponseEnqueuedUtc { get; set; }
		public DateTime TimeResponseRecievedUtc { get; set; }

		public TimeSpan RoundTripTime => TimeResponseRecievedUtc - TimeRequestSentUtc;
		public TimeSpan QueuedTime => (TimeRequestReceivedUtc - TimeRequestEnqueuedUtc) + (TimeResponseRecievedUtc - TimeResponseEnqueuedUtc);
		public TimeSpan RequestDuration => TimeRequestReceivedUtc - TimeRequestSentUtc;
		public TimeSpan ResponseDuration => TimeResponseRecievedUtc - TimeResponseSentUtc;


	}
}
