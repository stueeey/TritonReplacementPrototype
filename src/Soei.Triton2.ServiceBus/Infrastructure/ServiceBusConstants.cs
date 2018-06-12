namespace Soei.Triton2.ServiceBus.Infrastructure
{
    public static class ServiceBusConstants
    {
		// Queues and topics
	    public const string RegisteredClientsQueue = "clientsessions";
	    public const string ServerRequestsQueue = "serverrequests";
	    public const string AnnouncementTopic = "announcements";
	    public const string RegistrationQueue = "registrations";
	    public const string ClientAliasesQueue = "clientaliases";
    }
}
