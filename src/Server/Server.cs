/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2005 Mirco Bauer <meebey@meebey.net>
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
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Serialization.Formatters;
using Meebey.Smuxi;
#if CHANNEL_TCPEX
using TcpEx;
#endif

namespace Meebey.Smuxi.Server
{ 
    public class Server
    {
        public static void Init(string[] args)
        {
            System.Threading.Thread.CurrentThread.Name = "Main";
            Engine.Engine.Init();
            string channel = (string)Engine.Engine.Config["Server/Channel"];
            string formatter = (string)Engine.Engine.Config["Server/Formatter"];
            string host = (string)Engine.Engine.Config["Server/Host"];
            int port = (int)Engine.Engine.Config["Server/Port"];
            IDictionary props = new Hashtable();
            props["typeFilterLevel"] = TypeFilterLevel.Full;
            props["port"] = port.ToString();
            if (host != null) {
                props["machineName"] = host;
            } 
            switch (channel) {
                case "TCP":
                    props["name"] = "TcpChannel";
#if LOG4NET
                    Engine.Logger.Remoting.Debug("Registering TcpChannel port: "+props["port"]);
#endif            
                    ChannelServices.RegisterChannel(new TcpChannel(props, null, null));
                    break;
#if CHANNEL_TCPEX
                case "TcpEx":
                    props["name"] = "TcpExChannel";
#if LOG4NET
                    Engine.Logger.Remoting.Debug("Registering TcpExChannel port: "+props["port"]);
#endif            
                    ChannelServices.RegisterChannel(new TcpExChannel(props, null, null));
                    break;
#endif
                case "HTTP":
                    props["name"] = "HttpChannel";
#if LOG4NET
                    Engine.Logger.Remoting.Debug("Registering HttpChannel port: "+props["port"]);
#endif            
                    ChannelServices.RegisterChannel(new HttpChannel(props, null, null));
                    break;
                default:
                    Console.WriteLine("Unknown channel ("+channel+"), aborting...");
                    Environment.Exit(1);
                    break;
            }
            
            // register the SessionManager for .NET remoting
            RemotingServices.Marshal(Engine.Engine.SessionManager, "SessionManager");
            
#if LOG4NET
            Engine.Logger.Remoting.Info("Spawned remoting server with channel: "+channel+" formatter: "+formatter+" port: "+port);
#endif            
            
            while (true) {
                System.Console.ReadLine();
            } 
        }
    }
}
