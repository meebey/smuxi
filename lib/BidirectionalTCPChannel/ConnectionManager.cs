// Copyright (c) 2002 Ingo Rammer
//
// Permission is hereby granted, free of charge, to any person obtaining a 
// copy of this software and associated documentation files (the "Software"), 
// to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included 
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS 
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
// THE SOFTWARE.

/*
 * And now for a little bit of advertisment:
 * 
 *    I'm consultant, author, speaker, trainer and developer focusing on distributed 
 *	  .NET applications. I'm available for technical and architectural consulting,  
 *    on-site training and development throughout Europe. 
 * 
 *	  If you currently look at developing a distributed .NET application, 
 *    think about designing/implementing an application framework, or just like
 *    what you see here and think that you need a hardcore .NET person on your 
 *    project, please don't hesitate to contact me at rammer@sycom.at.
 *
 *    My services include: custom training, design, prototyping & architecural review.
 *    I'm not normally available for long term project development. But if you are
 *    working on something exceptionally interesting - who knows? ;-)
 * 
 *												Ingo Rammer
 *												rammer@sycom.at
 *												http://www.dotnetremoting.cc
 */

using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections;


namespace DotNetRemotingCC.Channels.BidirectionalTCP
{
	/// <summary>
	/// Handles all open connections.
	/// </summary>
	public class ConnectionManager
	{
		private static IDictionary ConnByReceiver = new Hashtable();

		internal class Listener
		{

			private int _port;
			internal Thread BackgroundThread;

			internal Listener(int port)
			{
				_port = port;
			}

			/// <summary>
			/// Server will go into connection-mode
			/// </summary>
			internal void ListenForConnectionRequests() 
			{
				TcpListener lst = new TcpListener(_port);
				lst.Start();

				while (true) 
				{
					Socket sock = lst.AcceptSocket();
					Connection conn = new Connection(sock,_port);
					Thread t = new Thread(new ThreadStart(conn.HandleIncomingConnection));
					t.Name = "HandleIncomingConnections";
					t.Start();
					t.IsBackground=true;
				}
			}
		}

		/// <summary>
		/// Sends a message. Looks for the necessary connection. If none exists, a new one will be built 
		/// </summary>
		/// <param name="msg"></param>
		public static void SendMessage(ChannelMessage msg) 
		{
			Connection conn = (Connection) ConnByReceiver[msg.To];
			if (conn == null) 
			{
				String host;
				int port;
				msg.SplitReceiver(out host, out port);
				if (host == null) 
				{
					// message is directed at a GUID but no connection found
					
					// TODO: wait some seconds and retry. Maybe the client reconnects.
					Console.WriteLine("Message is directed at a GUID but no connection found");
					throw new Exception("Message is directed at a GUID but no connection found");
				}
				conn = new Connection(host,port);
				conn.Connect();
				ConnByReceiver[msg.To] = conn;
			}

			conn.SendMessage(msg);
		}	

		/// <summary>
		/// Opens connection to a new machine
		/// </summary>
		public static void OpenConnection(String hostname, int port)
		{
            Connection conn = new Connection(hostname,port);
			ConnByReceiver[hostname.ToLower() + ":" + port] = conn;
		}

		public static void StartListening(int port)
		{
			Listener ls = new Listener(port);
			Thread backgrd = new Thread(new ThreadStart(ls.ListenForConnectionRequests));
			ls.BackgroundThread = backgrd;
			backgrd.Name = "ListenForConnectionRequests";
			backgrd.Start();
			backgrd.IsBackground=true;
		}

		public static void UnregisterConnection(Connection conn)
		{
			lock(ConnByReceiver)
			{
				bool found = true;
				
				// TODO: Clean up the following loop. It certainly takes longer than necessary.
				
				while (found)
				{
					found=false;
					foreach (DictionaryEntry de in ConnByReceiver) 
					{
						if (de.Value == conn) 
						{
							found=true;
							ConnByReceiver.Remove(de.Key);
							Console.WriteLine("Unregistered connection #{0} as {1}. Count: {2}",conn.ID,de.Key, ConnByReceiver.Count);
							break;
						}
					}
				}
			}
		}

		public static void RegisterConnection(Connection conn, Object key)
		{
			lock (ConnByReceiver) 
			{
				ConnByReceiver[key] = conn;
				Console.WriteLine("Registered connection #{0} as {1}. Count: {2}",conn.ID,key, ConnByReceiver.Count);
			}
		}
	}
}
