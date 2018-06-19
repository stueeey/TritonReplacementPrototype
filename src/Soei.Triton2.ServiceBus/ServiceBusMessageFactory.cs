using System;
using Soei.Triton2.Common;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.ServiceBus
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

	    public IMessage CreateReply(IMessage receivedMessage)
	    {
		    var response = CreateNewMessage();
		    response.TargetSession = receivedMessage.ReplyToSession;
		    response.ResponseTo = receivedMessage.Identifier;
		    return response;
	    }

	    public IMessage CreateAcknowledgment(IMessage receivedMessage)
	    {
		    var response = CreateReply(receivedMessage);
		    response.Label = TritonConstants.PositiveAcknowledgement;
		    return response;
	    }

	    public IMessage CreateNegativeAcknowledgment(IMessage receivedMessage, string reason)
	    {
		    var response = CreateReply(receivedMessage);
		    response.Label = TritonConstants.NegativeAcknowledgement;
			response["Reason"] = reason;
		    return response;
	    }

	    #endregion
    }
}
