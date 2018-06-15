namespace Soei.Triton2.Common.Abstractions
{
    public interface IMessageFactory
    {
	    IMessage CreateNewMessage(string label = null);
	    IMessage CreateReply(IMessage receivedMessage);
	    IMessage CreateAcknowledgment(IMessage receivedMessage);
	    IMessage CreateNegativeAcknowledgment(IMessage receivedMessage);
    }
}
