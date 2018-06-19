using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Abstractions;

namespace Soei.Triton2.ServiceBus
{
    public class ServiceBusMessage : IMessage
    {
	    public Message InnerMessage { get; }

	    public ServiceBusMessage() : this(new Message())
	    {
	    }

	    public ServiceBusMessage(Message innerMessage)
	    {
		    InnerMessage = innerMessage;
	    }

	    #region Implementation of ICloneable

	    public object Clone()
	    {
		    return new ServiceBusMessage(InnerMessage);
	    }

	    #endregion

	    #region Implementation of IMessage

	    public string Identifier
	    {
		    get => InnerMessage.MessageId;
		    set => InnerMessage.MessageId = value;
	    }

	    public string Label
	    {
		    get => InnerMessage.Label;
		    set => InnerMessage.Label = value;
	    }

	    public string TargetSession
	    {
		    get => InnerMessage.SessionId;
		    set => InnerMessage.SessionId = value;
	    }

	    public string To
	    {
		    get => InnerMessage.To;
		    set => InnerMessage.To = value;
	    }

	    public string ReplyToEntity
	    {
		    get => InnerMessage.ReplyTo;
		    set => InnerMessage.ReplyTo = value;
	    }

	    public string ReplyToSession
	    {
		    get => InnerMessage.ReplyToSessionId;
		    set => InnerMessage.ReplyToSessionId = value;
	    }

	    public string BodyType 
	    {
		    get => InnerMessage.ContentType;
		    set => InnerMessage.ContentType = value;
	    }

	    public byte[] Body 
	    {
		    get => InnerMessage.Body;
		    set => InnerMessage.Body = value;
	    }

	    public long BodySize => InnerMessage.Size;


	    public TimeSpan TimeToLive
	    {
		    get => InnerMessage.TimeToLive;
		    set => InnerMessage.TimeToLive = value;
	    }

	    public DateTime SentTimeUtc => InnerMessage.SystemProperties.EnqueuedTimeUtc;

	    public string ResponseTo
	    {
		    get => InnerMessage.CorrelationId;
		    set => InnerMessage.CorrelationId = value;
	    }

	    public IDictionary<string, object> Properties => InnerMessage.UserProperties;

	    public object this[string propertyKey]
	    {
		    get => Properties.ContainsKey(propertyKey) ? Properties[propertyKey] : null;
		    set => Properties[propertyKey] = value;
	    }

	    #endregion
    }
}
