/**
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

namespace Meebey.Smuxi.Engine
{
    public class SessionManager : PermanentComponent
    {
        private Hashtable _Sessions = Hashtable.Synchronized(new Hashtable());
        
        public SessionManager()
        {
            string[] users_list = (string[])Engine.Config["Engine/Users/UsersList"];
            if (users_list == null) {
                Console.WriteLine("No Engine/Users/*, aborting...\n");
                Environment.Exit(1);
            }
            foreach (string user in users_list) {
#if LOG4NET
                Logger.Session.Debug("Creating Session for User "+user);
#endif
                _Sessions.Add(user, new Session(Engine.Config, user));
            }
        }
        
        public Session Register(string username, string password, IFrontendUI ui)
        {
            object obj;
            if ((obj = Engine.Config["Engine/Users/"+username+"/Password"]) != null) {
                if ((string)obj == password) {
                    Session sess = (Session)_Sessions[username];
                    sess.RegisterFrontendUI(ui);
                    return sess;
                }
            }
            
            return null;
        }
    }
}
