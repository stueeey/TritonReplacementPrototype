using System;
using System.Collections.Generic;

namespace Soei.Triton2.Common
{
    public interface IMessage : ICloneable
    {
	    string Identifier { get; set; }
	    string Label { get; set; }
	    string TargetSession { get; set;}
	    string To { get; set; }
	    string ReplyToEntity { get; set; }
	    string ReplyToSession { get; set; }
	    string BodyType { get; set; }
	    byte[] Body { get; set; }
	    long BodySize { get; }
	    TimeSpan TimeToLive { get; set; }
	    DateTime SentTimeUtc { get; }
	    string ResponseTo { get; set; }
	    IDictionary<string, object> Properties { get; }
    }
}
