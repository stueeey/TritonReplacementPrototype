using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Soei.Triton2.ServiceBus.Communication
{
    public class DefaultServiceBusImplementations : IServiceBusImplementations
    {
	    public DefaultServiceBusImplementations(ServiceBusConfiguration configuration)
	    {
		    RegistrationSender    = new Lazy<IMessageSender>(   () => new MessageSender(configuration.Connection, configuration.AnnouncementTopic, RetryPolicy.Default));
		    RegistrationListener  = new Lazy<IMessageReceiver>( () => new MessageReceiver(configuration.Connection, EntityNameHelper.FormatSubscriptionPath(configuration.AnnouncementTopic, configuration.RegistrationQueue), ReceiveMode.PeekLock, RetryPolicy.Default));
		    ServerQueueListener   = new Lazy<IMessageReceiver>( () => new MessageReceiver(configuration.Connection, configuration.ServerRequestsQueue));
		    ServerQueueSender     = new Lazy<IMessageSender>(   () => new MessageSender(configuration.Connection, configuration.ServerRequestsQueue));
		    ClientSessionListener = new Lazy<ISessionClient>(   () => new SessionClient(configuration.Connection, configuration.RegisteredClientsQueue, ReceiveMode.PeekLock));
		    ClientSessionSender   = new Lazy<IMessageSender>(   () => new MessageSender(configuration.Connection, configuration.RegisteredClientsQueue));
			AliasSessionListener  = new Lazy<IMessageReceiver>(	() => new MessageReceiver(configuration.Connection, configuration.ServerRequestsQueue, ReceiveMode.PeekLock, RetryPolicy.Default));
			AliasSessionSender	  = new Lazy<IMessageSender>(   () => new MessageSender(configuration.Connection, configuration.ServerRequestsQueue));
		}

	    #region Implementation of IServiceBusImplementations

	    public Lazy<IMessageReceiver> RegistrationListener { get; }
	    public Lazy<IMessageSender> RegistrationSender { get; }
	    public Lazy<IMessageReceiver> ServerQueueListener { get; }
	    public Lazy<IMessageSender> ServerQueueSender { get; }
	    public Lazy<ISessionClient> ClientSessionListener { get; }
	    public Lazy<IMessageSender> ClientSessionSender { get; }
		public Lazy<IMessageReceiver> AliasSessionListener { get; }
		public Lazy<IMessageSender> AliasSessionSender { get; }

		#endregion
	}
}
