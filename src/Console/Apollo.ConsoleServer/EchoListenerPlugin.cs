﻿using System;
using System.Threading.Tasks;
using Apollo.Common.Abstractions;
using Apollo.Common.Infrastructure;

namespace Apollo.ConsoleServer
{
	public class EchoListenerPlugin : ApolloPluginBase
	{
		private const string EchoKey = "Echo";

		protected override async Task OnInitialized()
		{
			await base.OnInitialized();
			Communicator.AddHandler(ApolloQueue.ClientSessions, new MessageHandler(this, EchoKey, (q, m, token) =>
			{
				var reply = MessageFactory.CreateReply(m);
				reply.Label = m.Label;
				reply.Properties[EchoKey] = m.Properties[EchoKey];
				Logger.Info($"Echoing {reply.Properties[EchoKey]}");
				Communicator.SendToClientAsync(reply);
				return MessageStatus.Complete;
			}));
			Communicator.ServerJobReceived += OnServerJobReceived;
		}

		private void OnServerJobReceived(IMessage m, ref MessageReceivedEventArgs e)
		{
			if (m.Label != EchoKey) 
				return;
			var reply = MessageFactory.CreateReply(m);
			reply.Label = m.Label;
			reply.Properties[EchoKey] = m.Properties[EchoKey];
			Console.WriteLine($"Echoing {reply.Properties[EchoKey]}");
			Communicator.SendToClientAsync(reply);
			e.Status = MessageStatus.Complete;
		}
	}
}