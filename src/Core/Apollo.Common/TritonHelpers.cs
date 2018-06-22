using Microsoft.Win32;
using System;
using System.Linq;
using System.Text;
using Soei.Apollo.Common.Abstractions;

namespace Soei.Apollo.Common
{
    public static class TritonHelpers
    {
		private static string OsInstallIdentifier => Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion", "ProductId", "<<Unable To Read>>").ToString();
	    public static string GetMachineIdentifier() => $"{OsInstallIdentifier}|{Environment.MachineName}";

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
    }
}
