namespace Apollo.Common.Abstractions
{
    public interface IMessageFactory
    {
	    IMessage CreateNewMessage(string label = null);
	    IMessage CreateReply(IMessage receivedMessage);
	    IMessage CreateAcknowledgment(IMessage receivedMessage);
	    IMessage CreateNegativeAcknowledgment(IMessage receivedMessage, string reason);
	    IMessage CloneMessage(IMessage message);
    }
}
