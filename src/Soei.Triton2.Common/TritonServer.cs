using System;
using System.Collections.Generic;
using Soei.Triton2.Common.Abstractions;
using Soei.Triton2.Common.Infrastructure;
using Soei.Triton2.Common.Plugins;

namespace Soei.Triton2.Common
{
	public class InMemoryDatabaseRepository : IRegistrationStorage
	{
		#region Implementation of IRegistrationStorage

		public bool SaveRegistration(Guid identifier, IDictionary<string, string> metadata)
		{
			throw new NotImplementedException();
		}

		public bool CheckOwnership(string alias, Guid token, Guid candidateIdentifier)
		{
			throw new NotImplementedException();
		}

		public bool TakeOwnership(string alias, Guid token, Guid candidateIdentifier)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

    public class TritonServer : TritonClientBase, ITritonServer
    {
	    public TritonServer(IServiceCommunicator communicator, IRegistrationStorage storage) : base(communicator)
	    {
		    LoadCorePlugin(storage);
	    }

	    private void LoadCorePlugin(IRegistrationStorage storage)
	    {
		    if (GetPlugin<ServerCorePlugin>() == null) 
			    LoadPlugins(new ServerCorePlugin(storage));
	    }

	    public TritonServer(IServiceCommunicator communicator, IRegistrationStorage storage, params TritonPluginBase[] tritonPluginsBase) : base(communicator, tritonPluginsBase)
	    {
		    LoadCorePlugin(storage);
	    }

	    public TritonServer(IServiceCommunicator communicator, IRegistrationStorage storage, PluginCollection tritonPluginsBase) : base(communicator, tritonPluginsBase.ToArray())
	    {
		    LoadCorePlugin(storage);
	    }
    }
}
