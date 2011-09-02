/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005-2006 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using System.Threading;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Smuxi;
//using Smuxi.Channels.Tcp;
#if CHANNEL_TCPEX
using TcpEx;
#endif

namespace Smuxi.Server
{ 
    public class Server
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif

        public static void Init(string[] args)
        {
            Engine.Engine.Init();
            string channel = (string)Engine.Engine.Config["Server/Channel"];
            string formatter = (string)Engine.Engine.Config["Server/Formatter"];
            string host = (string)Engine.Engine.Config["Server/Host"];
            string bindAddress = (string)Engine.Engine.Config["Server/BindAddress"];
            int port = (int)Engine.Engine.Config["Server/Port"];
            IDictionary props = new Hashtable();
            props["port"] = port.ToString();
            if (host != null) {
                props["machineName"] = host;
            }
            if (bindAddress != null) {
                props["bindTo"] = bindAddress;
            }
            switch (channel) {
                case "TCP":
                    props["name"] = "TcpChannel";

                    BinaryClientFormatterSinkProvider cprovider =
                        new BinaryClientFormatterSinkProvider();

                    BinaryServerFormatterSinkProvider sprovider =
                        new BinaryServerFormatterSinkProvider();
                    // required for MS .NET 1.1
                    sprovider.TypeFilterLevel = TypeFilterLevel.Full;
#if LOG4NET
                    _Logger.Debug("Registering TcpChannel port: "+props["port"]);
#endif
                    try {
                        ChannelServices.RegisterChannel(new TcpChannel(props, cprovider, sprovider), false);
                    } catch (System.Net.Sockets.SocketException ex) {
                        Console.WriteLine("Could not register remoting channel on port {0} " +
                                          "(server already running on that port?) Error: " + ex.Message, port);
                        Environment.Exit(1);
                    }
                    break;
#if CHANNEL_TCPEX
                case "TcpEx":
                    props["name"] = "TcpExChannel";
#if LOG4NET
                    _Logger.Debug("Registering TcpExChannel port: "+props["port"]);
#endif            
                    ChannelServices.RegisterChannel(new TcpExChannel(props, null, null), false);
                    break;
#endif
                case "HTTP":
                    props["name"] = "HttpChannel";
#if LOG4NET
                    _Logger.Debug("Registering HttpChannel port: "+props["port"]);
#endif            
                    ChannelServices.RegisterChannel(new HttpChannel(props, null, null), false);
                    break;
                default:
                    Console.WriteLine("Unknown channel ("+channel+"), aborting...");
                    Environment.Exit(1);
                    break;
            }
            
            // register the SessionManager for .NET remoting
            RemotingServices.Marshal(Engine.Engine.SessionManager, "SessionManager");
            
#if LOG4NET
            _Logger.Info("Spawned remoting server with channel: "+channel+" formatter: "+formatter+" port: "+port);
#endif            
            
            Thread.CurrentThread.Join();
#if LOG4NET
            _Logger.Info("Shutting down remoting server...");
#endif
        }
    }
}
