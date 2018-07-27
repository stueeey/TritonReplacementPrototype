﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Apollo.Common.Abstractions;
using Microsoft.Win32;

namespace Apollo.Common
{
    public static class ApolloHelpers
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
    }
}
