using Apollo.Common.Abstractions;
using Microsoft.Azure.ServiceBus;
using System;

namespace Apollo.ServiceBus.Mocks
{
	public class MockMessage
	{
		public Message Message { get; set; }
		public DateTimeOffset EarliestDeliveryTime { get; set; } = DateTimeOffset.MinValue;
		public DateTimeOffset LatestDeliveryTime { get; set; } = DateTimeOffset.MaxValue;
	}
}
