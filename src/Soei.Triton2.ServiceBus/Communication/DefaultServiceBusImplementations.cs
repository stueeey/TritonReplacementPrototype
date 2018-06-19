using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Soei.Triton2.ServiceBus.Communication
{
    public class DefaultServiceBusImplementations : IServiceBusImplementations
    {
	    public DefaultServiceBusImplementations(ServiceBusConfiguration configuration)
	    {
		    RegistrationListener  = new Lazy<IMessageReceiver>(() => new MessageReceiver( configuration.Connection, configuration.RegistrationQueue,      ReceiveMode.PeekLock, RetryPolicy.Default));
		    RegistrationSender    = new Lazy<IMessageSender>  (() => new MessageSender(   configuration.Connection, configuration.RegistrationQueue,      RetryPolicy.Default));
		    ServerQueueListener   = new Lazy<IMessageReceiver>(() => new MessageReceiver( configuration.Connection, configuration.ServerRequestsQueue,    ReceiveMode.PeekLock, RetryPolicy.Default));
		    ServerQueueSender     = new Lazy<IMessageSender>  (() => new MessageSender(   configuration.Connection, configuration.ServerRequestsQueue,    RetryPolicy.Default));
		    ClientSessionListener = new Lazy<ISessionClient>  (() => new SessionClient(   configuration.Connection, configuration.RegisteredClientsQueue, ReceiveMode.PeekLock, RetryPolicy.Default));
		    ClientSessionSender   = new Lazy<IMessageSender>  (() => new MessageSender(   configuration.Connection, configuration.RegisteredClientsQueue, RetryPolicy.Default));
			AliasQueueListener    = new Lazy<IMessageReceiver>(() => new MessageReceiver( configuration.Connection, configuration.ClientAliasesQueue,     ReceiveMode.PeekLock, RetryPolicy.Default));
			AliasQueueSender	  = new Lazy<IMessageSender>  (() => new MessageSender(   configuration.Connection, configuration.ClientAliasesQueue,     RetryPolicy.Default));
		}

	    #region Implementation of IServiceBusImplementations

	    public Lazy<IMessageReceiver> RegistrationListener { get; }
	    public Lazy<IMessageSender> RegistrationSender { get; }
	    public Lazy<IMessageReceiver> ServerQueueListener { get; }
	    public Lazy<IMessageSender> ServerQueueSender { get; }
	    public Lazy<ISessionClient> ClientSessionListener { get; }
	    public Lazy<IMessageSender> ClientSessionSender { get; }
		public Lazy<IMessageReceiver> AliasQueueListener { get; }
		public Lazy<IMessageSender> AliasQueueSender { get; }

		#endregion
	}
}
