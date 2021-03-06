﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Plugins;
using Microsoft.AspNetCore.Mvc;
using Ninject;

namespace Apollo.ServerWorker.Controllers
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
		    public string[] Plugins { get; set; }
		    public KeyValuePair<string, object>[] State { get; set; }
		    public TimeSpan Uptime { get;set; }
	    }

		private readonly IApolloServer _server;

	    public ServerInstanceController(IKernel ioc)
	    {
		    _server = ioc.Get<IApolloServer>();
	    }

	    // GET api/ServerInstance
        [HttpGet]
        public ServerStatus Get()
        {
	        var retVal = new ServerStatus
	        {
		        Identifier = _server.Identifier,
		        ListeningForServerRequests = _server.Communicator.ListeningForServerJobs,
		        ListeningForRegistrationRequests = _server.Communicator.ListeningForRegistrations,
		        ListeningForAliasMessages = _server.Communicator.ListeningForAliasMessages,
		        ListeningForClientSessionMessages = _server.Communicator.ListeningForClientSessionMessages,
				Plugins = _server.GetPlugins().Select(p => p.GetType().Name).ToArray(),
		        State = _server.Communicator.State.OrderBy(e => e.Key).ToArray(),
		        Uptime = DateTime.Now - Process.GetCurrentProcess().StartTime
		        
	        };
	        return retVal;
        }

		[Route("Ping")]
		[HttpGet()]
		public async Task<string> Ping()
		{
			return (await _server.GetPlugin<ServerCorePlugin>().PingServer()).ToString(true);
		}
	}
}
