/*
 * $Id$
 * $URL$
 * $Rev$
 * $Author$
 * $Date$
 *
 * smuxi - Smart MUltipleXed Irc
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
using Meebey.Smuxi.Engine;
using Meebey.Smuxi.Common;

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
            Trace.Call(page);
            
            Console.WriteLine("New page: "+page.Name+ " type: "+page.PageType);
            Frontend.ChangeActivePage(page);
        }
        
        public void AddMessageToPage(Page page, FormattedMessage fmsg)
        {
            Trace.Call(page, fmsg);

            string msg = String.Empty;
            foreach (FormattedMessageItem item in fmsg.Items) {
                switch (item.Type) {
                    // TODO: implement other ItemTypes
                    case FormattedMessageItemType.Text:
                        FormattedMessageTextItem fmsgti = (FormattedMessageTextItem)item.Value;
                        msg += fmsgti.Text;
                        break; 
                } 
            }
            
            string timestamp;
            try {
                timestamp = fmsg.Timestamp.ToLocalTime().ToString((string)Frontend.UserConfig["Interface/Notebook/TimestampFormat"]);
            } catch (FormatException e) {
                timestamp = "Timestamp Format ERROR: "+e.Message;
            }
            msg = timestamp+" "+page.Name+" "+msg;
            
            Console.WriteLine(msg);
        }
        
        public void RemovePage(Page page)
        {
            Trace.Call(page);
            
            Console.WriteLine("Removed page: "+page.Name+" type: "+page.PageType);
        }
        
        public void EnablePage(Page page)
        {
            Trace.Call(page);
        }
        
        public void DisablePage(Page page)
        {
            Trace.Call(page);
        }
        
        public void SyncPage(Page page)
        {
            Trace.Call(page);
            
            Console.WriteLine("Synced page: "+page.Name+" type: "+page.PageType);
        }
        
        public void AddUserToChannel(ChannelPage cpage, User user)
        {
            Trace.Call(cpage, user);
        }
        
        public void UpdateUserInChannel(ChannelPage cpage, User olduser, User newuser)
        {
            Trace.Call(cpage, olduser, newuser);
        }
    
        public void UpdateTopicInChannel(ChannelPage cpage, string topic)
        {
            Trace.Call(cpage, topic);
            
            Console.WriteLine("Topic changed to: "+topic+ " on "+cpage.Name);
        }
        
        public void RemoveUserFromChannel(ChannelPage cpage, User user)
        {
            Trace.Call(cpage, user);
        }

        public void SetNetworkStatus(string status)
        {
            Trace.Call(status);
        }

        public void SetStatus(string status)
        {
            Trace.Call(status);
        }
    }
}
