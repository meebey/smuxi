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
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace DotNetRemotingCC.Channels.BidirectionalTCP
{
	/// <summary>
	/// Sounds like a cool hack to use messages via TCP ;-)?
	/// 
	/// Actually, to support having only one connection between client and server, I have to go this way to allow for
	/// request/response/event via a single connection.
	/// </summary>
	public class ChannelMessage
	{
		// message IDs
		public Guid ID;			  // will be filled
		public Guid InReplyTo;    // can be null on requests or events

		// sender GUIDs
		public String From;		  // will be bidirtcp://hostname:port OR a GUID (when identifying a client)
		public String To;		  // will be bidirtcp://hostname:port OR a GUID (when identifying a client)
		public String SecondaryGuessTo; // inserting the local address ;-)

		public ITransportHeaders Headers;
		public Stream Body;

		private ChannelMessage()
		{
		}


		/// <summary>
		/// Creates a client to server request message
		/// </summary>
		public ChannelMessage(String receiver, ITransportHeaders head, Stream message)
		{
			ID = Guid.NewGuid();
			From = Helper.GetMyGUID().ToString();
			To = receiver;
			Headers = head;
			Body = message;
		}

		/// <summary>
		/// Creates a server to client reply message
		/// </summary>
		public ChannelMessage(String receiver, Guid responseTo, ITransportHeaders head, Stream message) 
		{
			ID = Guid.NewGuid();
			InReplyTo = responseTo;
			From = Helper.GetMyGUID().ToString();
			To = receiver;
			Headers = head;
			Body = message;
		}

		/// <summary>
		/// read the message incoming from a socket
		/// </summary>
		/// <param name="sock"></param>
		public static ChannelMessage ReadFromSocket(Socket sock, int port) 
		{
			BinaryFormatter fmt = new BinaryFormatter();
			ChannelMessage msg = new ChannelMessage();
			// read the lead-in (16 bytes)
			byte[] buf = new Byte[16];
			sock.Receive(buf,16,SocketFlags.None);
			MemoryStream lead = new MemoryStream(buf);
			BinaryReader leadrdr = new BinaryReader(lead);
			

			// read the number of header bytes
			int headerlen = (int) leadrdr.ReadInt64();
			// read the number of body bytes
			int bodylen = (int) leadrdr.ReadInt64();

			// get header & body 
			byte[] headerbytes = new Byte[headerlen];
			sock.Receive(headerbytes,0,headerlen,SocketFlags.None);
			byte[] bodybytes = new Byte[bodylen];
			sock.Receive(bodybytes,0,bodylen,SocketFlags.None);

			// layer first memorystream
			MemoryStream head = new MemoryStream(headerbytes,0,headerlen,false);
			// layer body stream
			MemoryStream body = new MemoryStream(bodybytes,0,bodylen,false);

			msg.Headers = (ITransportHeaders) fmt.Deserialize(head);
			msg.ID = (Guid) msg.Headers["Id"];
			msg.InReplyTo = (Guid) msg.Headers["InReplyTo"];
			msg.From = (String) msg.Headers["From"];
			// IPEndPoint ipe = (IPEndPoint) sock.LocalEndPoint;
			msg.To = (String) msg.Headers["To"];
			msg.SecondaryGuessTo = Helper.GetIPAddress() + ":" + port;
			body.Seek(0,SeekOrigin.Begin);
			msg.Body = body;
			return msg;
		}

		/// <summary>
		/// returns host==null and port==0 if the receiver is specified by guid
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		internal void SplitReceiver(out String host, out int port)
		{
			host = null;
			port =0;
			int pos = To.IndexOf(":");
			if (pos == -1) return;
			
			host = To.Substring(0,pos);
			port = int.Parse(To.Substring(pos+1));
		}
	}
}
