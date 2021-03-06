﻿using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;

namespace Apollo.Common
{
    public static class ApolloExtensions
    {
	    public static string GetMessageBodyAsString(this IMessage message) => Encoding.UTF8.GetString(message.Body);
	    public static void SetMessageBodyAsString(this IMessage message, string body) => message.Body = Encoding.UTF8.GetBytes(body);

	    public static string GetStringProperty(this IMessage message, string key)
	    {
			return message.Properties.TryGetValue(key, out var value) 
				? value.ToString()
				: null;
	    }

	    public static void CopyPropertiesFrom(this IMessage target, IMessage source, params string[] propertyKeys)
	    {
		    if (target == null) throw new ArgumentNullException(nameof(target));
		    if (source == null) throw new ArgumentNullException(nameof(source));
		    if (propertyKeys == null || !propertyKeys.Any())
		    {
			    foreach (var property in source.Properties.Keys.Where(k => !k.StartsWith("x-opt-")))
				    target.Properties[property] = source.Properties[property];
		    }
		    else
		    {
			    foreach (var property in propertyKeys)
				    target.Properties[property] = source.Properties[property];
		    }
	    }

		
	    public static T GetState<T>(this IServiceCommunicator communicator, string key)
	    {
		    return communicator.State.TryGetValue(key, out var value)
			    ? (T)value
			    : default(T);
	    }

	    public static bool LabelMatches(this IMessage message, string label) => StringComparer.OrdinalIgnoreCase.Equals(message.Label.Trim(), label);

	    public static async Task<IMessage> WaitForSingleReplyAsync(this IServiceCommunicator communicator, IMessage message, CancellationToken? token = null, TimeSpan? timeout = null)
	    {
		    var options = ReplyOptions.WaitForSingleReply(message);
		    options.CancelToken = token;
			if (timeout != null)
				options.Timeout = timeout.Value;
			return (await communicator.WaitForRepliesAsync(options)).FirstOrDefault();
	    }

	    public static bool IsTerminatingMessage(this IMessage message) => message.LabelMatches(ApolloConstants.EndOfMessages);

	    public static bool IsPositiveAcknowledgement(this IMessage message) => message.LabelMatches(ApolloConstants.PositiveAcknowledgement);

	    public static bool IsNegativeAcknowledgement(this IMessage message) => message.LabelMatches(ApolloConstants.NegativeAcknowledgement);

	    public static void ThrowIfNegativeAcknowledgement(this IMessage message)
	    {
		    if (message.IsNegativeAcknowledgement())
			    throw new NakException(message);
	    }

		public static string GetReasonOrPlaceholder(this IMessage message) => message.GetReason() ?? "<Reason not specified>";

	    public static string GetReason(this IMessage message) => message.GetStringProperty("Reason");

	    public static string GetTargetAlias(this IMessage message) => message.GetStringProperty(ApolloConstants.TargetAliasKey);

	    public static void SetTargetAlias(this IMessage message, string value) => message[ApolloConstants.TargetAliasKey] = value;
    }
}
