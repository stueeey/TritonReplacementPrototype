using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Soei.Apollo.Common.Abstractions;

namespace Soei.Apollo.ServerWorker.Services
{
	public class InMemoryRegistrationStorage : IRegistrationStorage
	{
		private class AliasDetails
		{
			public Guid Token { get; set; }
			public string Owner { get; set; }
		}
		#region Implementation of IRegistrationStorage

		private readonly ConcurrentDictionary<string, IDictionary<string, string>> _registeredClients = new ConcurrentDictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		private readonly ConcurrentDictionary<string, AliasDetails> _registeredAliases = new ConcurrentDictionary<string, AliasDetails>(StringComparer.OrdinalIgnoreCase);

		public bool SaveRegistration(string identifier, IDictionary<string, string> metadata)
		{
			if (string.IsNullOrWhiteSpace(identifier))
				return false;
			_registeredClients.AddOrUpdate(identifier, metadata, (guid, oldValue) => metadata);
			return true;
		}

		public bool CheckOwnership(string alias, Guid token, string candidateIdentifier)
		{
			if (token == Guid.Empty || string.IsNullOrWhiteSpace(candidateIdentifier))
				return false;
			if (_registeredAliases.TryGetValue(alias, out var aliasDetails))
				return aliasDetails.Token == token;
			return _registeredAliases.TryAdd(alias, new AliasDetails {Owner = candidateIdentifier, Token = token});

		}

		public string TakeOwnership(string alias, Guid token, string candidateIdentifier)
		{
			if (token == Guid.Empty || string.IsNullOrWhiteSpace(candidateIdentifier))
				return null;
			var newValue = new AliasDetails {Owner = candidateIdentifier, Token = token};
			if (_registeredAliases.TryGetValue(alias, out var registeredAlias))
			{
				_registeredAliases.TryUpdate(alias, newValue, registeredAlias);
				return registeredAlias.Owner;
			}
			_registeredAliases.TryAdd(alias, newValue);
			return null;
		}

		public string GetAliasOwner(string alias)
		{
			return _registeredAliases.TryGetValue(alias, out var value)
				? value.Owner
				: null;
		}

		#endregion
	}
}