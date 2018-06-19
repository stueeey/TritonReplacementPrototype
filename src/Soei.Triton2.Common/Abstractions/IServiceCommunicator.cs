using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Soei.Triton2.Common.Infrastructure;

namespace Soei.Triton2.Common.Abstractions
{
	public delegate void OnMessageReceivedDelegate(IMessage message, ref MessageReceivedEventArgs e);
	public delegate void PluginEventDelegate(string eventName, object state);

	public interface IServiceCommunicator : IDisposable
	{
		ConcurrentDictionary<string, object> State { get; }
		T GetState<T>(string key);
		void SignalPluginEvent(string eventName, object state = null);

		IMessageFactory MessageFactory { get; }
		void RemoveListenersForPlugin(TritonPluginBase plugin);

		Task SendToClientAsync(IMessage message);
		Task SendToClientAsync(params IMessage[] messages);
		Task SendToServerAsync(IMessage message);
		Task SendToServerAsync(params IMessage[] messages);
		Task SendRegistrationMessageAsync(IMessage message);
		Task SendRegistrationMessageAsync(params IMessage[] messages);
		Task SendToAliasAsync(string alias, IMessage message);
		Task SendToAliasAsync(string alias, params IMessage[] messages);

		bool ListenForClientSessionMessages { get; }
		bool ListenForRegistrations { get; }
		bool ListenForServerJobs { get; }
		bool ListenForAliasMessages { get; }

		event PluginEventDelegate PluginEvent;
		event OnMessageReceivedDelegate ClientSessionMessageReceived;
		event OnMessageReceivedDelegate RegistrationReceived;
		event OnMessageReceivedDelegate ServerJobReceived;
		event OnMessageReceivedDelegate AliasMessageReceived;
		Task<IMessage> WaitForReplyTo(IMessage message, CancellationToken? token = null, TimeSpan? timeout = null);
	}
}