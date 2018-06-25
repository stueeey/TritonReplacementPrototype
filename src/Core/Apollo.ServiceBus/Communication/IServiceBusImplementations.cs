using System;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace Apollo.ServiceBus.Communication
{
    public interface IServiceBusImplementations
    {
	    Task Recreate();

	    Lazy<IMessageReceiver> RegistrationListener { get; }
	    Lazy<IMessageSender> RegistrationSender { get; }
	    Lazy<IMessageReceiver> ServerQueueListener { get; }
	    Lazy<IMessageSender> ServerQueueSender { get; }
	    Lazy<ISessionClient> ClientSessionListener { get; }
	    Lazy<IMessageSender> ClientSessionSender { get; }
		Lazy<IMessageReceiver> AliasQueueListener { get; }
		Lazy<IMessageSender> AliasQueueSender { get; }
	}
}
