using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Apollo.Common.Abstractions
{
	/// <summary>
	/// The delegate used to receive plugin events
	/// </summary>
	/// <param name="eventName">The name of the event</param>
	/// <param name="state">The state associated with this event</param>
	public delegate void PluginEventDelegate(string eventName, object state);

	/// <summary>
	/// The delegate used to track when messages are received (for diagnostics and logging)
	/// </summary>
	/// <param name="message">The message received</param>
	/// <param name="queue">The queue the message was received from</param>
	public delegate void OnMessageReceived(IMessage message, ApolloQueue queue);

	/// <summary>
	/// The delegate used to track when messages are sent (for diagnostics and logging)
	/// </summary>
	/// <param name="message">The message received</param>
	/// <param name="queue">The queue the message was received from</param>
	public delegate void OnMessageSent(IMessage message, ApolloQueue queue);

	public interface IServiceCommunicator : IDisposable
	{
		/// <summary>
		/// Stores state for this connection to Apollo
		/// Mainly used to send data between plugins
		/// </summary>
		ConcurrentDictionary<string, object> State { get; }

		/// <summary>
		/// Used to inform other plugins that an event has occurred (e.g. registered)
		/// </summary>
		/// <param name="eventName"></param>
		/// <param name="state"></param>
		void SignalPluginEvent(string eventName, object state = null);

		/// <summary>
		/// Creates appropriate messages for the underlying message broker
		/// </summary>
		IMessageFactory MessageFactory { get; }

		/// <summary>
		/// Removes all handlers added by a given plugin
		/// Does nothing if there are no handlers for this plugin
		/// </summary>
		/// <param name="plugin">The plugin which will be used as the criteria to find and remove the handlers</param>
		void RemoveListenersForPlugin(ApolloPlugin plugin);

		/// <summary>
		/// Adds a message handler which listens for matching messages from the given queue
		/// </summary>
		/// <param name="queueType">The queue to listen to, the <see cref="IServiceCommunicator"/> will start listening to that queue if it is not already</param>
		/// <param name="handler">The handler to attach</param>
		void AddHandler(ApolloQueue queueType, MessageHandler handler);

		/// <summary>
		/// Removes the given message handler from the given queue.
		/// Will stop listening to the queue if there are no handlers left listening to that queue.
		/// Does nothing if the handler does not exist 
		/// </summary>
		/// <param name="queueType">The queue to remove the handler from</param>
		/// <param name="handler">The handler to remove</param>
		void RemoveHandler(ApolloQueue queueType, MessageHandler handler);

		/// <summary>
		/// Sends the given messages to the clients specified in <see cref="IMessage.TargetSession"/>
		/// </summary>
		/// <param name="messages">The messages to send, must have <see cref="IMessage.TargetSession"/> set to a non-blank value</param>
		/// <returns>An awaitable void task</returns>
		/// <exception cref="ArgumentException">If <see cref="IMessage.TargetSession"/> is not set on any of the messages</exception>
		/// <exception cref="InvalidOperationException">If the implementation of <see cref="IMessage"/> is not compatible with the implementation of <see cref="IServiceCommunicator"/></exception>
		Task SendToClientsAsync(params IMessage[] messages);

		/// <summary>
		/// Sends the given messages to the client specified by <paramref name="clientIdentifier"/>
		/// </summary>
		/// <param name="clientIdentifier">The client to send ALL of the messages to</param>
		/// <param name="messages">The messages to send</param>
		/// <returns>An awaitable void task</returns>
		/// <exception cref="ArgumentException">If <paramref name="clientIdentifier"/> is null or whitespace</exception>
		/// <exception cref="InvalidOperationException">If the implementation of <see cref="IMessage"/> is not compatible with the implementation of <see cref="IServiceCommunicator"/></exception>
		Task SendToClientAsync(string clientIdentifier, params IMessage[] messages);

		/// <summary>
		/// Sends the given messages to the server
		/// </summary>
		/// <param name="messages">The messages to send</param>
		/// <returns>An awaitable void task</returns>
		/// <exception cref="InvalidOperationException">If the implementation of <see cref="IMessage"/> is not compatible with the implementation of <see cref="IServiceCommunicator"/></exception>
		Task SendToServerAsync(params IMessage[] messages);

		/// <summary>
		/// Sends the given messages to the registrations queue
		/// </summary>
		/// <param name="messages">The messages to send</param>
		/// <returns>An awaitable void task</returns>
		/// <exception cref="InvalidOperationException">If the implementation of <see cref="IMessage"/> is not compatible with the implementation of <see cref="IServiceCommunicator"/></exception>
		Task SendToRegistrationsAsync(params IMessage[] messages);

		/// <summary>
		/// Sends the given messages to the target aliases. The target alias property must be set prior to sending via <see cref="ApolloExtensions.SetTargetAlias(IMessage, string)"/>
		/// </summary>
		/// <param name="messages">The messages to send</param>
		/// <returns>An awaitable void task</returns>
		/// <exception cref="ArgumentException">If the TargetAlias property is not set on any of the messages</exception>
		/// <exception cref="InvalidOperationException">If the implementation of <see cref="IMessage"/> is not compatible with the implementation of <see cref="IServiceCommunicator"/></exception>
		Task SendToAliasAsync(params IMessage[] messages);

		/// <summary>
		/// Sends the given messages to the client specified by <paramref name="alias"/>
		/// </summary>
		/// <param name="alias">The alias to send ALL of the messages to</param>
		/// <param name="messages">The messages to send</param>
		/// <returns>An awaitable void task</returns>
		/// <exception cref="ArgumentException">If <paramref name="alias"/> is null or whitespace</exception>
		/// <exception cref="InvalidOperationException">If the implementation of <see cref="IMessage"/> is not compatible with the implementation of <see cref="IServiceCommunicator"/></exception>
		Task SendToAliasAsync(string alias, params IMessage[] messages);

		/// <summary>
		/// True if the <see cref="IServiceCommunicator"/> is currently listening to <see cref="ApolloQueue.ClientSessions"/>
		/// </summary>
		bool ListeningForClientSessionMessages { get; }

		/// <summary>
		/// True if the <see cref="IServiceCommunicator"/> is currently listening to <see cref="ApolloQueue.Registrations"/>
		/// </summary>
		bool ListeningForRegistrations { get; }

		/// <summary>
		/// True if the <see cref="IServiceCommunicator"/> is currently listening to <see cref="ApolloQueue.ServerRequests"/>
		/// </summary>
		bool ListeningForServerJobs { get; }

		/// <summary>
		/// True if the <see cref="IServiceCommunicator"/> is currently listening to <see cref="ApolloQueue.Aliases"/>
		/// </summary>
		bool ListeningForAliasMessages { get; }

		/// <summary>
		/// The event which is fired when a plugin wants to inform other plugins of a state change
		/// </summary>
		event PluginEventDelegate PluginEvent;

		/// <summary>
		/// Fired whenever a message is sent (for diagnostics)
		/// </summary>
		event OnMessageSent AnyMessageSent;

		/// <summary>
		/// Fired whenever a message is received (for diagnostics)
		/// </summary>
		event OnMessageReceived AnyMessageReceived;

		/// <summary>
		/// Sets the return address for a given message to the given queue 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="queue"></param>
		/// <param name="recipientIdentifier"></param>
		void SetReplyAddress(IMessage message, ApolloQueue queue, string recipientIdentifier = null);

		Task<ICollection<IMessage>> WaitForRepliesAsync(ReplyOptions options);
	}
}