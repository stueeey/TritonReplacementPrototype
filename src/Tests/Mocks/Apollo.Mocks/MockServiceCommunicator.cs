﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Common;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;
using log4net;
using Xunit.Abstractions;


namespace Apollo.Mocks
{
	public class MockServiceCommunicator : ServiceCommunicatorBase
	{
		private readonly MockService _service;
		private readonly string _identifier;
		private readonly ITestOutputHelper _logger;

		public const string ClientQueueName = "ClientSessions";
		public const string ServerQueueName = "ServerQueue";
		public const string RegistrationQueueName = "RegistrationQueue";
		public const string AliasQueueName = "AliasQueue";

		public MockServiceCommunicator(string identifier, MockService service, ITestOutputHelper logger = null)
		{
			State.TryAdd(ApolloConstants.RegisteredAsKey, identifier);
			_logger = logger;
			MessageFactory = new MockMessageFactory("ClientSessions", identifier, service);
			_service = service;
			_identifier = identifier;

			if (logger != null)
			{
				Logger = new MockLogger(logger, nameof(MockServiceCommunicator));
				AnyMessageSent += (m, q) => _logger.WriteLine($"{$"{(DateTime.UtcNow - _service.StartTimeUtc).TotalMilliseconds:N0} ms".PadRight(9)} | {_identifier.PadRight(18)}  ==>  {m.Identifier.PadRight(7)} | {q.ToString().PadRight(14)} | {(m.TargetSession ?? "").PadRight(18)} | {m.Label}");
				AnyMessageReceived += (m, q) => _logger.WriteLine($"{$"{(DateTime.UtcNow - _service.StartTimeUtc).TotalMilliseconds:N0} ms".PadRight(9)} | {_identifier.PadRight(18)}  <==  {m.Identifier.PadRight(7)}");
			}
		}

		public override async Task SendToClientsAsync(params IMessage[] messages)
		{
			if (messages == null)
				return;
			if (messages.Any(message => string.IsNullOrWhiteSpace(message.TargetSession)))
				throw new ArgumentException("Tried to send a client message without a target session specified, make sure you set message.TargetSession to the id of the target client", nameof(messages));
			foreach (var message in messages)
			{
				await Task.Delay(15);
				OnMessageSent(message, ApolloQueue.ClientSessions);
				_service.Enqueue(message, ApolloQueue.ClientSessions, message.TargetSession);
			}
		}

		public override async Task SendToServerAsync(params IMessage[] messages)
		{
			foreach (var message in messages)
			{
				await Task.Delay(15);
				OnMessageSent(message, ApolloQueue.ServerRequests);
				_service.Enqueue(message, ApolloQueue.ServerRequests, message.TargetSession);
			}
		}
		
		public override async Task SendToRegistrationsAsync(params IMessage[] messages)
		{
			foreach (var message in messages)
			{
				await Task.Delay(15);
				OnMessageSent(message, ApolloQueue.Registrations);
				_service.Enqueue(message, ApolloQueue.Registrations, message.TargetSession);
			}
		}

		public override async Task SendToAliasAsync(params IMessage[] messages)
		{
			if (messages == null)
				return;
			if (messages.Any(message => string.IsNullOrWhiteSpace(message.GetTargetAlias())))
				throw new ArgumentException("Tried to send an alias message without an alias specified, call message.SetTargetAlias(<alias>)", nameof(messages));
			foreach (var message in messages)
			{
				await Task.Delay(15);
				OnMessageSent(message, ApolloQueue.Aliases);
				_service.Enqueue(message, ApolloQueue.Aliases, null);
			}
		}

