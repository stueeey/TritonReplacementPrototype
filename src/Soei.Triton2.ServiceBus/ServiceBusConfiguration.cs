using System;
using Microsoft.Azure.ServiceBus;
using Soei.Triton2.ServiceBus.Infrastructure;

namespace Soei.Triton2.ServiceBus
{
    public class ServiceBusConfiguration
    {
	    public ServiceBusConfiguration(ServiceBusConnection connection)
	    {
		    Connection = connection ?? throw new ArgumentNullException(nameof(connection));
	    }

	    public ServiceBusConfiguration(ServiceBusConnection connection, string clientIdentifier)
	    {
		    Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		    ClientIdentifier = clientIdentifier ?? throw new ArgumentNullException(nameof(clientIdentifier));
		    if (string.IsNullOrWhiteSpace(ClientIdentifier))
			    throw new ArgumentException("Client Identifier is empty", nameof(clientIdentifier));
	    }

	    public ServiceBusConnection Connection { get; }
	    public string RegisteredClientsQueue  { get; set; } = ServiceBusConstants.DefaultRegisteredClientsQueue;
	    public string ServerRequestsQueue     { get; set; } = ServiceBusConstants.DefaultServerRequestsQueue;
	    public string RegistrationQueue       { get; set; } = ServiceBusConstants.DefaultRegistrationQueue;
	    public string ClientAliasesQueue      { get; set; } = ServiceBusConstants.DefaultClientAliasesQueue;

	    public string ClientIdentifier { get; set; }
    }
}
