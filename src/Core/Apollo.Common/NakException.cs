using System;
using System.Runtime.Serialization;
using Apollo.Common.Abstractions;

namespace Apollo.Common
{
	[Serializable]
	public class NakException : Exception
	{
		public IMessage Response { get; set; }

		public NakException()
		{
		}

		public NakException(string message) : base(message)
		{
		}

		public NakException(IMessage message) : base(message.GetReasonOrPlaceholder())
		{
		}

		public NakException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected NakException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