		private void InvokeMessageHandlers(ApolloQueue queue, IMessage message, CancellationToken? token)
		{
			if (message == null)
				return;

			try
			{
				if (Handlers.TryGetValue(queue, out var handlers))
				{
					var status = MessageStatus.Unhandled;
					foreach (var handler in handlers.Where(h => h.PassesFilter(message)))
					{
						try
						{
							if (token?.IsCancellationRequested ?? false)
								return;
							status = handler.HandleMessage(queue, message, token);
							if (status.HasFlag(MessageStatus.Handled))
								break;
						}
						catch (Exception ex)
						{
							_service.AsyncListeningExceptions.Add(ExceptionDispatchInfo.Capture(ex));
							_logger?.WriteLine($"{ex.Message} (This would normally be suppressed by the system)");
							throw;
						}
					}
					if (message.Label == ApolloConstants.PositiveAcknowledgement || message.Label == ApolloConstants.NegativeAcknowledgement)
						return;
					if (status == MessageStatus.Unhandled)
						throw new Exception($"{_identifier} received a message from {queue} with label '{message.Label}' which no handler could handle");
					if (!status.HasFlag(MessageStatus.MarkedForDeletion))
						_service.Enqueue(message, queue, _identifier);

				}
				else
					Debug.Assert(false, "Received a message without having any handlers!");
			}
			catch (Exception ex)
			{
				_logger?.WriteLine($"{ex.Message} (This would normally be suppressed by the system)");
				_service.AsyncListeningExceptions.Add(ExceptionDispatchInfo.Capture(ex));
			}
		}

		void OnClientMessageArrived() => InvokeMessageHandlers(ApolloQueue.ClientSessions, _service.Dequeue(ApolloQueue.ClientSessions, _identifier), null);
		protected override void HandleListenForClientMessagesChanged(bool value)
		{
			if (value)
				_service.GetQueue(ApolloQueue.ClientSessions, _identifier).MessageArrived += OnClientMessageArrived;
			else
				_service.GetQueue(ApolloQueue.ClientSessions, _identifier).MessageArrived -= OnClientMessageArrived;
		}

		void OnRegistrationMessageArrived() => InvokeMessageHandlers(ApolloQueue.Registrations, _service.Dequeue(ApolloQueue.Registrations, _identifier), null);
		protected override void HandleListenForRegistrationsChanged(bool value)
		{
			if (value)
				_service.GetQueue(ApolloQueue.Registrations, _identifier).MessageArrived += OnRegistrationMessageArrived;
			else
				_service.GetQueue(ApolloQueue.Registrations, _identifier).MessageArrived -= OnRegistrationMessageArrived;
		}

		void OnServerMessageArrived() => InvokeMessageHandlers(ApolloQueue.ServerRequests, _service.Dequeue(ApolloQueue.ServerRequests, _identifier), null);
		protected override void HandleListenForServerJobsChanged(bool value)
		{
			if (value)
				_service.GetQueue(ApolloQueue.ServerRequests, _identifier).MessageArrived += OnServerMessageArrived;
			else
				_service.GetQueue(ApolloQueue.ServerRequests, _identifier).MessageArrived -= OnServerMessageArrived;
		}

		void OnAliasMessageArrived() => InvokeMessageHandlers(ApolloQueue.Aliases, _service.Dequeue(ApolloQueue.Aliases, _identifier), null);
		protected override void HandleListenForAliasMessagesChanged(bool value)
		{
			if (value)
				_service.GetQueue(ApolloQueue.Aliases, _identifier).MessageArrived += OnAliasMessageArrived;
			else
				_service.GetQueue(ApolloQueue.Aliases, _identifier).MessageArrived -= OnAliasMessageArrived;
		}

		protected override string GetQueueName(ApolloQueue queue)
		{
			switch (queue)
			{
				case ApolloQueue.Registrations:
					return RegistrationQueueName;
				case ApolloQueue.ServerRequests:
					return ServerQueueName;
				case ApolloQueue.Aliases:
					return AliasQueueName;
				case ApolloQueue.ClientSessions:
					return ClientQueueName;
				default:
					throw new ArgumentOutOfRangeException(nameof(queue), queue, null);
			}
		}

		protected override TimeSpan MaxReplyWaitTime { get; } = TimeSpan.FromSeconds(1);
		protected override ApolloQueue? ParseQueue(string queueName)
		{
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, AliasQueueName))
				return ApolloQueue.Aliases;
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, ClientQueueName))
				return ApolloQueue.ClientSessions;
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, RegistrationQueueName))
				return ApolloQueue.Registrations;
			if (StringComparer.OrdinalIgnoreCase.Equals(queueName, ServerQueueName))
				return ApolloQueue.ServerRequests;
			return null;
		}
	}
}
