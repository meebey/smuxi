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
using System.IO;
using System.Threading;


namespace DotNetRemotingCC.Channels.BidirectionalTCP
{
	public class BidirTcpClientTransportSink: BaseChannelSinkWithProperties,
		IClientChannelSink, IChannelSinkBase
	{
		String _hostname;
		int _port;
		String _objectURI;
		String _GUID;

		public BidirTcpClientTransportSink(String url)
		{	
			Helper.SplitURL(url,out _hostname, out _GUID, out _port, out _objectURI);
		}

		public void ProcessMessage(IMessage msg, 
			ITransportHeaders requestHeaders, Stream requestStream, 
			out ITransportHeaders responseHeaders, 
			out Stream responseStream) 
		{
			String objectURI;

			// check the URL
			String URL = (String) msg.Properties["__Uri"];
			String baseurl = Helper.ParseURL(URL,out objectURI);
			if (baseurl == null) 
			{
				objectURI = URL;
				if (_hostname == null) 
				{
					URL = Helper.TCPGUID_PREFIX + _GUID + objectURI;
				}
			}

			requestHeaders["__RequestUri"] = objectURI;

			// transfer it
			ChannelMessage req;
			if (URL.ToLower().StartsWith(Helper.TCP_PREFIX)) 
			{
				req = new ChannelMessage(_hostname.ToLower() + ":" + _port,requestHeaders,requestStream);
			} 
			else 
			{
				req = new ChannelMessage(_GUID,requestHeaders,requestStream);
			}
			AutoResetEvent evt = new AutoResetEvent(false);
			MessageHandler.RegisterAutoResetEvent(req.ID,evt);
			ConnectionManager.SendMessage(req);
			ChannelMessage resp = MessageHandler.WaitAndGetResponseMessage(req.ID, evt);
			responseHeaders = resp.Headers;
			responseStream = resp.Body;
		}


		public void AsyncProcessRequest(IClientChannelSinkStack sinkStack, 
			IMessage msg, ITransportHeaders headers, Stream stream) 
		{
			String objectURI;

			// check the URL
			String URL = (String) msg.Properties["__Uri"];
			String baseurl = Helper.ParseURL(URL,out objectURI);
			if (baseurl == null) 
			{
				objectURI = URL;
			}

			headers["__RequestUri"] = objectURI;

			// transfer it
			ChannelMessage req;
			if (URL.ToLower().StartsWith(Helper.TCP_PREFIX)) 
			{
				req = new ChannelMessage(_hostname.ToLower() + ":" + _port,headers,stream);
			} 
			else 
			{
				req = new ChannelMessage(_GUID,headers,stream);
			}
			AsyncResponseHandler hdl = new AsyncResponseHandler(sinkStack);
			MessageHandler.RegisterAsyncResponseHandler(req.ID,hdl);
			ConnectionManager.SendMessage(req);
		}

		public void AsyncProcessResponse(System.Runtime.Remoting.Channels.IClientResponseChannelSinkStack sinkStack, object state, System.Runtime.Remoting.Channels.ITransportHeaders headers, System.IO.Stream stream) 
		{
			// not needed in a transport sink!
			throw new NotSupportedException();
		}

		public Stream GetRequestStream(System.Runtime.Remoting.Messaging.IMessage msg, System.Runtime.Remoting.Channels.ITransportHeaders headers) 
		{
			// no direct way to access the stream
			return null;
		}

		public System.Runtime.Remoting.Channels.IClientChannelSink NextChannelSink 
		{
			get 
			{
				// no more sinks
				return null;
			}
		}
	
	}
}
