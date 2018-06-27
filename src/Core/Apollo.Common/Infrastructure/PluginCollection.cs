using System.Collections.Generic;

namespace Apollo.Common.Infrastructure
{
	public class PluginCollection : List<ApolloPluginBase>
	{
		public PluginCollection()
		{
		}

		public PluginCollection(IEnumerable<ApolloPluginBase> collection) : base(collection)
		{
		}
	}
}