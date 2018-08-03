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
		private readonly ITestOutputHelper _logger;
		public CorePluginTests(ITestOutputHelper logger)
		{
			_logger = logger;
		}

		[Fact(DisplayName = "When a client registers it should update the metadata in the server's database")]
		public async Task Registration__metadata_should_be_stored_by_the_server()
		{
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				await client.RegisterAsync(new Dictionary<string, string> {{"Answer", "Bloop"}});
				serverStorage.LoadRegistration("Client1")
					.Should()
					.Contain(new KeyValuePair<string, string>("Answer", "Bloop"));
			}
		}

		[Fact(DisplayName = "Given that no one owns an alias, I should be able to request ownership of it")]
		public async Task Alias__requesting_an_unowned_alias()
		{
			var uk123Token = Guid.NewGuid();
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				(await client.RegisterAsync()).Should().NotBeNullOrEmpty();
				(await client.RequestOwnershipOfAliasAsync("UK123", uk123Token)).Should().Be(uk123Token);
			}
		}

		[Fact(DisplayName = "Given that someone else owns an alias, when I ask I should be told that I cannot have it")]
		public async Task Alias__requesting_an_already_owned_alias()
		{
			var uk123Token = Guid.NewGuid();
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var client2 = new ApolloClient(new MockServiceCommunicator("Client2", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				(await client1.RegisterAsync()).Should().NotBeNullOrEmpty();
				(await client2.RegisterAsync()).Should().NotBeNullOrEmpty();

				(await client1.RequestOwnershipOfAliasAsync("UK123", uk123Token)).Should().Be(uk123Token, "client 1 was the first to request ownership of UK123");
				(await client2.RequestOwnershipOfAliasAsync("UK123", Guid.NewGuid())).Should().Be(Guid.Empty, "client 1 already owns UK123");
			}
		}

		[Fact(DisplayName = "Given that someone else owns an alias, I should be able to claim it")]
		public async Task Alias__taking_an_already_owned_alias()
		{
			var client1Token = Guid.NewGuid();
			var client2Token = Guid.NewGuid();
			var practiceId = "UK123";
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var client2 = new ApolloClient(new MockServiceCommunicator("Client2", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				(await client1.RegisterAsync()).Should().NotBeNullOrEmpty();
				(await client2.RegisterAsync()).Should().NotBeNullOrEmpty();

				(await client1.RequestOwnershipOfAliasAsync(practiceId, client1Token)).Should().Be(client1Token, "client 1 was the first to request ownership of UK123");
				(await client2.RequestOwnershipOfAliasAsync(practiceId, client2Token)).Should().Be(Guid.Empty, "client 1 already owns UK123");
				(await client2.DemandOwnershipOfAliasAsync(practiceId, client2Token)).Should().Be(client2Token, "client 2 should be able to steal ownership of UK123");
				(await client1.RequestOwnershipOfAliasAsync(practiceId, client1Token)).Should().Be(Guid.Empty, "client 1 no longer owns UK123");
				(await client2.RequestOwnershipOfAliasAsync(practiceId, client2Token)).Should().Be(client2Token, "client 2 now owns UK123");
			}
		}

		[Fact(DisplayName = "Given that a client owns an alias, I should be able to ping them via that alias")]
		public async Task Ping__ping_an_alias()
		{
			var client1Token = Guid.NewGuid();
			var practiceId = "UK123";
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var client2 = new ApolloClient(new MockServiceCommunicator("Client2", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				(await client1.RegisterAsync()).Should().NotBeNullOrEmpty();
				(await client2.RegisterAsync()).Should().NotBeNullOrEmpty();

				(await client1.RequestOwnershipOfAliasAsync(practiceId, client1Token)).Should().Be(client1Token, "client 1 was the first to request ownership of UK123");

				var result = await client1.GetPlugin<ClientCorePlugin>().PingAlias(practiceId);
				result.RethrowCaughtException();
				result.Result.Should().Be(PingStats.PingResult.Success);
			}
		}

		[Fact(DisplayName = "Given that no client owns an alias, I should be told that I cannot ping that alias")]
		public async Task Ping__ping_an_unowned_alias()
		{
			var practiceId = "UK123";
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var client2 = new ApolloClient(new MockServiceCommunicator("Client2", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				(await client1.RegisterAsync()).Should().NotBeNullOrEmpty();
				(await client2.RegisterAsync()).Should().NotBeNullOrEmpty();

				var result = await client1.GetPlugin<ClientCorePlugin>().PingAlias(practiceId);
				result.RethrowCaughtException();
				result.Result.Should().Be(PingStats.PingResult.AddresseeNotFound);
			}
		}

		[Fact(DisplayName = "As a client I want to be able to ping the server")]
		public async Task Ping__ping_the_server()
		{
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				(await client1.RegisterAsync()).Should().NotBeNullOrEmpty();

				var result = await client1.GetPlugin<ClientCorePlugin>().PingServer();
				result.RethrowCaughtException();
				result.Result.Should().Be(PingStats.PingResult.Success);
			}
		}

		[Fact(DisplayName = "As a client I should get a timeout if i try to ping the servers when they are down")]
		public async Task Ping__ping_the_server_when_it_is_down()
		{
			using (var service = new MockService(_logger))
			{
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var result = await client1.GetPlugin<ClientCorePlugin>().PingServer();
				result.RethrowCaughtException();
				result.Result.Should().Be(PingStats.PingResult.Timeout);
			}
		}

		[Fact(DisplayName = "As a client I want to be able to ping a client")]
		public async Task Ping__ping_a_client()
		{
			using (var service = new MockService(_logger))
			{
				var serverStorage = new InMemoryApolloServerRepository();
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));
				var client2 = new ApolloClient(new MockServiceCommunicator("Client2", service, _logger));
				var server = new ApolloServer(new MockServiceCommunicator("Server1", service, _logger), serverStorage);
				(await client1.RegisterAsync()).Should().NotBeNullOrEmpty();
				(await client2.RegisterAsync()).Should().NotBeNullOrEmpty();

				var result = await client1.GetPlugin<ClientCorePlugin>().PingClient(client2.Identifier);
				result.RethrowCaughtException();
				result.Result.Should().Be(PingStats.PingResult.Success);
				
			}
		}

		[Fact(DisplayName = "As a client I want to be able to ping myself")]
		public async Task Ping__ping_self()
		{
			using (var service = new MockService(_logger))
			{
				var client1 = new ApolloClient(new MockServiceCommunicator("Client1", service, _logger));

				var result = await client1.GetPlugin<ClientCorePlugin>().PingClient(client1.Identifier);
				result.RethrowCaughtException();
				result.Result.Should().Be(PingStats.PingResult.Success);

			}
		}
	}
}
