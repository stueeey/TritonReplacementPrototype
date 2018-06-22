using System;
using System.Collections.Generic;

namespace Soei.Apollo.Common.Abstractions
{
	public interface IRegistrationStorage
	{
		bool SaveRegistration(string identifier, IDictionary<string, string> metadata);
		bool CheckOwnership(string alias, Guid token, string candidateIdentifier);
		string TakeOwnership(string alias, Guid token, string candidateIdentifier);
		string GetAliasOwner(string alias);
	}
}