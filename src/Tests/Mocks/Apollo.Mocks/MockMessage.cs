using System;
using System.Collections.Generic;
using Apollo.Common.Infrastructure;

namespace Apollo.Mocks
{
	public class MockMessage : MessageBase
	{
		#region Implementation of IMessage

		public override string Identifier { get; set; }
		public override string Label { get; set; }
		public override string TargetSession { get; set; }
		public override string To { get; set; }
		public override string ReplyToEntity { get; set; }
		public override string ReplyToSession { get; set; }
		public override string BodyType { get; set; }
		public override byte[] Body { get; set; }
		public override long BodySize => Body.LongLength;
		public override TimeSpan TimeToLive { get; set; }
		public override DateTime EnqueuedTimeUtc { get; } = DateTime.UtcNow;
		public override string ResponseTo { get; set; }
		public override IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

		#endregion
	}
}
