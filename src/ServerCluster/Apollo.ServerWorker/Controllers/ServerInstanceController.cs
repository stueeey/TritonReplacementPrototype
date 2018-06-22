using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Ninject;
using Soei.Apollo.Common.Abstractions;
using Soei.Apollo.Common.Infrastructure;

namespace Soei.Apollo.ServerWorker.Controllers
{
    [Route("api/[controller]")]
    public class ServerInstanceController : Controller
    {
	    public class ServerStatus
	    {
		    public string Identifier { get;set;}
		    public bool ListeningForServerRequests { get; set; }
		    public bool ListeningForRegistrationRequests { get; set; }
		    public bool ListeningForAliasMessages { get; set; }
		    public bool ListeningForClientSessionMessages { get; set; }
		    public KeyValuePair<string, object>[] State { get; set; }
		    public TimeSpan Uptime { get;set; }
	    }

		private readonly ITritonServer _server;

	    public ServerInstanceController(IKernel ioc)
	    {
		    _server = ioc.Get<ITritonServer>();
	    }

	    // GET api/ServerInstance
        [HttpGet]
        public ServerStatus Get()
        {
	        var retVal = new ServerStatus
	        {
		        Identifier = _server.Identifier,
		        ListeningForServerRequests = _server.Communicator.ListenForServerJobs,
		        ListeningForRegistrationRequests = _server.Communicator.ListenForRegistrations,
		        ListeningForAliasMessages = _server.Communicator.ListenForAliasMessages,
		        ListeningForClientSessionMessages = _server.Communicator.ListenForClientSessionMessages,
		        State = _server.Communicator.State.OrderBy(e => e.Key).ToArray(),
		        Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime
		        
	        };
	        return retVal;
        }
    }
}
