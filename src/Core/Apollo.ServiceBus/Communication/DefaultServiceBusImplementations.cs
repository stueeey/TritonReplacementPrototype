using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Apollo.ServiceBus.Communication
{
    public class DefaultServiceBusImplementations : IServiceBusImplementations
    {
	    private readonly ServiceBusConfiguration _configuration;
	    public DefaultServiceBusImplementations(ServiceBusConfiguration configuration)
	    {
		    _configuration = configuration;
		    Recreate().Wait();
	    }

	    #region Implementation of IServiceBusImplementations

	    public async Task Recreate()
	    {
		    if ((RegistrationListener?.IsValueCreated ?? false) && !RegistrationListener.Value.IsClosedOrClosing)
			    await RegistrationListener.Value.CloseAsync();
		    if ((RegistrationSender?.IsValueCreated ?? false) && !RegistrationSender.Value.IsClosedOrClosing)
			    await RegistrationSender.Value.CloseAsync();
		    if ((ServerQueueListener?.IsValueCreated ?? false) && !ServerQueueListener.Value.IsClosedOrClosing)
			    await ServerQueueListener.Value.CloseAsync();
		    if ((ServerQueueSender?.IsValueCreated ?? false) && !ServerQueueSender.Value.IsClosedOrClosing)
			    await ServerQueueSender.Value.CloseAsync();
		    if ((ClientSessionListener?.IsValueCreated ?? false) && !ClientSessionListener.Value.IsClosedOrClosing)
			    await ClientSessionListener.Value.CloseAsync();
		    if ((ClientSessionSender?.IsValueCreated ?? false) && !ClientSessionSender.Value.IsClosedOrClosing)
			    await ClientSessionSender.Value.CloseAsync();
		    if ((AliasQueueListener?.IsValueCreated ?? false) && !AliasQueueListener.Value.IsClosedOrClosing)
			    await AliasQueueListener.Value.CloseAsync();
		    if ((AliasQueueSender?.IsValueCreated ?? false) && !AliasQueueSender.Value.IsClosedOrClosing)
			    await AliasQueueSender.Value.CloseAsync();

		    RegistrationListener  = new Lazy<IMessageReceiver>(() => new MessageReceiver( _configuration.Connection, _configuration.RegistrationQueue,      ReceiveMode.PeekLock, _configuration.Connection.RetryPolicy));
		    RegistrationSender    = new Lazy<IMessageSender>  (() => new MessageSender(   _configuration.Connection, _configuration.RegistrationQueue,      _configuration.Connection.RetryPolicy));
		    ServerQueueListener   = new Lazy<IMessageReceiver>(() => new MessageReceiver( _configuration.Connection, _configuration.ServerRequestsQueue,    ReceiveMode.PeekLock, _configuration.Connection.RetryPolicy));
		    ServerQueueSender     = new Lazy<IMessageSender>  (() => new MessageSender(   _configuration.Connection, _configuration.ServerRequestsQueue,    _configuration.Connection.RetryPolicy));
		    ClientSessionListener = new Lazy<ISessionClient>  (() => new SessionClient(   _configuration.Connection, _configuration.RegisteredClientsQueue, ReceiveMode.PeekLock, _configuration.Connection.RetryPolicy));
		    ClientSessionSender   = new Lazy<IMessageSender>  (() => new MessageSender(   _configuration.Connection, _configuration.RegisteredClientsQueue, _configuration.Connection.RetryPolicy));
		    AliasQueueListener    = new Lazy<IMessageReceiver>(() => new MessageReceiver( _configuration.Connection, _configuration.ClientAliasesQueue,     ReceiveMode.PeekLock, _configuration.Connection.RetryPolicy));
		    AliasQueueSender	  = new Lazy<IMessageSender>  (() => new MessageSender(   _configuration.Connection, _configuration.ClientAliasesQueue,     _configuration.Connection.RetryPolicy));
	    }

	    public Lazy<IMessageReceiver> RegistrationListener { get; set;}
	    public Lazy<IMessageSender> RegistrationSender { get;  set;}
	    public Lazy<IMessageReceiver> ServerQueueListener { get; set; }
	    public Lazy<IMessageSender> ServerQueueSender { get; set; }
	    public Lazy<ISessionClient> ClientSessionListener { get; set; }
	    public Lazy<IMessageSender> ClientSessionSender { get; set; }
		public Lazy<IMessageReceiver> AliasQueueListener { get; set; }
		public Lazy<IMessageSender> AliasQueueSender { get; set; }

		#endregion
	}
}
