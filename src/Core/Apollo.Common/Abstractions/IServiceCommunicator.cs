using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Abstractions
{
	public delegate void PluginEventDelegate(string eventName, object state);
	public delegate void OnMessageReceived(IMessage message, ApolloQueue queue);
	public delegate void OnMessageSent(IMessage message, ApolloQueue queue);

	public interface IServiceCommunicator : IDisposable
	{
		ConcurrentDictionary<string, object> State { get; }
		void SignalPluginEvent(string eventName, object state = null);

		IMessageFactory MessageFactory { get; }
		void RemoveListenersForPlugin(ApolloPlugin plugin);
		void AddHandler(ApolloQueue queueType, MessageHandler handler);
		void RemoveHandler(ApolloQueue queueType, MessageHandler handler);

		Task SendToClientAsync(params IMessage[] messages);
		Task SendToServerAsync(params IMessage[] messages);
		Task SendToRegistrationsAsync(params IMessage[] messages);
		Task SendToAliasAsync(string alias, params IMessage[] messages);

		bool ListeningForClientSessionMessages { get; }
		bool ListeningForRegistrations { get; }
		bool ListeningForServerJobs { get; }
		bool ListeningForAliasMessages { get; }

		event PluginEventDelegate PluginEvent;
		event OnMessageSent AnyMessageSent;
		event OnMessageReceived AnyMessageReceived;

		Task<ICollection<IMessage>> WaitForRepliesAsync(ReplyOptions options);
	}
}