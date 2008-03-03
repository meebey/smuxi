using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Collections;
/*
using XihSolutions.DotMSN;
using XihSolutions.DotMSN.Core;
*/
using MSNPSharp;
using MSNPSharp.Core;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "MSNP", Description = "MSN Messenger Protocol", Alias = "msn")]
    public class MsnProtocolManager : MsnpProtocolManager
    {
        public MsnProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "MSNP", Description = "MSN Messenger Protocol", Alias = "msnp")]
    public class MsnpProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private Messenger       _MsnClient;
        private FrontendManager _FrontendManager;
        private ChatModel       _NetworkChat;
        private Conversation    _Conversation;
        private string          _UsersAddress;
        
        public override string NetworkID {
            get {
                return "MSN";
            }
        }
        
        public override string Protocol {
            get {
                return "MSNP";
            }
        }
        
        public override ChatModel Chat {
            get {
                return _NetworkChat;
            }
        }

        public MsnpProtocolManager(Session session) : base(session)
        {
            Trace.Call(session);
            
            _MsnClient = new Messenger();
            _MsnClient.Credentials.ClientID = "msmsgs@msnmsgr.com";
            _MsnClient.Credentials.ClientCode = "Q1P7W2E4J9R8U3S5";   
            
            _MsnClient.NameserverProcessor.ConnectionEstablished += new EventHandler(NameserverProcessor_ConnectionEstablished);
            _MsnClient.NameserverProcessor.ConnectionClosed += new EventHandler(NameserverProcessor_ConnectionClosed);
            _MsnClient.Nameserver.AuthenticationError += new HandlerExceptionEventHandler(_NameServerAuthenticationError);
            _MsnClient.Nameserver.SignedIn += new EventHandler(Nameserver_SignedIn);
            _MsnClient.Nameserver.SignedOff += new SignedOffEventHandler(Nameserver_SignedOff);
//            _MsnClient.NameserverProcessor.ConnectingException += new ProcessorExceptionEventHandler(NameserverProcessor_ConnectingException);            
//            _MsnClient.Nameserver.ExceptionOccurred += new HandlerExceptionEventHandler(Nameserver_ExceptionOccurred);                    
//            _MsnClient.Nameserver.AuthenticationError += new HandlerExceptionEventHandler(Nameserver_AuthenticationError);
//            _MsnClient.Nameserver.ServerErrorReceived += new ErrorReceivedEventHandler(Nameserver_ServerErrorReceived);
            _MsnClient.ConversationCreated += new ConversationCreatedEventHandler(MsnClient_ConversationCreated);
            
//            _Conversation.Switchboard.SessionClosed += new .SBChangedEventHandler(SessionClosed);
//            _Conversation.Switchboard.ContactJoined += new .ContactChangedEventHandler(ContactJoined);
//            _Conversation.Switchboard.ContactLeft   += new .ContactChangedEventHandler(ContactLeft);            
        }
        
        public override void Connect(FrontendManager fm, string host, int port, string username, string password)
        {
            Trace.Call(fm, host, port, username, password);
            
            _UsersAddress = username;
            _FrontendManager = fm;
            Host = host;
            Port = port;
            
            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new NetworkChatModel(NetworkID, "MSN Messenger", this);
            Session.AddChat(_NetworkChat);
            Session.SyncChat(_NetworkChat);
            
            _MsnClient.Credentials.Account = username;
            _MsnClient.Credentials.Password = password;
            _MsnClient.Connect();
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
            string result = "MSN ";
            
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            } else {
                result += " (" + _("connected") + ")";
            }
            return result;
        }
        
        public override void Dispose()
        {
            Trace.Call();
            
            // we can't delete directly, it will break the enumerator, let's use a list
            ArrayList removelist = new ArrayList();
            foreach (ChatModel  chat in Session.Chats) {
                if (chat.ProtocolManager == this) {
                    removelist.Add(chat);
                }
            }
            
            // now we can delete
            foreach (ChatModel  chat in removelist) {
                Session.RemoveChat(chat);
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
            fmsgti.Text = _("[MsnProtocolManager Commands]");
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);
            
                Session.AddMessageToChat(cd.FrontendManager.CurrentChat, fmsg);
            
            string[] help = {
            "help",
            "connect msn username password",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }
        
        public void CommandConnect(CommandModel cd)
        {
            FrontendManager fm = cd.FrontendManager;
            
            string user;
            if (cd.DataArray.Length >= 1) {
                user = cd.DataArray[2];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            string pass;
            if (cd.DataArray.Length >= 2) {
                pass = cd.DataArray[3];
            } else {
                NotEnoughParameters(cd);
                return;
            }
            
            Connect(fm, null, 0, user, pass);
        }
        
        private void _Say(CommandModel command, string text)
        {
            if (!command.Chat.IsEnabled) {
                return;
            }
            
//            string target = command.Chat.ID;
//            
//            _JabberClient.Message(target, text);
            
            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "<";
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = _UsersAddress;
            //msgPart.ForegroundColor = IrcTextColor.Blue;
            msgPart.ForegroundColor = new TextColor(0x0000FF);
            msg.MessageParts.Add(msgPart);
            
            msgPart = new TextMessagePartModel();
            msgPart.Text = "> ";
            msg.MessageParts.Add(msgPart);
                
            msgPart = new TextMessagePartModel();
            msgPart.Text = text;
            msg.MessageParts.Add(msgPart);
            
                Session.AddMessageToChat(command.Chat, msg);
        }
        
//        private void _OnReadText(xobject sender, string text)
//        {
//                Session.AddTextToChat(_NetworkChat, "-!- RECV: " + text);
//        }
        
//        private void _OnWriteText(xobject sender, string text)
//        {
//                Session.AddTextToChat(_NetworkChat, "-!- SENT: " + text);
//        }

            
         private void MsnClient_ConversationCreated(object sender, ConversationCreatedEventArgs e) 
         {
             Trace.Call(sender, e);
            
            
             
            // Session.AddTextToChat(_NetworkChat, "Conversation bla");
            //e.Conversation.Switchboard.Co
            //_MsnClient.Nameserver.RequestScreenName();
            
            // NetworkChatModel nchat = new NetworkChatModel("MSN", "MSN", this);
             //Session.AddChat(nchat);
             
            e.Conversation.Switchboard.TextMessageReceived +=  delegate(object sender2, TextMessageEventArgs e2) {
                PersonModel person = new PersonModel(e2.Sender.Name,
                                                     e2.Sender.Name,
                                                     NetworkID, Protocol,
                                                     this);
                PersonChatModel personChat = new PersonChatModel(person,
                                                                 e2.Sender.Name,
                                                                 e2.Sender.Name,
                                                                 this);
                Session.AddChat(personChat);
                Session.AddTextToChat(personChat, e2.Message.Text);
             };
         }

        private void TextMessageReceived(object sender, TextMessageEventArgs e) 
        {
            Trace.Call(sender, e);
                
            string user = e.Sender.Name;
            string status = e.Sender.Status.ToString();
            string message = e.Message.Text;

            ChatModel chat = Session.GetChat(user, ChatType.Person, this);
            if (chat == null) {
               PersonModel person = new PersonModel(user, user, NetworkID, Protocol, this);
                   Session.AddChat(chat);
            }

            MessageModel msg = new MessageModel();
            TextMessagePartModel msgPart;

            msgPart = new TextMessagePartModel();
            msgPart.Text = String.Format("{0} (Status: {1}) says:\n{2}", user, status, message);
            msg.MessageParts.Add(msgPart);

            Session.AddMessageToChat(chat, msg);
        }
        
//        private void _OnDisconnect(xobject sender)
//        {
//            IsConnected = false;
//        }
        
        private void NameserverProcessor_ConnectionEstablished(object sender, EventArgs e)
        {
            IsConnected = true;
            Session.AddTextToChat(_NetworkChat, "-!- Connection to MSN Server established.");
        }        
        
        private void NameserverProcessor_ConnectionClosed(object sender, EventArgs e)
        {
            IsConnected = false;
            Session.AddTextToChat(_NetworkChat, "-!- Connection to MSN Server closed.");
        }
        
        private void Nameserver_SignedIn(object sender, EventArgs e)
        {
            Session.AddTextToChat(_NetworkChat, "-!- Signed into MSN.");
            _Conversation = _MsnClient.CreateConversation();
            _Conversation.Switchboard.TextMessageReceived += new TextMessageReceivedEventHandler(TextMessageReceived);    
            _MsnClient.Owner.Status = PresenceStatus.Online;
            _MsnClient.Owner.Name = "Test";
        }
        
        private void Nameserver_SignedOff(object sender, EventArgs e)
        {
            Session.AddTextToChat(_NetworkChat, "-!- Signed off in MSN.");
        }
        
        private void _NameServerAuthenticationError(object sender, ExceptionEventArgs e)
        {
            Session.AddTextToChat(_NetworkChat, "-!- Authentication error: " + e.Exception.Message);
            _MsnClient.Disconnect();
        }
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
