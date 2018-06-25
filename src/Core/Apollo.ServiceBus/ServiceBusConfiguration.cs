using System;
using Apollo.ServiceBus.Infrastructure;
using Microsoft.Azure.ServiceBus;

namespace Apollo.ServiceBus
{
    public class ServiceBusConfiguration
    {
	    private readonly object _synchronisationtoken = new object();
	    public ServiceBusConfiguration(ServiceBusConnectionStringBuilder connectionString, string identifier)
	    {
		    ConnectionStringBuilder = connectionString ?? throw new ArgumentException(nameof(connectionString));
		    Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
		    if (string.IsNullOrWhiteSpace(Identifier))
			    throw new ArgumentException("Client Identifier is empty", nameof(identifier));
		    Reconnect();
	    }

	    public void Reconnect()
	    {
		    lock (_synchronisationtoken)
		    {
			    try
			    {
				    Connection?.CloseAsync().Wait();
					Connection = null;
			    }
			    catch
			    {
					// Suppress
			    }
		    }
		    Connection = new ServiceBusConnection(ConnectionStringBuilder.ToString(), TimeSpan.FromSeconds(15), new RetryExponential(TimeSpan.FromSeconds(0.15), TimeSpan.FromSeconds(60), int.MaxValue));
	    }

	    public ServiceBusConnectionStringBuilder ConnectionStringBuilder { get; }
	    public ServiceBusConnection Connection { get; private set; }
	    public string RegisteredClientsQueue  { get; set; } = ServiceBusConstants.DefaultRegisteredClientsQueue;
	    public string ServerRequestsQueue     { get; set; } = ServiceBusConstants.DefaultServerRequestsQueue;
	    public string RegistrationQueue       { get; set; } = ServiceBusConstants.DefaultRegistrationQueue;
	    public string ClientAliasesQueue      { get; set; } = ServiceBusConstants.DefaultClientAliasesQueue;

	    public string Identifier { get; set; }
    }
}
