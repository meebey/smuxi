/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2007, 2010, 2012-2014, 2017-2018 Mirco Bauer <meebey@meebey.net>
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
        public string ClientCertificateFilename { get; set; }
        public string Protocol { get; set; }
        public string Hostname { get; set; }
        public int Port { get; set; }
        public string Network { get; set; }
        public string Nickname { get; set; }
        public string Realname { get; set; }
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

        public ServerModel(ServerModel server)
        {
            if (server == null) {
                throw new ArgumentNullException("server");
            }

            UseEncryption = server.UseEncryption;
            ValidateServerCertificate = server.ValidateServerCertificate;
            ClientCertificateFilename = server.ClientCertificateFilename;
            Protocol = server.Protocol;
            Hostname = server.Hostname;
            Port = server.Port;
            Network = server.Network;
            Nickname = server.Nickname;
            Realname = server.Realname;
            Username = server.Username;
            Password = server.Password;
            OnStartupConnect = server.OnStartupConnect;
            OnConnectCommands = new List<string>(server.OnConnectCommands);
            ServerID = server.ServerID;
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
            //ServerID = info.GetString("_ServerID");
            bool foundServerID = false;
            bool foundEncryption = false;
            bool foundValidation = false;
            foreach(SerializationEntry e in info) {
                switch (e.Name) {
                    case "_ServerID":
                        ServerID = (string)e.Value;
                        foundServerID = true;
                        break;
                    case "_Username":
                        Username = (string) e.Value;
                        break;
                    case "_Nickname":
                        Nickname = (string) e.Value;
                        break;
                    case "_Realname":
                        Realname = (string) e.Value;
                        break;
                    // UseEncryption and ValidateServerCertificate were forgotten
                    // when moving from autoserialization to manual serialization.
                    // To prevent crashes when git users' updated engines receive a ServerModel
                    // from an older git frontend, we manually check for the fields' existance
                    case "<UseEncryption>k__BackingField":
                        UseEncryption = (bool)e.Value;
                        foundEncryption = true;
                        break;
                    case "<ValidateServerCertificate>k__BackingField":
                        ValidateServerCertificate = (bool)e.Value;
                        foundValidation = true;
                        break;
                    case "ClientCertificateFilename":
                        ClientCertificateFilename = (string) e.Value;
                        break;
                }
            }
            if (foundServerID == false) {
                // this is from an old frontend/engine that doesn't know about ServerID yet
                ServerID = Hostname;
            }
            if (!foundEncryption) {
                UseEncryption = false;
            }
            if (!foundValidation) {
                ValidateServerCertificate = false;
            }
            OnConnectCommands = (IList<string>) info.GetValue(
                "_OnConnectCommands",
                typeof(IList<string>)
            );
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext ctx) 
        {
            // HACK: skip ServerID if it has no value as it breaks older
            // ServerModel implementations that relied on automatic
            // serialization which was the case in < 0.8.11
            if (ServerID != null) {
                info.AddValue("_ServerID", ServerID);
            }
            if (Nickname != null) {
                info.AddValue("_Nickname", Nickname);
            }
            if (Realname != null) {
                info.AddValue("_Realname", Realname);
            }
            // HACK: skip ClientCertificateFilename if it has no value as it
            // breaks older ServerModel implementations that relied on automatic
            // serialization which was the case in < 0.8.11
            if (!String.IsNullOrEmpty(ClientCertificateFilename)) {
                info.AddValue("ClientCertificateFilename", ClientCertificateFilename);
            }
            info.AddValue("_Protocol", Protocol);
            info.AddValue("_Hostname", Hostname);
            info.AddValue("_Port", Port);
            info.AddValue("_Network", Network);
            info.AddValue("_Username", Username);
            info.AddValue("_Password", Password);
            info.AddValue("_OnStartupConnect", OnStartupConnect);
            info.AddValue("_OnConnectCommands", OnConnectCommands);
            // oddball names are necessary because the fields always were auto properties
            info.AddValue("<UseEncryption>k__BackingField", UseEncryption);
            info.AddValue("<ValidateServerCertificate>k__BackingField", ValidateServerCertificate);
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
            Nickname = (string) config[ConfigKeyPrefix + "Nickname"];
            Realname = (string) config[ConfigKeyPrefix + "Realname"];
            Username    = (string) config[ConfigKeyPrefix + "Username"];
            Password    = (string) config[ConfigKeyPrefix + "Password"];
            UseEncryption = (bool) config[ConfigKeyPrefix + "UseEncryption"];
            ValidateServerCertificate =
                (bool) config[ConfigKeyPrefix + "ValidateServerCertificate"];
            ClientCertificateFilename = (string) config[ConfigKeyPrefix + "ClientCertificateFilename"];
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
            config[ConfigKeyPrefix + "Nickname"]  = Nickname;
            config[ConfigKeyPrefix + "Realname"]  = Realname;
            config[ConfigKeyPrefix + "Username"] = Username;
            config[ConfigKeyPrefix + "Password"] = Password;
            config[ConfigKeyPrefix + "UseEncryption"] = UseEncryption;
            config[ConfigKeyPrefix + "ValidateServerCertificate"] =
                ValidateServerCertificate;
            config[ConfigKeyPrefix + "ClientCertificateFilename"] =
                ClientCertificateFilename;
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
