using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Soei.Triton2.ServiceBus.Infrastructure;

namespace Soei.Triton2.ServiceBus.Communication
{
    public interface IServiceBusImplementations
    {
	    Lazy<IMessageReceiver> RegistrationListener { get; }
	    Lazy<IMessageSender> RegistrationSender { get; }
	    Lazy<IMessageReceiver> ServerQueueListener { get; }
	    Lazy<IMessageSender> ServerQueueSender { get; }
	    Lazy<ISessionClient> ClientSessionListener { get; }
	    Lazy<IMessageSender> ClientSessionSender { get; }
    }
}
