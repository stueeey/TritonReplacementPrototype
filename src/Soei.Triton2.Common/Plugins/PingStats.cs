using System;
using System.Collections.Generic;
using System.Text;

namespace Soei.Triton2.Common.Plugins
{
	public class PingStats
	{
		public DateTime TimeRequestEnqueuedUtc { get; set; }
		public DateTime TimeRequestReceivedUtc { get; set; }
		public DateTime TimeResponseSentUtc { get; set; }
		public DateTime TimeResponseEnqueuedUtc { get; set; }

		public TimeSpan RoundTripTime { get; set; }
		public TimeSpan QueuedTime => (TimeRequestReceivedUtc - TimeRequestEnqueuedUtc) + (TimeRequestReceivedUtc - TimeResponseEnqueuedUtc);
		public TimeSpan RequestDuration => TimeRequestReceivedUtc - TimeRequestEnqueuedUtc;
		public TimeSpan ResponseDuration => TimeRequestReceivedUtc - TimeResponseSentUtc;

		public override string ToString()
		{
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($">> Request Enqueued: {TimeRequestEnqueuedUtc.ToLocalTime():HH:mm:ss}");
			stringBuilder.AppendLine($">> Request Received: {TimeRequestReceivedUtc.ToLocalTime():HH:mm:ss}");
			stringBuilder.AppendLine($">> Response Sent: {TimeResponseSentUtc.ToLocalTime():HH:mm:ss}");
			stringBuilder.AppendLine($">> Response Enqueued: {TimeResponseEnqueuedUtc.ToLocalTime():HH:mm:ss}");
			stringBuilder.AppendLine($">> RTT: {RoundTripTime.TotalMilliseconds:N2} ms");
			stringBuilder.AppendLine($">> Queued Duration: {QueuedTime.TotalMilliseconds:N2} ms");
			stringBuilder.AppendLine($">> Request Duration: {RequestDuration.TotalMilliseconds:N2} ms");
			stringBuilder.AppendLine($">> Response Duration: {ResponseDuration.TotalMilliseconds:N2} ms");
			return stringBuilder.ToString();
		}
	}
}
