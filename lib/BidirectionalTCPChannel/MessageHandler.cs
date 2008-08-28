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
 *    I'm consultant, author, speaker, trainer and developer - mainly for distributed 
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
using System.Collections;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting;
using System.Threading;


namespace DotNetRemotingCC.Channels.BidirectionalTCP
{
	public class MessageHandler
	{
		/// <summary>
		/// Use to throw the request at a different execution thread. Else lockups will occur.
		/// </summary>
		private class SingleMessageHandler
		{
			ChannelMessage msg;

			internal SingleMessageHandler(ChannelMessage incomingMessage) 
			{
				msg = incomingMessage;
			}

			public void HandleMessage() 
			{
				// whenever a channel message has been received, it
				// will be forwarded to this method

				// check if it's a request or a reply
				if ((msg.InReplyTo == Guid.Empty) && (msg.ID != Guid.Empty)) 
				{
					// it's a request

					Guid requestID = msg.ID;
				
					// Request received 

					// check for a registered server
					BidirTcpServerTransportSink snk = (BidirTcpServerTransportSink) 
						_servers[msg.To];

					if (snk==null) 
					{
						// No server side sink found for address 
						snk = (BidirTcpServerTransportSink) 
							_servers[msg.SecondaryGuessTo];
						if (snk==null) 
						{
							Console.WriteLine("No sink found for address");
							return;
						}
					} 

					// Dispatch the message to serversink
					// Console.WriteLine("Will dispatch message to server sink");
					snk.HandleIncomingMessage(msg);

				} 
				else if (msg.InReplyTo != Guid.Empty) 
				{
					// check who's waiting for it

					Object notify = _waitingFor[msg.InReplyTo];


					AutoResetEvent evt = notify as AutoResetEvent;
					if (evt!= null) 
					{
						_responses[msg.InReplyTo] = msg;
						//Console.WriteLine("Removing Event for   ID:" + msg.InReplyTo.ToString());
						_waitingFor.Remove(msg.InReplyTo);
						evt.Set();
					} 
					else if (notify as AsyncResponseHandler != null) 
					{
						_waitingFor.Remove(msg.InReplyTo);
						((AsyncResponseHandler)notify).HandleAsyncResponseMsg(msg);
					} 
					else 
					{
						Console.WriteLine("No one is waiting for this reply");
						// No one is waiting for this reply. Ignore.
					}
				}
			}
		}

		// threads waiting for response
		private static IDictionary _waitingFor = 
			Hashtable.Synchronized(new Hashtable());

		// known server sinks
		private static IDictionary _servers = 
			Hashtable.Synchronized(new Hashtable());

		// responses received
		private static IDictionary _responses = 
			Hashtable.Synchronized(new Hashtable());

		internal static void RegisterAsyncResponseHandler(Guid ID, 
			AsyncResponseHandler ar) 
		{
			_waitingFor[ID] = ar;
		}

		internal static void RegisterServer(BidirTcpServerTransportSink snk, 
			String receiver) 
		{
			// Registering sink for a specified receiver channel
			_servers[receiver] = snk;
		}

		internal static void RegisterAutoResetEvent(Guid ID, AutoResetEvent evt) 
		{
			_waitingFor[ID] = evt;
			//Console.WriteLine("Registered Event for ID:" + ID.ToString());
		}

		internal static ChannelMessage WaitAndGetResponseMessage(Guid ID,AutoResetEvent evt) 
		{
			// suspend the thread until the message returns
			// Console.WriteLine("Getting Event for    ID:" + ID.ToString());
			// AutoResetEvent evt = (AutoResetEvent) _waitingFor[ID];
			evt.WaitOne();

			// waiting for resume
			ChannelMessage msg = (ChannelMessage) _responses[ID];
			_responses.Remove(ID);
			return msg;
		}


		/// <summary>
		/// Will be called whenever a message has been received. No matter if request, response, event, or event reply ...
		/// 
		/// Starts a new thread to handle the message
		/// </summary>
		/// <param name="msg"></param>
		public static void MessageReceived(ChannelMessage msg)
		{		
			SingleMessageHandler mh = new SingleMessageHandler(msg);

            Thread t = new Thread(new ThreadStart(mh.HandleMessage));
			t.IsBackground=true;
			t.Start();
		}
	}
}
