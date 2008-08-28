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
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;

namespace DotNetRemotingCC.Channels.BidirectionalTCP
{
	public class BidirTcpClientChannel: BaseChannelWithProperties, IChannelSender, IChannelReceiver
	{
		IDictionary _properties;
		IClientChannelSinkProvider _provider;
		BidirTcpServerTransportSink _serverSink;

		String _name;
		private ChannelDataStore _channelData;

		public BidirTcpClientChannel (IDictionary properties, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider) 
		{
			_properties = properties;
			_provider = clientSinkProvider;
			_name = (String) _properties["name"];

			String[] urls = { this.GetURLBase() };
			// needed for CAOs!
			_channelData = new ChannelDataStore(urls);

			if (_provider == null) 
			{
				_provider = new BinaryClientFormatterSinkProvider();
			}

			if (serverSinkProvider == null) 
			{
				serverSinkProvider = new BinaryServerFormatterSinkProvider();
			}

			// collect additional channel data from all providers
			IServerChannelSinkProvider prov = serverSinkProvider;
			while (prov != null) 
			{
				prov.GetChannelData(_channelData);
				prov = prov.Next;
			}			

			// create the sink chain
			IServerChannelSink snk = 
				ChannelServices.CreateServerChannelSinkChain(serverSinkProvider,this);

			// add the BidirTcpServerTransportSink as a first element to the chain
			_serverSink = new BidirTcpServerTransportSink(snk);
			MessageHandler.RegisterServer(_serverSink,Helper.GetMyGUID().ToString());
		}

		private String GetURLBase() 
		{
			return "BidirTcpGuid://" + Helper.GetMyGUID();
		}

		public string ChannelName 
		{
			get 
			{
				return _name;
			}
		}

		public int ChannelPriority 
		{
			get 
			{
				return 0;
			}
		}

		public string Parse(string url, out string objectURI) 
		{
			return Helper.ParseURL(url,out objectURI);
		}


		public IMessageSink CreateMessageSink(string url, object remoteChannelData, 
			out string objectURI) 
		{

			if (url == null && remoteChannelData != null && remoteChannelData as IChannelDataStore != null ) 
			{
				IChannelDataStore ds = (IChannelDataStore) remoteChannelData;
				url = ds.ChannelUris[0]; 
			}

			// format:   "BidirTCP://hostname:port/URI/to/object"
			if (url != null && (url.ToLower().StartsWith(Helper.TCP_PREFIX) || url.ToLower().StartsWith(Helper.TCPGUID_PREFIX))) 
			{
				// walk to last provider and add this channel sink's provider
				IClientChannelSinkProvider prov = _provider;
				while (prov.Next != null) { prov = prov.Next ;};
				prov.Next = new BidirTcpClientTransportSinkProvider(url);

				Helper.ParseURL(url, out objectURI);
				IMessageSink msgsink = (IMessageSink) _provider.CreateSink(this,url,remoteChannelData);
				return msgsink;
			} 
			else 
			{
				objectURI =null;
				return null;
			}
		}

		public void StartListening(object data)
		{
            // not needed		
		}

		public void StopListening(object data)
		{
			// not needed		
		
		}

		public string[] GetUrlsForUri(string objectURI) 
		{
			String[] urls;
			urls = new String[1];
			if (!(objectURI.StartsWith("/")))
				objectURI = "/" + objectURI;
			urls[0] = this.GetURLBase() + objectURI;
			return urls;
		}

		public object ChannelData 
		{
			get 
			{
				return _channelData;
			}
		}

	}
}
