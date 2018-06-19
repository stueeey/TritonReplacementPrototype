using System;

namespace Soei.Triton2.Common.Infrastructure
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

	    public static readonly TimeSpan MaximumReplyWaitTime = TimeSpan.FromHours(1);
    }
}
