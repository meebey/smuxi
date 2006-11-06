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
using System.Collections;
using System.Threading;

namespace Meebey.Smuxi.Engine
{
    public delegate void SimpleDelegate(); 
    
    public class FrontendManager : PermanentRemoteObject, IFrontendUI
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private int             _Version = 0;
        private Queue           _Queue  = Queue.Synchronized(new Queue());
        private Thread          _Thread;
        private Session         _Session;
        private IFrontendUI     _UI;
        private Page            _CurrentPage;
        private INetworkManager _CurrentNetworkManager;
        private bool            _IsFrontendDisconnecting;
        private SimpleDelegate  _ConfigChangedDelegate;
        private ArrayList       _SyncedPages = new ArrayList();
        
        public int Version {
            get {
                return _Version;
            }
        }
        
        public SimpleDelegate ConfigChangedDelegate {
            set {
                _ConfigChangedDelegate = value;
            }
        }
        
        public Page CurrentPage {
            get {
                return _CurrentPage;
            }
            set {
                _CurrentPage = value;
            }
        }
        
        public INetworkManager CurrentNetworkManager {
            get {
                return _CurrentNetworkManager;
            }
            set {
                _CurrentNetworkManager = value;
            }
        }
        
        public bool IsFrontendDisconnecting {
            get {
                return _IsFrontendDisconnecting;
            }
            set {
                _IsFrontendDisconnecting = value;
            }
        }
        
        public FrontendManager(Session session, IFrontendUI ui)
        {
            _Session = session;
            _UI = ui;
            _Thread = new Thread(new ThreadStart(_Worker));
            _Thread.IsBackground = true;
            _Thread.Start();
            
            // register event for config invalidation
            _Session.Config.Changed += new EventHandler(_OnConfigChanged);
            
            // BUG: Session adds stuff to the queue but the frontend is not ready yet!
            // The frontend must Sync() _first_!
            // HACK: so this bug doesn't happen for now
            //Sync();
        }
        
        public void Sync()
        {
            // sync pages            
            foreach (Page page in _Session.Pages) {
                AddPage(page);
            }
            
            // sync current network manager (if any exists)
            if (_Session.NetworkManagers.Count > 0) {
                INetworkManager nm = (INetworkManager)_Session.NetworkManagers[0];
                CurrentNetworkManager = nm;
            }
            
            // sync current page
            _CurrentPage = (Page)_Session.Pages[0];
            
            // sync content of pages
            foreach (Page page in _Session.Pages) {
                SyncPage(page);
            }
        }
        
        public void AddSyncedPage(Page page)
        {
            _SyncedPages.Add(page);
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
                SetNetworkStatus(String.Format("({0})", _("no network connections")));
            }
        }
        
        public void AddPage(Page page)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.AddPage, page));
        }
        
        public void AddTextToPage(Page page, string text)
        {
            AddMessageToPage(page, new FormattedMessage(text));
        }
        
        public void AddTextToCurrentPage(string text)
        {
            AddTextToPage(CurrentPage, text);
        }
        
        public void EnablePage(Page page)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.EnablePage, page));
        }
        
        public void DisablePage(Page page)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.DisablePage, page));
        }
        
        public void AddMessageToPage(Page page, FormattedMessage fmsg)
        {
            if (_SyncedPages.Contains(page)) {
                _AddMessageToPage(page, fmsg);
            }
        }
        
        private void _AddMessageToPage(Page page, FormattedMessage fmsg)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.AddMessageToPage, page, fmsg));
        }
        
        public void AddMessageToCurrentPage(FormattedMessage fmsg)
        {
            AddMessageToPage(CurrentPage, fmsg);
        }
        
        public void RemovePage(Page page)
        {
            if (_SyncedPages.Contains(page)) {
                _RemovePage(page);
            }
        }
        
        private void _RemovePage(Page page)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.RemovePage, page));
        }
        
        public void SyncPage(Page page)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.SyncPage, page));
        }
                
        public void AddUserToChannel(ChannelPage cpage, User user)
        {
            if (_SyncedPages.Contains(cpage)) {
                _AddUserToChannel(cpage, user);
            }
        }
        
        private void _AddUserToChannel(ChannelPage cpage, User user)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.AddUserToChannel, cpage, user));
        }
        
        public void UpdateUserInChannel(ChannelPage cpage, User olduser, User newuser)
        {
            if (_SyncedPages.Contains(cpage)) {
                _UpdateUserInChannel(cpage, olduser, newuser);
            }
        }
        
        private void _UpdateUserInChannel(ChannelPage cpage, User olduser, User newuser)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.UpdateUserInChannel, cpage, olduser, newuser));
        }
    
        public void UpdateTopicInChannel(ChannelPage cpage, string topic)
        {
            if (_SyncedPages.Contains(cpage)) {
                _UpdateTopicInChannel(cpage, topic);
            }
        }
        
        private void _UpdateTopicInChannel(ChannelPage cpage, string topic)
        {
            _Queue.Enqueue(new UICommandContainer(UICommand.UpdateTopicInChannel, cpage, topic));
        }
    
        public void RemoveUserFromChannel(ChannelPage cpage, User user)
        {
            if (_SyncedPages.Contains(cpage)) {
                _RemoveUserFromChannel(cpage, user);
            }
        }
        
        private void _RemoveUserFromChannel(ChannelPage cpage, User user)
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
                            case UICommand.RemovePage:
                                _UI.RemovePage((Page)com.Parameters[0]);
                                break;
                            case UICommand.EnablePage:
                                _UI.EnablePage((Page)com.Parameters[0]);
                                break;
                            case UICommand.DisablePage:
                                _UI.DisablePage((Page)com.Parameters[0]);
                                break;
                            case UICommand.SyncPage:
                                _UI.SyncPage((Page)com.Parameters[0]);
                                break;
                            case UICommand.AddMessageToPage:
                                _UI.AddMessageToPage((Page)com.Parameters[0],
                                    (FormattedMessage)com.Parameters[1]);
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
#if LOG4NET
                                _Logger.Error("_Worker(): Unknown UICommand: "+com.Command);
#endif
                                break;
                        }
                    } catch (System.Runtime.Remoting.RemotingException e) {
#if LOG4NET
                        if (!_IsFrontendDisconnecting) {
                            // we didn't expect this problem
                            _Logger.Error("RemotingException in _Worker(), aborting FrontendManager thread...", e);
                            _Logger.Error("Inner-Exception: ", e.InnerException);
                        }
#endif
                        _Session.DeregisterFrontendUI(_UI);
                        return;
                    } catch (Exception e) {
#if LOG4NET
                        _Logger.Error("Exception in _Worker(), aborting FrontendManager thread...", e);
                        _Logger.Error("Inner-Exception: ", e.InnerException);
#endif
                        _Session.DeregisterFrontendUI(_UI);
                        return;
                    }
                } else {
                    Thread.Sleep(10);
                }
            }
        }
        
        private void _OnConfigChanged(object sender, EventArgs e)
        {
            if (_ConfigChangedDelegate != null) {
                _ConfigChangedDelegate();
            }
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
