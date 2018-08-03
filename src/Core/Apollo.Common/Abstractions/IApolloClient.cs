using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Apollo.Common.Abstractions
{
    public interface IApolloClient : IApolloClientBase
    {
	    /// <summary>
	    /// Asks nicely for ownership of an alias
	    /// Ownership will only be granted if it is not owned or the token
	    /// matches the one on the server
	    /// </summary>
	    /// <param name="alias">The alias to request ownership of</param>
	    /// <param name="token">The token that should be used to prove ownership</param>
	    /// <exception cref="TimeoutException">Thrown if no response is received</exception>
	    /// <returns>A guid representing the secret for this alias or a blank guid if the request is denied</returns>
	    Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token);

	    /// <summary>
	    /// Forcibly takes ownership of an alias
	    /// </summary>
	    /// <param name="alias">The alias to demand ownership of</param>
	    /// <param name="token">The token which should be used to prove ownership in future requests</param>
	    /// <exception cref="TimeoutException">Thrown if no response is received</exception>
	    /// <returns><see cref="Guid.Empty"/> if the demand is denied, otherwise the ownership token for the alias</returns>
	    Task<Guid> DemandOwnershipOfAliasAsync(string alias, Guid token);

	    /// <summary>
	    /// Registers with the server so that it knows we exist
	    /// </summary>
	    /// <param name="metadata">Information about this client</param>
	    /// <param name="cancellationToken">A token which can be used to cancel waiting</param>
	    /// <returns>An awaitable task which will return the client's ID</returns>
	    /// <exception cref="TimeoutException">If there is no reply</exception>
	    /// <exception cref="NakException">If the server says no</exception>
	    Task<string> RegisterAsync(IDictionary<string, string> metadata = null, CancellationToken? cancellationToken = null);

	    /// <summary>
	    /// Fired if we lose ownership of an alias
	    /// </summary>
	    event Action<string> LostOwnershipOfAlias;
    }
}
