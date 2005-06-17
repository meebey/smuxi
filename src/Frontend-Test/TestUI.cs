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
using Meebey.Smuxi.Engine;

namespace Meebey.Smuxi.FrontendTest
{
    public class TestUI : PermanentRemoteObject, IFrontendUI 
    {
        private int _Version = 0;
        
        public int Version
        {
            get {
                return _Version;
            }
        }
        
        public void AddPage(Page page)
        {
            Console.WriteLine("AddPage()");
        }
        
        public void AddTextToPage(Page page, string text)
        {
            Console.WriteLine("AddTextToPage()");
        }
        
        public void RemovePage(Page page)
        {
            Console.WriteLine("RemovePage()");
        }
        
        public void AddUserToChannel(ChannelPage cpage, User user)
        {
            Console.WriteLine("AddUserToChannel()");
        }
        
        public void UpdateUserInChannel(ChannelPage cpage, User olduser, User newuser)
        {
            Console.WriteLine("UpdateUserInChannel()");
        }
    
        public void UpdateTopicInChannel(ChannelPage cpage, string topic)
        {
            Console.WriteLine("UpdateTopicInChannel()");
        }
        
        public void RemoveUserFromChannel(ChannelPage cpage, User user)
        {
            Console.WriteLine("RemoveUserFromChannel()");
        }

        public void SetNetworkStatus(string status)
        {
            Console.WriteLine("SetNetworkStatus()");
        }

        public void SetStatus(string status)
        {
            Console.WriteLine("SetStatus()");
        }
    }
}
