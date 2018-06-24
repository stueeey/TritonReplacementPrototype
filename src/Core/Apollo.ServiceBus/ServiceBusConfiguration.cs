using System;
using Apollo.ServiceBus.Infrastructure;
using Microsoft.Azure.ServiceBus;

namespace Apollo.ServiceBus
{
    public class ServiceBusConfiguration
    {
	    public ServiceBusConfiguration(ServiceBusConnection connection)
	    {
		    Connection = connection ?? throw new ArgumentNullException(nameof(connection));
	    }

	    public ServiceBusConfiguration(ServiceBusConnectionStringBuilder connectionString, string clientIdentifier)
	    {
		    ConnectionStringBuilder = connectionString ?? throw new ArgumentException(nameof(connectionString));
		    Connection = new ServiceBusConnection(ConnectionStringBuilder.ToString(), TimeSpan.FromSeconds(60), new RetryExponential(TimeSpan.FromSeconds(0.15), TimeSpan.FromSeconds(60), int.MaxValue));
		    ClientIdentifier = clientIdentifier ?? throw new ArgumentNullException(nameof(clientIdentifier));
		    if (string.IsNullOrWhiteSpace(ClientIdentifier))
			    throw new ArgumentException("Client Identifier is empty", nameof(clientIdentifier));


	    }

	    public ServiceBusConnectionStringBuilder ConnectionStringBuilder { get; }
	    public ServiceBusConnection Connection { get; }
	    public string RegisteredClientsQueue  { get; set; } = ServiceBusConstants.DefaultRegisteredClientsQueue;
	    public string ServerRequestsQueue     { get; set; } = ServiceBusConstants.DefaultServerRequestsQueue;
	    public string RegistrationQueue       { get; set; } = ServiceBusConstants.DefaultRegistrationQueue;
	    public string ClientAliasesQueue      { get; set; } = ServiceBusConstants.DefaultClientAliasesQueue;

	    public string ClientIdentifier { get; set; }
    }
}
