/*
 * $Id: IrcNetworkManager.cs 149 2007-04-11 16:47:52Z meebey $
 * $URL: svn+ssh://svn.qnetp.net/svn/smuxi/smuxi/trunk/src/Engine/IrcNetworkManager.cs $
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
using System.Globalization;
using System.Threading;
using System.Collections;
using jabber.client;
using jabber.connection;
using jabber.protocol.client;
using Meebey.Smuxi.Common;

namespace Meebey.Smuxi.Engine
{
    public class XmppNetworkManager : NetworkManagerBase
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
        
        public override NetworkProtocol NetworkProtocol {
            get {
                return NetworkProtocol.Xmpp;
            }
        }

        public XmppNetworkManager(Session session) : base(session)
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
        
        public void Connect(FrontendManager fm, string host, int port, string username, string password)
        {
            Trace.Call(fm, host, port, username, password);
            
            _FrontendManager = fm;
            Host = host;
            Port = port;
            
            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new NetworkChatModel(NetworkID, "Jabber " + host, this);
            this.Session.AddChat(_NetworkChat);
            
            _JabberClient.Server = host;
            _JabberClient.Port = port;
            _JabberClient.User = username;
            _JabberClient.Password = password;
            _JabberClient.Connect();
            
            /*
            Thread thread = new Thread(new ThreadStart(_Run));
            thread.IsBackground = true;
            thread.Name = "IrcNetworkManager ("+server+":"+port+")";
            thread.Start();
            */
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
        
        public override void Dispose()
        {
            Trace.Call();
            
            // we can't delete directly, it will break the enumerator, let's use a list
            ArrayList removelist = new ArrayList();
            foreach (ChatModel  chat in this.Session.Chats) {
                if (chat.NetworkManager == this) {
                    removelist.Add(chat);
                }
            }
            
            // now we can delete
            foreach (ChatModel  chat in removelist) {
                this.Session.RemoveChat(chat);
            }
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
            }
            return handled;
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
            msgPart.ForegroundColor = IrcTextColor.Blue;
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
                ChatModel chat = this.Session.GetChat(user, ChatType.Person, this);
                if (chat == null) {
                    PersonModel person = new PersonModel(jid, user, 
                                                NetworkID, NetworkProtocol, this);
                    chat = new PersonChatModel(person, jid, user, this);
                    this.Session.AddChat(chat);
                }
                
                MessageModel msg = new MessageModel();
                TextMessagePartModel msgPart;
                
                // TODO: parse possible markup in body
                msgPart = new TextMessagePartModel();
                msgPart.Text = String.Format("<{0}> {1}", xmppMsg.From.User, xmppMsg.Body);
                msg.MessageParts.Add(msgPart);
                
                this.Session.AddMessageToChat(chat, msg);
            }
        }
        
        private void _OnConnect(object sender, StanzaStream stream)
        {
            this.IsConnected = true;
        }
        
        private void _OnDisconnect(object sender)
        {
            this.IsConnected = false;
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
