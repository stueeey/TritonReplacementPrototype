﻿using System;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

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
