using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apollo.Common.Plugins;
using Apollo.Mocks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Apollo.Common.Tests
{
	public class CorePluginTests
	{
		private ITestOutputHelper _logger;
		public CorePluginTests(ITestOutputHelper logger)
		{
			_logger = logger;
		}

		[Fact(DisplayName = "When a client registers it should update the metadata in the server's database")]
		public async Task Registration__metadata_should_be_stored_by_the_server()
		{
			MockServiceCommunicator.LogHeaders(_logger);
			var service = new MockService();
			var serverStorage = new InMemoryRegistrationStorage();
			var client = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
			var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
			await client.RegisterAsync(new Dictionary<string, string>{ {"Answer", "Bloop"} });
			serverStorage.LoadRegistration("Client1")
				.Should()
				.Contain(new KeyValuePair<string, string>("Answer", "Bloop"));
		}

		[Fact(DisplayName = "Given that no one owns an alias, I should be able to request ownership of it")]
		public async Task Alias__requesting_an_unowned_alias()
		{
			MockServiceCommunicator.LogHeaders(_logger);
			var uk123Token = Guid.NewGuid();
			var service = new MockService();
			var serverStorage = new InMemoryRegistrationStorage();
			var client = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
			var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
			await client.RegisterAsync();
			(await client.RequestOwnershipOfAliasAsync("UK123", uk123Token)).Should().Be(uk123Token);
		}

		[Fact(DisplayName = "Given that someone else owns an alias, when I ask I should be told that I cannot have it")]
		public async Task Alias__requesting_an_already_owned_alias()
		{
			MockServiceCommunicator.LogHeaders(_logger);
			var uk123Token = Guid.NewGuid();
			var service = new MockService();
			var serverStorage = new InMemoryRegistrationStorage();
			var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
			var client2 = new ApolloClient(new MockServiceCommunicator("Client2", service, _logger));
			var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
			await client1.RegisterAsync();
			await client2.RegisterAsync();
			(await client1.RequestOwnershipOfAliasAsync("UK123", uk123Token)).Should().Be(uk123Token);
			(await client2.RequestOwnershipOfAliasAsync("UK123", Guid.NewGuid())).Should().Be(Guid.Empty);
		}

		[Fact(DisplayName = "Given that someone else owns an alias, I should be able to claim it")]
		public async Task Alias__taking_an_already_owned_alias()
		{
			MockServiceCommunicator.LogHeaders(_logger);
			var client1Token = Guid.NewGuid();
			var client2Token = Guid.NewGuid();
			var service = new MockService();
			var serverStorage = new InMemoryRegistrationStorage();
			var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
			var client2 = new ApolloClient(new MockServiceCommunicator("Client2", service, _logger));
			var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
			await client1.RegisterAsync();
			await client2.RegisterAsync();
			(await client1.RequestOwnershipOfAliasAsync("UK123", client1Token)).Should().Be(client1Token);
			(await client2.RequestOwnershipOfAliasAsync("UK123", client2Token)).Should().Be(Guid.Empty);
			(await client2.TakeOwnershipOfAliasAsync("UK123", client2Token)).Should().Be(client2Token);
		}
	}
}
