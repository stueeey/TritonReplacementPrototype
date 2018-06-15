using System;
using System.Threading.Tasks;

namespace Soei.Triton2.Common.Abstractions
{
    public interface ITritonClient : ITritonClientBase
    {
	    Task<Guid> RequestOwnershipOfAliasAsync(string alias, Guid token);
    }
}
