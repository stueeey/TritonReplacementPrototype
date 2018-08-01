using System;
using System.Collections.Generic;
using Apollo.Common.Infrastructure;
using Microsoft.Azure.ServiceBus;

namespace Apollo.ServiceBus
{
    public class ServiceBusMessage : MessageBase
    {
	    public Message InnerMessage { get; }

	    public ServiceBusMessage() : this(new Message())
	    {
	    }

	    public ServiceBusMessage(Message innerMessage)
	    {
		    InnerMessage = innerMessage;
	    }

	    #region Implementation of IMessage

	    public override string Identifier
	    {
		    get => InnerMessage.MessageId;
		    set => InnerMessage.MessageId = value;
	    }

	    public override string Label
	    {
		    get => InnerMessage.Label;
		    set => InnerMessage.Label = value;
	    }

	    public override string TargetSession
	    {
		    get => InnerMessage.SessionId;
		    set => InnerMessage.SessionId = value;
	    }

	    public override string To
	    {
		    get => InnerMessage.To;
		    set => InnerMessage.To = value;
	    }

	    public override string ReplyToEntity
	    {
		    get => InnerMessage.ReplyTo;
		    set => InnerMessage.ReplyTo = value;
	    }

	    public override string ReplyToSession
	    {
		    get => InnerMessage.ReplyToSessionId;
		    set => InnerMessage.ReplyToSessionId = value;
	    }

	    public override string BodyType 
	    {
		    get => InnerMessage.ContentType;
		    set => InnerMessage.ContentType = value;
	    }

	    public override byte[] Body 
	    {
		    get => InnerMessage.Body;
		    set => InnerMessage.Body = value;
	    }

	    public override long BodySize => InnerMessage.Size;


	    public override TimeSpan TimeToLive
	    {
		    get => InnerMessage.TimeToLive;
		    set => InnerMessage.TimeToLive = value;
	    }

	    public override DateTime EnqueuedTimeUtc => InnerMessage.SystemProperties.EnqueuedTimeUtc;

	    public override string ResponseTo
	    {
		    get => InnerMessage.CorrelationId;
		    set => InnerMessage.CorrelationId = value;
	    }

	    public override IDictionary<string, object> Properties => InnerMessage.UserProperties;

	    #endregion
    }
}
