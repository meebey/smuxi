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

	public class BidirTcpServerChannel: BaseChannelWithProperties, 
		IChannelReceiver, IChannelSender,
		IChannel
	{
		private BidirTcpServerTransportSink _transportSink;
		private IServerChannelSinkProvider _sinkProvider;
		private IClientChannelSinkProvider _clientProvider;
		private IDictionary _properties;

		private ChannelDataStore _channelData;
		private int _port;
		private String _name;

		public BidirTcpServerChannel(IDictionary properties, IClientChannelSinkProvider clientSinkProvider, IServerChannelSinkProvider serverSinkProvider)
		{
			_name = (String) properties["name"];
			if (_name == null) 
			{
				_name = "BidirTcpServer";
			}
			_port = int.Parse((String) properties["port"]);

			String[] urls = { this.GetURLBase() };

			// needed for CAOs!
			_channelData = new ChannelDataStore(urls);
			
			String IPAddress = Helper.GetIPAddress();

			if (serverSinkProvider == null) 
			{
				serverSinkProvider = new BinaryServerFormatterSinkProvider();
			}

			if (_clientProvider == null) 
			{
				_clientProvider = new BinaryClientFormatterSinkProvider();
			}

			_sinkProvider = serverSinkProvider;

			// collect channel data from all providers
			IServerChannelSinkProvider provider = _sinkProvider;
			while (provider != null) 
			{
				provider.GetChannelData(_channelData);
				provider = provider.Next;
			}			

			// create the sink chain
			IServerChannelSink snk = 
				ChannelServices.CreateServerChannelSinkChain(_sinkProvider,this);

			// add the BidirTcpServerTransportSink as a first element to the chain
			_transportSink = new BidirTcpServerTransportSink(snk, _port ,IPAddress);
			MessageHandler.RegisterServer(_transportSink,Helper.GetIPAddress() + ":" + _port);
			MessageHandler.RegisterServer(_transportSink,Helper.GetMyGUID().ToString());

			// start to listen
			this.StartListening(null);
		}

		private String GetURLBase() 
		{
			return "BidirTcpGuid://" + Helper.GetMyGUID();
//			return "BidirTcp://" + Helper.GetIPAddress() + ":" + _port ;
		}

		public string Parse(string url, out string objectURI) 
		{
			return Helper.ParseURL(url, out objectURI);
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

		public void StartListening(object data) 
		{
			// TODO: Start listening
			ConnectionManager.StartListening(_port);
		}

		public void StopListening(object data) 
		{
			// TODO: Stop to listen
			
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

		public System.Runtime.Remoting.Messaging.IMessageSink CreateMessageSink(string url, object remoteChannelData, out string objectURI)
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
				IClientChannelSinkProvider prov = _clientProvider;
				while (prov.Next != null) { prov = prov.Next ;};
				prov.Next = new BidirTcpClientTransportSinkProvider(url);

				Helper.ParseURL(url, out objectURI);
				IMessageSink msgsink = (IMessageSink) _clientProvider.CreateSink(this,url,remoteChannelData);
				return msgsink;
			} 
			else 
			{
				objectURI =null;
				return null;
			}		
		}
	}
}
