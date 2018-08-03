using System;
using System.Collections.Generic;

namespace Apollo.Common.Abstractions
{
	public interface IApolloServerRepository
	{
		bool SaveRegistration(string identifier, IDictionary<string, string> metadata);
		IDictionary<string, string> LoadRegistration(string identifier);
		bool CheckOwnership(string alias, Guid token, string candidateIdentifier);
		string TakeOwnership(string alias, Guid token, string candidateIdentifier);
		string GetAliasOwner(string alias);
	}
}