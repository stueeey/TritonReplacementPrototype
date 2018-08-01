using System;
using System.Collections.Generic;
using Apollo.Common.Abstractions;

namespace Apollo.Common.Infrastructure
{
	public abstract class MessageBase : IMessage
	{
		#region Implementation of IMessage

		public abstract string Identifier { get; set; }
		public abstract string Label { get; set; }
		public abstract string TargetSession { get; set; }
		public abstract string To { get; set; }
		public abstract string ReplyToEntity { get; set; }
		public abstract string ReplyToSession { get; set; }
		public abstract string BodyType { get; set; }
		public abstract byte[] Body { get; set; }
		public abstract long BodySize { get; }
		public abstract TimeSpan TimeToLive { get; set; }
		public abstract DateTime EnqueuedTimeUtc { get; }
		public abstract string ResponseTo { get; set; }
		public abstract IDictionary<string, object> Properties { get; }
		public object this[string propertyKey]
		{
			get => Properties.ContainsKey(propertyKey) ? Properties[propertyKey] : null;
			set
			{
				if (value == null)
					Properties.Remove(propertyKey);
				else
					Properties[propertyKey] = value;
			}
		}

		#endregion
	}
}
