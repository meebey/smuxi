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
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace DotNetRemotingCC.Channels.BidirectionalTCP
{
	/// <summary>
	/// Represents exactly one bidirectional connection.
	/// </summary>
	public class Connection
	{
		private const string CLIENT_SIGNATURE_STR = "BIDIRTCPCLIENT";
		private const string SERVER_SIGNATURE_STR = "BIDIRTCPSERVER";
		private static byte[] CLIENT_SIGNATURE = System.Text.Encoding.ASCII.GetBytes(CLIENT_SIGNATURE_STR);
		private static byte[] SERVER_SIGNATURE = System.Text.Encoding.ASCII.GetBytes(SERVER_SIGNATURE_STR);
		
		private Socket _sock; // underlying socket
		private int _port; // server port number (either my own if server, else the other one)
		private String _hostname; // server host name (either my own if server, else the other one)
		public static long conncount;
		public long ID;

		public String Receiver; // name:port or GUID of the other peer
		public bool Error;
		
		/// <summary>
		/// sends a message to the current connection
		/// </summary>
		/// <param name="msg"></param>
		public void SendMessage(ChannelMessage msg) 
		{
			msg.Headers["Id"] = msg.ID;
			msg.Headers["InReplyTo"] = msg.InReplyTo;
			msg.Headers["From"] = msg.From;
			msg.Headers["To"] = msg.To;

			BinaryFormatter fmt = new BinaryFormatter();
			MemoryStream headers = new MemoryStream();
			// complete output message
			MemoryStream message = new MemoryStream();

			// serialize headers and get header size
			fmt.Serialize(headers,msg.Headers);
			long len = headers.Length;

			BinaryWriter wrt = new BinaryWriter(message);

			// write the number of header bytes
			wrt.Write(headers.Length);
			// write the number of body bytes
			wrt.Write(msg.Body.Length);
			// write the header
			byte[] buf = headers.GetBuffer();
			message.Write(buf,0,(int) headers.Length);
			// write the body
			buf = new Byte[msg.Body.Length];
			msg.Body.Read(buf,0,buf.Length);
			message.Write(buf,0,buf.Length);

			lock (_sock) 
			{
				// only one message is sent at a given time via a connection
				_sock.Send(message.GetBuffer(),0,(int) message.Length,SocketFlags.None);
			}
		}

		/// <summary>
		/// Prepares to open a new connection
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public Connection(String host, int port) 
		{
			ID = conncount++;
			_hostname = host.ToLower();
			_port = port;
		}

		/// <summary>
		/// A client connection has been established. Start to exchange receiver names.
		/// </summary>
		/// <param name="sock">Newly .accept'ed socket</param>
		/// <param name="port">Server side port number</param>
		public Connection(Socket sock, int port) 
		{
			ID = conncount++;
			_sock = sock;
			_port = port;
		}

		public void HandleIncomingConnection()
		{
			// the client is expected to send "BIDIRTCPCHANNEL" plus a 16 byte GUID

			Byte[] buf = new Byte[14];
			_sock.Receive(buf,14,SocketFlags.None);

			String sig = System.Text.Encoding.ASCII.GetString(buf);

			if (sig != CLIENT_SIGNATURE_STR)
			{
				_sock.Send(System.Text.Encoding.ASCII.GetBytes("Invalid BIDIRTCPCHANNEL header"));
				_sock.Close();
				Error=true;
				return;
			}

			buf = new Byte[16];
			_sock.Receive(buf,16,SocketFlags.None);
			Guid g = new Guid(buf);
			Receiver = g.ToString();

			// now the server will send BIDIRTCPSERVER + GUID

			Guid myguid = Helper.GetMyGUID();
			buf = new Byte[30]; // 14 byte string + 16 byte GUID
			Array.Copy(SERVER_SIGNATURE,0,buf,0,14);
			Array.Copy(myguid.ToByteArray(),0,buf,14,16);
			_sock.Send(buf,SocketFlags.None);
			ConnectionManager.RegisterConnection(this,Receiver);
			IPEndPoint ep = (IPEndPoint) _sock.RemoteEndPoint;
			Console.WriteLine("INCOMING from " + ep.Address.ToString() + ":" + ep.Port + " --> " + Receiver);
			StartHandleIncomingTraffic();
		}

		public void Connect() 
		{
			_sock = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
			_sock.Connect(new IPEndPoint(IPAddress.Parse(_hostname),_port));

			// client sends "BIDIRTCPCLIENT" + GUID
			Byte[] buf = new Byte[30]; // 14 byte string + 16 byte GUID
			Guid myguid = Helper.GetMyGUID();
			Array.Copy(CLIENT_SIGNATURE,0,buf,0,14);
			Array.Copy(myguid.ToByteArray(),0,buf,14,16);
			_sock.Send(buf,SocketFlags.None);

			// now the server will reply with its GUID (which will be ignored!)
			_sock.Receive(buf,30,SocketFlags.None);
			String sig = System.Text.Encoding.ASCII.GetString(buf,0,14);


			if (sig != SERVER_SIGNATURE_STR)
			{
				_sock.Send(System.Text.Encoding.ASCII.GetBytes("Invalid BIDIRTCPCHANNEL header"));
				_sock.Close();
				Error=true;
				throw new Exception("Server did not reply correctly. BIDIRTCPSERVER missing in reply");
			}
			Receiver = _hostname + ":" + _port;
			ConnectionManager.RegisterConnection(this,Receiver);
			Byte[] guidbuf = new Byte[16];
			Array.Copy(buf,14,guidbuf,0,16);
			Guid g = new Guid(guidbuf);

			ConnectionManager.RegisterConnection(this,g.ToString());
			IPEndPoint ep = (IPEndPoint) _sock.LocalEndPoint;
			Console.WriteLine("OUTGOING to " + ep.Address.ToString() + ":" + ep.Port + " --> " + Receiver);
			StartHandleIncomingTraffic();
		}

		private void HandleIncomingTraffic() 
		{
			try 
			{
				while (true) // forever
				{
					// wait until a message is received
					ChannelMessage msg = ChannelMessage.ReadFromSocket(_sock,_port);

					// dispatch it via the handler
					MessageHandler.MessageReceived(msg);
				}
			} 
			catch (Exception e)
			{
				Close();
			}
		}

		private void StartHandleIncomingTraffic() 
		{
			Thread x = new Thread(new ThreadStart(this.HandleIncomingTraffic));
			x.Name  ="HandleIncomingTraffic";
			x.IsBackground = true;
			x.Start();
		}

		private void Close() 
		{
			try 
			{
				_sock.Close();
				Console.WriteLine("Closing connection #{0} to {1}", ID, Receiver);
				ConnectionManager.UnregisterConnection(this);
			} 
			catch 
			{
			}
		}

	}
}
