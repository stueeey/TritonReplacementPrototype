using System;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Apollo.Common.Plugins
{

	public class PingStats
	{
		public enum PingResult
		{
			Success,
			Timeout,
			AddresseeNotFound,
			Exception
		}

		public PingResult Result { get; set;}
		public ExceptionDispatchInfo Exception { get; set;}
		public DateTime TimeRequestEnqueuedUtc { get; set; }
		public DateTime TimeResponseEnqueuedUtc { get; set; }

		public TimeSpan RoundTripTime { get; set; }
		public TimeSpan QueuedTime => TimeResponseEnqueuedUtc - TimeRequestEnqueuedUtc;

		public string ServedBy { get; set; }

		public override string ToString()
		{
			return ToString(false);
		}

		public string ToString(bool verbose)
		{
			switch (Result)
			{
				case PingResult.Success:
					if (!verbose)
						return $"Total RTT: {RoundTripTime.TotalMilliseconds:N0} ms | Queue RTT Time: {QueuedTime.TotalMilliseconds:N0} ms | Served By: {ServedBy}";
					var stringBuilder = new StringBuilder();
					stringBuilder.AppendLine($">> Request Enqueued: {TimeRequestEnqueuedUtc.ToLocalTime():HH:mm:ss.fff}");
					stringBuilder.AppendLine($">> Response Enqueued: {TimeResponseEnqueuedUtc.ToLocalTime():HH:mm:ss.fff}");
					stringBuilder.AppendLine($">> RTT: {RoundTripTime.TotalMilliseconds:N2} ms");
					stringBuilder.AppendLine($">> Request Queue to Response Queue Duration: {QueuedTime.TotalMilliseconds:N2} ms");
					stringBuilder.AppendLine($">> Served By: {ServedBy}");
					return stringBuilder.ToString();
				case PingResult.Timeout:
					return "Timed out";
				case PingResult.AddresseeNotFound:
					return "Target alias is not currently owned";
				case PingResult.Exception:
					return verbose 
						? Exception.SourceException.ToString() 
						: Exception.SourceException.Message;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public void RethrowCaughtException()
		{
			Exception?.Throw();
		}
	}
}
