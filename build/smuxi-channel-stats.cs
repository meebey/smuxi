using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Meebey.SmartIrc4net;

class MainClass
{
	static void Main()
	{
		var client = new IrcClient() {
			AutoReconnect = true,
			AutoRetry = true,
			ActiveChannelSyncing = true
		};
		client.Connect("irc.oftc.net", 6667);
		client.Login("smuxi-bot", "smuxi bot");
		client.OnRegistered += delegate {
			client.RfcJoin("#smuxi");
		};
		
		var client2 = new IrcClient() {
			AutoReconnect = true,
			AutoRetry = true,
			ActiveChannelSyncing = true
		};
		client2.Connect("irc.freenode.net", 6667);
		client2.Login("smuxi-bot", "smuxi bot");
		client2.OnRegistered += delegate {
			client2.RfcJoin("#smuxi");
		};

		var timer = new Timer(delegate {
			var chan = client.GetChannel("#smuxi");
			var chan2 = client2.GetChannel("#smuxi");
			if (chan == null) {
				client.RfcJoin("#smuxi");
				return;
			}
			if (chan2 == null) {
				client2.RfcJoin("#smuxi");
				return;
			}
			// filter duplicates and clones
			var users = new List<string>();
			foreach (string user in chan.Users.Keys) {
				var nick = user.TrimEnd('_');
				users.Add(nick);
			};
			foreach (string user in chan2.Users.Keys) {
				var nick = user.TrimEnd('_');
				if (users.Contains(nick)) {
					continue;
				}
				users.Add(nick);
			}
			Console.WriteLine("{0} #smuxi user count: {1}",
							DateTime.Now.ToString("s"), users.Count);
		}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

		var thread = new Thread(new ThreadStart(delegate { 
			while (client.IsConnected) {
				client.Listen();
				Thread.Sleep(1000);
			}
		}));
		thread.Start();

		var thread2 = new Thread(new ThreadStart(delegate {
			while (client2.IsConnected) {
				client2.Listen();
				Thread.Sleep(1000);
			}
		}));
		thread2.Start();

		thread.Join();
		thread2.Join();
	}
}
