// Smuxi - Smart MUltipleXed Irc
//
// Copyright (c) 2013 Mirco Bauer <meebey@meebey.net>
//
// Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
using System;
using System.IO;
using System.Net.Sockets;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Starksoft.Net.Proxy;
using StarkProxyType = Starksoft.Net.Proxy.ProxyType;

namespace Smuxi.Engine
{
    public class TorChatOutConnection : TorChatConnection
    {
        string TorHostname { get; set; }
        int TorPort { get; set; }

        public TorChatOutConnection()
        {
            TorHostname = "localhost";
            TorPort = 9050;

            TcpClient = new TcpClient();
            TcpClient.NoDelay = true;
            TcpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
            // set timeout, after this the connection will be aborted
            TcpClient.ReceiveTimeout = SocketReceiveTimeout * 1000;
            TcpClient.SendTimeout = SocketSendTimeout * 1000;
        }

        public void Connect(string onionAddress)
        {
            if (!onionAddress.EndsWith(".onion")) {
                onionAddress = String.Format("{0}.onion", onionAddress);
            }
            var proxyFactory = new ProxyClientFactory();
            // SOCKS5 wants a user/pass for some reason
            //var proxyClient = proxyFactory.CreateProxyClient(StarkProxyType.Socks5);
            var proxyClient = proxyFactory.CreateProxyClient(StarkProxyType.Socks4a);
            TcpClient.Connect(TorHostname, TorPort);
            proxyClient.TcpClient = TcpClient;
            proxyClient.CreateConnection(onionAddress, 11009);
            var stream = TcpClient.GetStream();
            BeginReadStream(stream);
            var writer = new StreamWriter(stream, Encoding.UTF8);
            writer.Write("foobar" + MSG_SEPARATOR);
            writer.Flush();
        }
    }
}
