/*
 * $Id: IrcProtocolManager.cs 149 2007-04-11 16:47:52Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcProtocolManager.cs $
 * $Rev: 149 $
 * $Author: meebey $
 * $Date: 2007-04-11 18:47:52 +0200 (Wed, 11 Apr 2007) $
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

using jabber.client;
using jabber.connection;
using jabber.protocol.client;

using Smuxi.Common;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "jabber")]
    public class JabberProtocolManager : XmppProtocolManager
    {
        public JabberProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "XMPP", Description = "Extensible Messaging and Presence Protocol", Alias = "xmpp")]
    public class XmppProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private JabberClient    _JabberClient;
        private FrontendManager _FrontendManager;
        private ChatModel       _NetworkChat;
        
        public override string NetworkID {
            get {
                return "XMPP";
            }
        }
        
        public override string Protocol {
            get {
                return "XMPP";
            }
        }
        
        public override ChatModel Chat {
            get {
                return _NetworkChat;
            }
        }

        public XmppProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
            
            _JabberClient = new JabberClient();
            _JabberClient.Resource = Engine.VersionString;
            _JabberClient.AutoLogin = true;
            _JabberClient.AutoPresence = true;
            _JabberClient.OnMessage += new MessageHandler(_OnMessage);
            _JabberClient.OnConnect += new StanzaStreamHandler(_OnConnect);
            _JabberClient.OnDisconnect += new bedrock.ObjectHandler(_OnDisconnect);

            _JabberClient.OnReadText += new bedrock.TextHandler(_OnReadText);
            _JabberClient.OnWriteText += new bedrock.TextHandler(_OnWriteText);
        }
        
        public override void Connect(FrontendManager fm, string host, int port, string username, string password)
        {
            Connect(fm, host, port, username, password, "smuxi");
        }
        
        public void Connect(FrontendManager fm, string host, int port, string username, string password, string resource)
        {
            Trace.Call(fm, host, port, username, password);
            
            _FrontendManager = fm;
            Host = host;
            Port = port;
            
            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new ProtocolChatModel(NetworkID, "Jabber " + host, this);
            Session.AddChat(_NetworkChat);
            Session.SyncChat(_NetworkChat);
            
            _JabberClient.Server = host;
            _JabberClient.Port = port;
            _JabberClient.User = username;
            _JabberClient.Password = password;
            _JabberClient.Resource = resource;
            _JabberClient.Connect();
        }
        
        public override void Reconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }
        
        public override void Disconnect(FrontendManager fm)
        {
            Trace.Call(fm);
        }
        
        public override string ToString()
        {
            string result = "Jabber ";
            if (_JabberClient != null) {
                result += _JabberClient.Server + ":" + _JabberClient.Port;
            }
            
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }
        
        public override IList<GroupChatModel> FindGroupChats(GroupChatModel filter)
        {
            Trace.Call(filter);
            
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
        
        public override bool Command(CommandModel command)
        {
            bool handled = false;
            if (IsConnected) {
                if (command.IsCommand) {
                } else {
                    _Say(command, command.Data);
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

        public void CommandHelp(CommandModel cd)
        {
            MessageModel fmsg = new MessageModel();
            TextMessagePartModel fmsgti;

            fmsgti = new TextMessagePartModel();
            fmsgti.Text = _("[XmppProtocolManager Commands]");
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);
            
            Session.AddMessageToChat(cd.FrontendManager.CurrentChat, fmsg);
            
            string[] help = {
            "help",
            "connect xmpp/jabber server port username passwort [resource]",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }
        
        public void CommandConnect(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            
            string server;
            if (cd.DataArray.Length >= 3) {
                server = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            int port;
            if (cd.DataArray.Length >= 4) {
                try {
                    port = Int32.Parse(cd.DataArray[3]);
                } catch (FormatException) {
                    fm.AddTextToCurrentChat("-!- " + String.Format(
                                                        _("Invalid port: {0}"),
                                                        cd.DataArray[3]));
                    return;
                }
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            string username;                
            if (cd.DataArray.Length >= 5) {
                username = cd.DataArray[4];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            string password;
            if (cd.DataArray.Length >= 6) {
                password = cd.DataArray[5];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            string resource;
            if (cd.DataArray.Length >= 7) {
                resource = cd.DataArray[6];
            } else {
                resource = "smuxi";
            }
            
            Connect(fm, server, port, username, password, resource);
        }
        
        private void _Say(CommandModel command, string text)
        {
            if (!command.Chat.IsEnabled) {
                return;
            }
            
            string target = command.Chat.ID;
            
            _JabberClient.Message(target, text);
            
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "<";
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = _JabberClient.User;
            //msgPart.ForegroundColor = IrcTextColor.Blue;
            msgPart.ForegroundColor = new TextColor(0x0000FF);
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "> ";
            msg.MessageParts.Add(msgPart);
                
            msgPart = new TextMessagePartModel();
            msgPart.Text = text;
            msg.MessageParts.Add(msgPart);
            
            this.Session.AddMessageToChat(command.Chat, msg);
        }
        
        private void _OnReadText(object sender, string text)
        {
            this.Session.AddTextToChat(_NetworkChat, "-!- RECV: " + text);
        }
        
        private void _OnWriteText(object sender, string text)
        {
            this.Session.AddTextToChat(_NetworkChat, "-!- SENT: " + text);
        }
        
        private void _OnMessage(object sender, Message xmppMsg)
        {
            // TODO: implement group chat
            if (xmppMsg.Type == MessageType.chat) {
                string jid = xmppMsg.From.ToString();
                string user = xmppMsg.From.User;
                ChatModel chat = Session.GetChat(user, ChatType.Person, this);
                if (chat == null) {
                    PersonModel person = new PersonModel(jid, user, 
                                                NetworkID, Protocol, this);
                    chat = new PersonChatModel(person, jid, user, this);
                    Session.AddChat(chat);
                }
                
                MessageModel msg = new MessageModel();
                TextMessagePartModel msgPart;
                
                // TODO: parse possible markup in body
                msgPart = new TextMessagePartModel();
                msgPart.Text = String.Format("<{0}> {1}", xmppMsg.From.User, xmppMsg.Body);
                msg.MessageParts.Add(msgPart);
                
                Session.AddMessageToChat(chat, msg);
            }
        }
        
        private void _OnConnect(object sender, StanzaStream stream)
        {
            IsConnected = true;
        }
        
        private void _OnDisconnect(object sender)
        {
            IsConnected = false;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
