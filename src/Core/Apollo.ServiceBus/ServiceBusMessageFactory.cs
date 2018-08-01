using System;
using Apollo.Common;
using Apollo.Common.Abstractions;

namespace Apollo.ServiceBus
{
    public class ServiceBusMessageFactory : IMessageFactory
    {
	    private string ReplyEntity { get; }
	    private string ReplySession { get; }

	    private void SetReplyAddress(IMessage message)
	    {
		    message.ReplyToEntity = ReplyEntity;
		    message.ReplyToSession = ReplySession;
	    }

	    public ServiceBusMessageFactory(string replyEntity, string replySession)
	    {
		    ReplyEntity = replyEntity;
		    ReplySession = replySession;
	    }

	    #region Implementation of IMessageFactory

	    public IMessage CreateNewMessage(string label = null)
	    {
		    var message = new ServiceBusMessage
		    {
			    Label = label,
				Identifier = Guid.NewGuid().ToString()
		    };
		    SetReplyAddress(message);
		    return message;
	    }

	    public IMessage CreateReply(IMessage receivedMessage, string label = null)
	    {
		    var response = CreateNewMessage(label);
		    response.TargetSession = receivedMessage.ReplyToSession;
		    response.ResponseTo = receivedMessage.Identifier;
		    return response;
	    }

	    public IMessage CreateAcknowledgment(IMessage receivedMessage) => CreateReply(receivedMessage, ApolloConstants.PositiveAcknowledgement);

	    public IMessage CreateNegativeAcknowledgment(IMessage receivedMessage, string reason)
	    {
		    var response = CreateReply(receivedMessage, ApolloConstants.NegativeAcknowledgement);
			response["Reason"] = reason;
		    return response;
	    }

	    public IMessage CloneMessage(IMessage message)
	    {
		    var clone = new ServiceBusMessage
		    { 
			    Identifier = message.Identifier, 
			    Body = message.Body, 
			    BodyType = message.BodyType, 
			    Label = message.Label, 
			    ReplyToEntity = message.ReplyToEntity, 
			    ReplyToSession = message.ReplyToSession, 
			    ResponseTo = message.ResponseTo, 
			    TargetSession = message.TargetSession, 
			    TimeToLive = message.TimeToLive, 
			    To = message.To
		    };
		    clone.CopyPropertiesFrom(message);
		    return clone;
	    }

	    #endregion
    }
}
