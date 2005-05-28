/**
 * $Id: AssemblyInfo.cs 34 2004-09-05 14:46:59Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/Gnosmirc/trunk/src/AssemblyInfo.cs $
 * $Rev: 34 $
 * $Author: meebey $
 * $Date: 2004-09-05 16:46:59 +0200 (Sun, 05 Sep 2004) $
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
using System.Threading;

namespace Meebey.Smuxi.Engine
{
    public class FrontendManager : PermanentComponent, IFrontendUI
    {
        private int             _Version = 0;
        private Queue           _Queue  = Queue.Synchronized(new Queue());
        private Thread          _Thread;
        private Session         _Session;
        private IFrontendUI     _UI;
        private Page            _CurrentPage;
        private INetworkManager _CurrentNetworkManager;
        
        public int Version
        {
            get {
                return _Version;
            }
        }
        
        public Page CurrentPage
        {
            get {
                return _CurrentPage;
            }
            set {
                _CurrentPage = value;
            }
        }
        
        public INetworkManager CurrentNetworkManager
        {
            get {
                return _CurrentNetworkManager;
            }
            set {
                _CurrentNetworkManager = value;
            }
        }
        
        public FrontendManager(Session session, IFrontendUI ui)
        {
            _Session = session;
            _UI = ui;
            _Thread = new Thread(new ThreadStart(_Worker));
            _Thread.IsBackground = true;
            _Thread.Start();
            
            // sync all pages
            foreach (Page page in _Session.Pages) {
                AddPage(page);
                if (page.PageType == PageType.Channel) {
                    ChannelPage cpage = (ChannelPage)page;
                    // sync topic
                    UpdateTopicInChannel(cpage, cpage.Topic);
                    // sync all users
                    foreach (User user in cpage.Users.Values) {
                        AddUserToChannel(cpage, user);
                    }
                }
                
                /*
                if (page.PageType == PageType.Server) {
                    _CurrentPage = page;
                }
                */
            }
            
            // sync current network manager (if any exists)
            if (_Session.NetworkManagers.Count > 0) {
                INetworkManager nm = (INetworkManager)_Session.NetworkManagers[0];
                CurrentNetworkManager = nm;
            }
        }
        
        public void NextNetworkManager()
        {
            if (!(_Session.NetworkManagers.Count > 0)) {
                return;
            }
            
            int pos = _Session.NetworkManagers.IndexOf(CurrentNetworkManager);
            if (pos < _Session.NetworkManagers.Count-1) {
                pos++;
            } else {
                pos = 0;
            }
            CurrentNetworkManager = (INetworkManager)_Session.NetworkManagers[pos];
            UpdateNetworkStatus();
        }
        
        public void UpdateNetworkStatus()
        {
            if (CurrentNetworkManager != null) {
                SetNetworkStatus(CurrentNetworkManager.ToString());
            } else {
                SetNetworkStatus("(no network connections)");
            }
        }
        
        public void AddPage(Page page)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.AddPage, page));
        }
        
        public void AddTextToPage(Page page, string text)
        {
            string formated_timestamp;
            try {
                formated_timestamp = System.DateTime.Now.ToString((string)_Session.UserConfig["Interface/Notebook/TimestampFormat"]);
            } catch (FormatException e) {
                formated_timestamp = "Timestamp Format ERROR: "+e.Message;
            }
            string formated_text = formated_timestamp+" "+text;
            
            _Queue.Enqueue(new UICommandContainer(UICommand.AddTextToPage, page, formated_text));
        }
        
        public void AddTextToCurrentPage(string text)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.AddTextToPage, CurrentPage, text));
        }
        
        public void RemovePage(Page page)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.RemovePage, page));
        }
        
        public void AddUserToChannel(ChannelPage cpage, User user)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.AddUserToChannel, cpage, user));
        }
        
        public void UpdateUserInChannel(ChannelPage cpage, User olduser, User newuser)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.UpdateUserInChannel, cpage, olduser, newuser));
        }
    
        public void UpdateTopicInChannel(ChannelPage cpage, string topic)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.UpdateTopicInChannel, cpage, topic));
        }
    
        public void RemoveUserFromChannel(ChannelPage cpage, User user)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.RemoveUserFromChannel, cpage, user));
        }
        
        public void SetNetworkStatus(string status)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.SetNetworkStatus, status));
        }
        
        public void SetStatus(string status)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.SetStatus, status));
        }
        
        private void _Worker()
        {
            while (true) {
                if (_Queue.Count > 0) {
                    UICommandContainer com = (UICommandContainer)_Queue.Dequeue();
                    try {
                        switch (com.Command) {
                            case UICommand.AddPage:
                                _UI.AddPage((Page)com.Parameters[0]);
                            break;
                            case UICommand.AddTextToPage:
                                _UI.AddTextToPage((Page)com.Parameters[0],
                                    (string)com.Parameters[1]);
                            break;
                            case UICommand.RemovePage:
                                _UI.RemovePage((Page)com.Parameters[0]);
                            break;
                            case UICommand.AddUserToChannel:
                                _UI.AddUserToChannel((ChannelPage)com.Parameters[0],
                                    (User)com.Parameters[1]);
                            break;
                            case UICommand.UpdateUserInChannel:
                                _UI.UpdateUserInChannel((ChannelPage)com.Parameters[0],
                                    (User)com.Parameters[1], (User)com.Parameters[2]);
                            break;
                            case UICommand.UpdateTopicInChannel:
                                _UI.UpdateTopicInChannel((ChannelPage)com.Parameters[0],
                                    (string)com.Parameters[1]);
                            break;
                            case UICommand.RemoveUserFromChannel:
                                _UI.RemoveUserFromChannel((ChannelPage)com.Parameters[0],
                                    (User)com.Parameters[1]);
                            break;
                            case UICommand.SetNetworkStatus:
                                _UI.SetNetworkStatus((string)com.Parameters[0]);
                            break;
                            case UICommand.SetStatus:
                                _UI.SetStatus((string)com.Parameters[0]);
                            break;
                            default:
                            break;
                        }
                    } catch (System.Runtime.Remoting.RemotingException e) {
#if LOG4NET
                        Logger.Remoting.Error("RemotingException in _Worker(), aborting FrontendManager thread...", e);
                        Logger.Remoting.Error("Inner-Exception: ", e.InnerException);
#endif
                        _Session.DeregisterFrontendUI(_UI);
                        return;
                    } catch (Exception e) {
#if LOG4NET
                        Logger.Main.Error("Exception in _Worker(), aborting FrontendManager thread...", e);
                        Logger.Main.Error("Inner-Exception: ", e.InnerException);
#endif
                        _Session.DeregisterFrontendUI(_UI);
                        return;
                    }
                } else {
                    Thread.Sleep(10);
                }
            }
        }
    }
}
