/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007, 2010 Mirco Bauer <meebey@meebey.net>
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
using System.Runtime.Serialization;
using System.Collections.Generic;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    public class ServerModel : ISerializable
    {
        public bool UseEncryption { get; set; }
        public bool ValidateServerCertificate { get; set; }
        public string Protocol { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Network { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool OnStartupConnect { get; set; }
        public IList<string> OnConnectCommands { get; set; }
        public string ServerID { get; set; }
        
        protected string ConfigKeyPrefix {
            get {
                if (String.IsNullOrEmpty(Protocol)) {
                    throw new ArgumentNullException("Protocol");
                }
                if (String.IsNullOrEmpty(ServerID)) {
                    throw new ArgumentNullException("ServerID");
                }
                return "Servers/" + Protocol + "/" + ServerID + "/";
            }
        }
        
        public ServerModel()
        {
        }

        protected ServerModel(SerializationInfo info, StreamingContext ctx)
        {
            Protocol = info.GetString("_Protocol");
            Hostname = info.GetString("_Hostname");
            Port = info.GetInt32("_Port");
            Network = info.GetString("_Network");
            Username = info.GetString("_Username");
            Password = info.GetString("_Password");
            OnStartupConnect = info.GetBoolean("_OnStartupConnect");
            OnConnectCommands = (IList<string>) info.GetValue(
                "_OnConnectCommands",
                typeof(IList<string>)
            );
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            info.AddValue("_Protocol", Protocol);
            info.AddValue("_Hostname", Hostname);
            info.AddValue("_Port", Port);
            info.AddValue("_Network", Network);
            info.AddValue("_Username", Username);
            info.AddValue("_Password", Password);
            info.AddValue("_OnStartupConnect", OnStartupConnect);
            info.AddValue("_OnConnectCommands", OnConnectCommands);
        }
        
        public virtual void Load(UserConfig config, string protocol, string id)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            if (String.IsNullOrEmpty(protocol)) {
                throw new ArgumentNullException("protocol");
            }
            if (String.IsNullOrEmpty(id)) {
                throw new ArgumentNullException("id");
            }
            // don't use ConfigKeyPrefix, so exception guarantees can be kept
            string prefix = "Servers/" + protocol + "/" + id + "/";
            if (config[prefix + "Hostname"] == null) {
                // server does not exist
                throw new ArgumentException("ServerID not found in config", id);
            }
            ServerID    = id;
            Protocol    = protocol;
            // now we have a valid ServerID and Protocol, ConfigKeyPrefix works
            Hostname    = (string) config[ConfigKeyPrefix + "Hostname"];
            Port        = (int)    config[ConfigKeyPrefix + "Port"];
            Network     = (string) config[ConfigKeyPrefix + "Network"];
            Username    = (string) config[ConfigKeyPrefix + "Username"];
            Password    = (string) config[ConfigKeyPrefix + "Password"];
            UseEncryption = (bool) config[ConfigKeyPrefix + "UseEncryption"];
            ValidateServerCertificate =
                (bool) config[ConfigKeyPrefix + "ValidateServerCertificate"];
            if (config[ConfigKeyPrefix + "OnStartupConnect"] != null) {
                OnStartupConnect = (bool) config[ConfigKeyPrefix + "OnStartupConnect"];
            }
            OnConnectCommands  = config[ConfigKeyPrefix + "OnConnectCommands"] as IList<string>;
        }
        
        public virtual void Save(UserConfig config)
        {
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            config[ConfigKeyPrefix + "Hostname"] = Hostname;
            config[ConfigKeyPrefix + "Port"]     = Port;
            config[ConfigKeyPrefix + "Network"]  = Network;
            config[ConfigKeyPrefix + "Username"] = Username;
            config[ConfigKeyPrefix + "Password"] = Password;
            config[ConfigKeyPrefix + "UseEncryption"] = UseEncryption;
            config[ConfigKeyPrefix + "ValidateServerCertificate"] =
                ValidateServerCertificate;
            config[ConfigKeyPrefix + "OnStartupConnect"] = OnStartupConnect;
            config[ConfigKeyPrefix + "OnConnectCommands"] = OnConnectCommands;
        }

        public override string ToString()
        {
            return String.Format("<{0}>", ToTraceString());
        }

        public string ToTraceString()
        {
            return String.Format("{0}/{1}", Protocol, ServerID);
        }
    }
}
