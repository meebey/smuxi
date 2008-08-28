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

using System.Threading;
using System;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Net.Sockets;

namespace DotNetRemotingCC.Channels.BidirectionalTCP
{

	public class BidirTcpServerTransportSink: IServerChannelSink 
	{
		private IServerChannelSink _nextSink;
		private TcpListener _serverSocket;
		private Thread _backgrd;

		private int _port;
		private String _IPAddress;

		public BidirTcpServerTransportSink(IServerChannelSink nextSink, int port, String IPAddress) 
		{
			_port = port;
			_IPAddress = IPAddress;
			_nextSink = nextSink;
			// StartListening();
		}

		// used on the client side
		public BidirTcpServerTransportSink(IServerChannelSink nextSink) 
		{
			_nextSink = nextSink;
		}

	
		public void HandleIncomingMessage(ChannelMessage msg) 
		{
			ITransportHeaders responseHeaders;
			Stream responseStream;
			IMessage responseMsg;

			ServerChannelSinkStack stack = new ServerChannelSinkStack();
			stack.Push(this,msg);
			ServerProcessing proc = _nextSink.ProcessMessage(stack,null,msg.Headers,msg.Body,out responseMsg, out responseHeaders,out responseStream);
			
			// check the return value. 
			switch (proc) 
			{
					// this message has been handled synchronously
				case ServerProcessing.Complete:
					// send a response message
	
					ChannelMessage reply = new ChannelMessage(msg.From,msg.ID,responseHeaders,responseStream);
					ConnectionManager.SendMessage(reply);
					break;

					// this message has been handled asynchronously
				case ServerProcessing.Async:
					// nothing needs to be done yet 
					break;

					// it's been a one way message
				case ServerProcessing.OneWay:
					// nothing needs to be done yet 
					break;
			}
		}
/*
		public void StartListening()
		{
			ConnectionManager.StartListening(_port);
		}
		*/
		public void AsyncProcessResponse(
			IServerResponseChannelSinkStack sinkStack, object state, 
			IMessage msg, ITransportHeaders headers, System.IO.Stream stream) 
		{
			ChannelMessage req = (ChannelMessage) state;
			ChannelMessage reply = new ChannelMessage(req.From,req.ID,headers,stream);
			ConnectionManager.SendMessage(reply);
		}

		public IServerChannelSink NextChannelSink 
		{
			get 
			{
				return _nextSink;
			}
		}

		public System.Collections.IDictionary Properties 
		{
			get 
			{
				// not needed
				return null;
			}
		}

		public ServerProcessing ProcessMessage(
			IServerChannelSinkStack sinkStack, IMessage requestMsg, 
			ITransportHeaders requestHeaders, Stream requestStream, 
			out IMessage responseMsg, out ITransportHeaders responseHeaders, 
			out Stream responseStream) 
		{
			// will never be called for a server side transport sink
			throw new NotSupportedException();
		}

		public Stream GetResponseStream(
			IServerResponseChannelSinkStack sinkStack, object state, 
			IMessage msg, ITransportHeaders headers) 
		{
			// it's not possible to directly access the stream 
			return null;
		}


	}
}
