﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Soei.Triton2.Common.Abstractions
{
    public interface ITritonClient : ITritonClientBase
    {
	    Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token);
	    Task<string> RegisterAsync(IDictionary<string, string> metadata = null, TimeSpan? timeout = null);
    }
}
