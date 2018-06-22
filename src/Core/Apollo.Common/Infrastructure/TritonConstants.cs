using System;

namespace Soei.Apollo.Common.Infrastructure
{
    public static class TritonConstants
    {
	    public const string ConnectionKey = "ServiceBusConnectionKey";

		// Loggers
	    public const string LoggerPrefix = "TritonLogger";
	    public const string LoggerInternalsPrefix = LoggerPrefix + ".Internals";
	    public const string LoggerPluginsPrefix = LoggerPrefix + ".Plugins";
		
		// Standard message labels
	    public const string PositiveAcknowledgement = "ACK";
	    public const string NegativeAcknowledgement = "NACK";
	    public const string RegistrationKey = "Registration";
	    public const string RegisteredAsKey = "Registered As";
	    public const string TargetAliasKey = "Target Alias";

		// Standard state labels
	    public const string NumberOfMessagesSentToRegistrations   = "# Messages Sent to Registrations";
	    public const string NumberOfMessagesSentToServerRequests  = "# Messages Sent to ServerRequests";
	    public const string NumberOfMessagesSentToAliases         = "# Messages Sent to Aliases";
	    public const string NumberOfMessagesSentToClientSessions  = "# Messages Sent to ClientSessions";

	    public const string NumberOfMessagesReceivedFromRegistrations   = "# Messages received from Registrations";
	    public const string NumberOfMessagesReceivedFromServerRequests  = "# Messages received from ServerRequests";
	    public const string NumberOfMessagesReceivedFromAliases         = "# Messages received from Aliases";
	    public const string NumberOfMessagesReceivedFromClientSessions  = "# Messages received from ClientSessions";

	    public static readonly TimeSpan MaximumReplyWaitTime = TimeSpan.FromHours(1);
    }
}
