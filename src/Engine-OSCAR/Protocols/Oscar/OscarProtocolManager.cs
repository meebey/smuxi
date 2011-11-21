/*
 * $Id: IrcProtocolManager.cs 149 2007-04-11 16:47:52Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcProtocolManager.cs $
 * $Rev: 149 $
 * $Author: meebey $
 * $Date: 2007-04-11 18:47:52 +0200 (Wed, 11 Apr 2007) $
 *
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008 Mirco Bauer <meebey@meebey.net>
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using csammisrun.OscarLib;
using OscarSession = csammisrun.OscarLib.Session;

using Smuxi.Common;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "ICQ", Description = "ICQ Messenger", Alias = "icq")]
    public class IcqProtocolManager : OscarProtocolManager
    {
        public override string NetworkID {
            get {
                return "ICQ";
            }
        }
        
        public IcqProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "AIM", Description = "AOL Instant Messenger", Alias = "aim")]
    public class AimProtocolManager : OscarProtocolManager
    {
        public override string NetworkID {
            get {
                return "AIM";
            }
        }
        
        public AimProtocolManager(Session session) : base(session)
        {
        }
    }
    
    //[ProtocolManagerInfo(Name = "OSCAR", Description = "Open System for CommunicAtion in Realtime Protocol", Alias = "oscar")]
    public abstract class OscarProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private FrontendManager _FrontendManager;
        private ChatModel       _NetworkChat;
        private OscarSession    _OscarSession;
        
        public override string Protocol {
            get {
                return "OSCAR";
            }
        }
        
        public override ChatModel Chat {
            get {
                return _NetworkChat;
            }
        }

        public OscarProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
        }
        
        public override void Connect(FrontendManager fm, ServerModel server)
        {
            Trace.Call(fm, server);
            
            if (fm == null) {
                throw new ArgumentNullException("fm");
            }
            if (server == null) {
                throw new ArgumentNullException("server");
            }

            _FrontendManager = fm;
            Host = "login.oscar.aol.com";
            Port = 5190;
            
            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new ProtocolChatModel(NetworkID, NetworkID + " Messenger", this);
            Session.AddChat(_NetworkChat);
            Session.SyncChat(_NetworkChat);
            
            _OscarSession = new OscarSession(server.Username, server.Password);
            _OscarSession.ClientCapabilities = Capabilities.Chat | Capabilities.OscarLib;
            _OscarSession.LoginCompleted           += new LoginCompletedHandler(_OnLoginCompleted);
            _OscarSession.LoginFailed              += new LoginFailedHandler(_OnLoginFailed);
            _OscarSession.LoginStatusUpdate        += new LoginStatusUpdateHandler(_OnLoginStatusUpdate);
            _OscarSession.ErrorMessage             += new ErrorMessageHandler(_OnErrorMessage);
            _OscarSession.WarningMessage           += new WarningMessageHandler(_OnWarningMessage);
            _OscarSession.StatusUpdate             += new InformationMessageHandler(_OnStatusUpdate);
            _OscarSession.ContactListFinished      += new ContactListFinishedHandler(_OnContactListFinished);
            _OscarSession.Messages.MessageReceived += new MessageReceivedHandler(_OnMessageReceived);
            _OscarSession.Logon(Host, Port);
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
            
            _OscarSession.Logoff();
        }
        
        public override string ToString()
        {
            string result = NetworkID;
            
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }
        
        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            throw new NotImplementedException();
        }
        
        
        public override void OpenChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
            
            throw new NotImplementedException();
        }

        public override void CloseChat(FrontendManager fm, ChatModel chat)
        {
            Trace.Call(fm, chat);
            
            throw new NotImplementedException();
        }
        

        public override void SetPresenceStatus(PresenceStatus status,
                                               string message)
        {
            Trace.Call(status, message);

            // TODO: implement me
        }

        public override bool Command(CommandModel command)
        {
            Trace.Call(command);
            
            bool handled = false;
            if (IsConnected) {
                if (command.IsCommand) {
                } else {
                    _Say(command.Chat, command.Data);
                    handled = true;
                }
            } else {
                if (command.IsCommand) {
                    // commands which work even without beeing connected
                    switch (command.Command) {
                        case "help":
                            CommandHelp(command);
                            handled = true;
                            break;
                        case "connect":
                            CommandConnect(command);
                            handled = true;
                            break;
                    }
                } else {
                    // normal text, without connection
                    NotConnected(command);
                    handled = true;
                }
            }
            
            return handled;
        }

        public void CommandConnect(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            
            var server = new ServerModel();
            if (cd.DataArray.Length >= 3) {
                server.Username = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            if (cd.DataArray.Length >= 4) {
                server.Password = cd.DataArray[3];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            Connect(fm, server);
        }
        
        public void CommandHelp(CommandModel cd)
        {
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;

            fmsgti = new TextMessagePartModel();
            // TRANSLATOR: this line is used as a label / category for a
            // list of commands below. {0} would be AIM or ICQ for example.
            fmsgti.Text = "[" + String.Format(_("{0} Commands"), NetworkID) + "]";
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);
            
            this.Session.AddMessageToChat(cd.Chat, fmsg);
            
            string[] help = {
            "help",
            "connect aim/icq username password",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToChat(cd.Chat, "-!- " + line);
            }
        }
        
        private void _Say(ChatModel chat, string message)
        {
            if (!chat.IsEnabled) {
                return;
            }

            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;
            
            _OscarSession.Messages.SendMessage(chat.ID, message);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "<";
            msg.MessageParts.Add(msgPart);
        
            msgPart = new TextMessagePartModel();
            msgPart.Text = _OscarSession.ScreenName;
            msgPart.ForegroundColor = new TextColor(0x0000FF);
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "> ";
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = message;
            msg.MessageParts.Add(msgPart);
            
            Session.AddMessageToChat(chat, msg);
        }
        
        private void _OnLoginCompleted(OscarSession sess)
        {
            IsConnected = true;
            
            string msg = _("Login successful");
            Session.AddTextToChat(_NetworkChat, "-!- " + msg);
        }

        private void _OnLoginStatusUpdate(OscarSession sess, string messages, double percent)
        {
            Session.AddTextToChat(_NetworkChat, "-!- Login: " + messages);
        }
        
        private void _OnLoginFailed(OscarSession sess, LoginErrorCode errorCode)
        {
            string msg = String.Format(_("Login failed: {0}"), errorCode);
            Session.AddTextToChat(_NetworkChat, "-!- " + msg);
        }

        private void _OnStatusUpdate(OscarSession sess, string message)
        {
            Session.AddTextToChat(_NetworkChat, "-!- Status: " + message);
        }
        
        private void _OnErrorMessage(OscarSession sess, ServerErrorCode errorCode)
        {
            string msg = String.Format(_("Connection Error: {0}"), errorCode);
            Session.AddTextToChat(_NetworkChat, "-!- " + msg);
        }

        private void _OnWarningMessage(OscarSession sess, ServerErrorCode errorCode)
        {
            string msg = String.Format(_("Connection Warning: {0}"), errorCode);
            Session.AddTextToChat(_NetworkChat, "-!- " + msg);
        }
        
        private void _OnContactListFinished(OscarSession sess, DateTime time)
        {
            Session.AddTextToChat(_NetworkChat, "-!- Contact list finished");
            
            _OscarSession.ActivateBuddyList();
        }
        
        //private void _OnMessageReceived(OscarSession sess, UserInfo userInfo, string message, Encoding encoding, MessageFlags msgFlags)
        private void _OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            ChatModel chat = GetChat(e.Message.ScreenName, ChatType.Person);
            if (chat == null) {
                PersonModel person = new PersonModel(e.Message.ScreenName,
                                                     e.Message.ScreenName,
                                                     NetworkID,
                                                     Protocol,
                                                     this);
                chat = new PersonChatModel(person,
                                           e.Message.ScreenName,
                                           e.Message.ScreenName,
                                           this);
                Session.AddChat(chat);
                Session.SyncChat(chat);
            }
            
            MessageModel msg = new MessageModel();
            TextMessagePartModel textMsg;
            
            textMsg = new TextMessagePartModel();
            textMsg.Text = String.Format("<{0}> ", e.Message.ScreenName);
            textMsg.IsHighlight = true;
            msg.MessageParts.Add(textMsg);
            
            textMsg = new TextMessagePartModel();
            textMsg.Text = e.Message.Message;
            msg.MessageParts.Add(textMsg);
            
            Session.AddMessageToChat(chat, msg);
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
