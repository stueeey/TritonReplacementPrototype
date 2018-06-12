namespace Soei.Triton2.Common
{
    public interface IMessageFactory
    {
	    IMessage CreateNewMessage(string label = null);
	    IMessage CreateReply(IMessage receivedMessage);
	    IMessage CreateAcknowledgment(IMessage receivedMessage);
	    IMessage NegativeAcknowledgment(IMessage receivedMessage);
    }
}
