using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Infrastructure;

namespace Apollo.Common.Abstractions
{
	public delegate void OnMessageReceivedDelegate(IMessage message, ref MessageReceivedEventArgs e);
	public delegate void PluginEventDelegate(string eventName, object state);
	public delegate void OnMessageReceived(IMessage message, ApolloQueue queue);
	public delegate void OnMessageSent(IMessage message, ApolloQueue queue);

	public interface IServiceCommunicator : IDisposable
	{
		ConcurrentDictionary<string, object> State { get; }
		T GetState<T>(string key);
		void SignalPluginEvent(string eventName, object state = null);

		IMessageFactory MessageFactory { get; }
		void RemoveListenersForPlugin(ApolloPluginBase plugin);
		void AddHandler(ApolloQueue queueType, MessageHandler handler);
		void RemoveHandler(ApolloQueue queueType, MessageHandler handler);

		Task SendToClientAsync(IMessage message, CancellationToken? token = null);
		Task SendToClientAsync(CancellationToken? token, params IMessage[] messages);
		Task SendToClientAsync(params IMessage[] messages);

		Task SendToServerAsync(IMessage message, CancellationToken? token = null);
		Task SendToServerAsync(CancellationToken? token, params IMessage[] messages);
		Task SendToServerAsync(params IMessage[] messages);

		Task SendRegistrationMessageAsync(IMessage message, CancellationToken? token = null);
		Task SendRegistrationMessageAsync(CancellationToken? token, params IMessage[] messages);
		Task SendRegistrationMessageAsync(params IMessage[] messages);

		Task SendToAliasAsync(string alias, IMessage message, CancellationToken? token = null);
		Task SendToAliasAsync(string alias, CancellationToken? token, params IMessage[] messages);
		Task SendToAliasAsync(string alias, params IMessage[] messages);

		bool ListeningForClientSessionMessages { get; }
		bool ListeningForRegistrations { get; }
		bool ListeningForServerJobs { get; }
		bool ListeningForAliasMessages { get; }

		event PluginEventDelegate PluginEvent;
		event OnMessageReceivedDelegate ClientSessionMessageReceived;
		event OnMessageReceivedDelegate RegistrationReceived;
		event OnMessageReceivedDelegate ServerJobReceived;
		event OnMessageReceivedDelegate AliasMessageReceived;
		event OnMessageSent AnyMessageSent;
		event OnMessageReceived AnyMessageReceived;

		Task<IMessage> WaitForReplyTo(IMessage message, CancellationToken? token = null, TimeSpan? timeout = null);
	}
}