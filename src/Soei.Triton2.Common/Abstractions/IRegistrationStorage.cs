using System;
using System.Collections.Generic;

namespace Soei.Triton2.Common.Abstractions
{
	public interface IRegistrationStorage
	{
		bool SaveRegistration(Guid identifier, IDictionary<string, string> metadata);
		bool CheckOwnership(string alias, Guid token, Guid candidateIdentifier);
		bool TakeOwnership(string alias, Guid token, Guid candidateIdentifier);
	}
}