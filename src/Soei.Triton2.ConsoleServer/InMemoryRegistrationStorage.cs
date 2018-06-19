using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Soei.Triton2.Common.Abstractions;

namespace Soei.Triton2.ConsoleServer
{
	public class InMemoryRegistrationStorage : IRegistrationStorage
	{
		private class AliasDetails
		{
			public Guid Token { get; set; }
			public string Owner { get; set; }
		}
		#region Implementation of IRegistrationStorage

		private readonly ConcurrentDictionary<string, IDictionary<string, string>> RegisteredClients = new ConcurrentDictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		private readonly ConcurrentDictionary<string, AliasDetails> RegisteredAliases = new ConcurrentDictionary<string, AliasDetails>(StringComparer.OrdinalIgnoreCase);

		public bool SaveRegistration(string identifier, IDictionary<string, string> metadata)
		{
			if (string.IsNullOrWhiteSpace(identifier))
				return false;
			RegisteredClients.AddOrUpdate(identifier, metadata, (guid, oldValue) => metadata);
			return true;
		}

		public bool CheckOwnership(string alias, Guid token, string candidateIdentifier)
		{
			if (token == Guid.Empty || string.IsNullOrWhiteSpace(candidateIdentifier))
				return false;
			if (RegisteredAliases.TryGetValue(alias, out var aliasDetails))
				return aliasDetails.Token == token;
			return RegisteredAliases.TryAdd(alias, new AliasDetails {Owner = candidateIdentifier, Token = token});

		}

		public string TakeOwnership(string alias, Guid token, string candidateIdentifier)
		{
			if (token == Guid.Empty || string.IsNullOrWhiteSpace(candidateIdentifier))
				return null;
			var newValue = new AliasDetails {Owner = candidateIdentifier, Token = token};
			if (RegisteredAliases.TryGetValue(alias, out var registeredAlias))
			{
				RegisteredAliases.TryUpdate(alias, newValue, registeredAlias);
				return registeredAlias.Owner;
			}
			RegisteredAliases.TryAdd(alias, newValue);
			return null;
		}

		#endregion
	}
}