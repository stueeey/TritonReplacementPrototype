using System;
using System.Collections.Generic;
using System.Text;
using Apollo.Common.Abstractions;

namespace Apollo.Mocks
{
	public class MockMessage : IMessage
	{
		#region Implementation of IMessage

		public string Identifier { get; set; }
		public string Label { get; set; }
		public string TargetSession { get; set; }
		public string To { get; set; }
		public string ReplyToEntity { get; set; }
		public string ReplyToSession { get; set; }
		public string BodyType { get; set; }
		public byte[] Body { get; set; }
		public long BodySize => Body.LongLength;
		public TimeSpan TimeToLive { get; set; }
		public DateTime EnqueuedTimeUtc { get; } = DateTime.UtcNow;
		public string ResponseTo { get; set; }
		public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

		public object this[string propertyKey]
		{
			get => Properties.ContainsKey(propertyKey) ? Properties[propertyKey] : null;
			set => Properties[propertyKey] = value;
		}

		#endregion
	}
}
