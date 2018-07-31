using System.Collections.Generic;

namespace Apollo.Common.Infrastructure
{
	public class PluginCollection : List<ApolloPlugin>
	{
		public PluginCollection()
		{
		}

		public PluginCollection(IEnumerable<ApolloPlugin> collection) : base(collection)
		{
		}
	}
}