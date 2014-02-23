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
using System.Collections;
using Smuxi.Common;

namespace Smuxi.Engine
{
    public class SessionManager : PermanentRemoteObject
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Hashtable _Sessions = Hashtable.Synchronized(new Hashtable());
        private Version   _EngineVersion;
        private Config    _Config;
        private ProtocolManagerFactory _ProtocolManagerFactory;
        
        public Version EngineVersion {
            get {
                return _EngineVersion;
            }
        }
        
        public SessionManager(Config config, ProtocolManagerFactory protocolManagerFactory)
        {
            Trace.Call(config, protocolManagerFactory);
            
            if (config == null) {
                throw new ArgumentNullException("config");
            }
            if (protocolManagerFactory == null) {
                throw new ArgumentNullException("protocolManagerFactory");
            }
            
            _Config = config;
            _ProtocolManagerFactory = protocolManagerFactory;

            // BUG: out of scope?
            _EngineVersion = Engine.Version;

            string[] users = (string[])Engine.Config["Engine/Users/Users"];
            if (users == null) {
                Console.WriteLine("No Engine/Users/*, aborting...\n");
                Environment.Exit(1);
            }
            foreach (string user in users) {
                // skip local session
                if (user == "local") {
                    continue;
                }
#if LOG4NET
                _Logger.Debug("Creating Session for User "+user);
#endif
                _Sessions.Add(user, new Session(_Config, _ProtocolManagerFactory, user));
            }
        }
        
        public Session Register(string username, string password, IFrontendUI ui)
        {
            Trace.Call(username, "XXX", ui);
            
            if (username == null) {
                throw new ArgumentNullException("username");
            }
            if (password == null) {
                throw new ArgumentNullException("password");
            }
            if (ui == null) {
                throw new ArgumentNullException("ui");
            }
            
            string configPassword = (string)Engine.Config["Engine/Users/"+username+"/Password"]; 
            if (configPassword == null ||
                configPassword == String.Empty) {
                return null;
            }
            
            // calculate MD5 string from config password
            configPassword = MD5.FromString(configPassword);
            
            if (configPassword == password) {
                Session sess = (Session)_Sessions[username];
                sess.RegisterFrontendUI(ui);
                return sess;
            }
            
            return null;
        }

        internal void Shutdown()
        {
            lock (_Sessions) {
                foreach (Session session in _Sessions.Values) {
                    session.Shutdown();
                }
            }
        }
    }
}
