using Apollo.Common.Abstractions;
using System;

namespace Apollo.ServiceBus.Mocks
{
	public class MockMessage
	{
		public IMessage Message { get; set; }
		public DateTimeOffset EarliestDeliveryTime { get; set; } = DateTimeOffset.MinValue;
		public DateTimeOffset LatestDeliveryTime { get; set; } = DateTimeOffset.MaxValue;
	}
}
