using Apollo.Common;
using Apollo.Common.Abstractions;

namespace Apollo.Mocks
{
	public class MockMessageFactory : IMessageFactory
	{

		private string ReplyEntity { get; }
		private string ReplySession { get; }

		private void SetReplyAddress(IMessage message)
		{
			message.ReplyToEntity = ReplyEntity;
			message.ReplyToSession = ReplySession;
		}

		private readonly MockService _service;

		public MockMessageFactory(string replyEntity, string replySession, MockService service)
		{
			ReplyEntity = replyEntity;
			ReplySession = replySession;
			_service = service;
		}

		#region Implementation of IMessageFactory

		public IMessage CreateNewMessage(string label = null)
		{
			string identifier;
			lock (_service)
			{
				identifier = $"{ReplySession.Substring(0, 1)}{ReplySession.Substring(ReplySession.Length - 1)}_{new string((char) ('A' + (_service.MessageCounter % 25)), 1)}";
				_service.MessageCounter++;
			}
			var retVal = new MockMessage
			{
				Identifier = identifier,
				Label = label
			};
			SetReplyAddress(retVal);
			return retVal;
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
			var clone = new MockMessage
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
