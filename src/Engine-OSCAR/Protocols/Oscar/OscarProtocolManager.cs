using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using System.Collections;
using csammisrun.OscarLib;
using OscarSession = csammisrun.OscarLib.Session;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [ProtocolManagerInfo(Name = "OSCAR", Description = "Open System for CommunicAtion in Realtime Protocol", Alias = "icq")]
    public class IcqProtocolManager : OscarProtocolManager
    {
        public IcqProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "OSCAR", Description = "Open System for CommunicAtion in Realtime Protocol", Alias = "aim")]
    public class AimProtocolManager : OscarProtocolManager
    {
        public AimProtocolManager(Session session) : base(session)
        {
        }
    }
    
    [ProtocolManagerInfo(Name = "OSCAR", Description = "Open System for CommunicAtion in Realtime Protocol", Alias = "oscar")]
    public class OscarProtocolManager : ProtocolManagerBase
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private FrontendManager _FrontendManager;
        private ChatModel       _NetworkChat;
        private OscarSession    _OscarSession;
        
        public override string NetworkID {
            get {
                return "OSCAR";
            }
        }
        
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
        
        public override void Connect(FrontendManager fm, string host, int port, string username, string password)
        {
            Trace.Call(fm, host, port, username, password);
            
            _FrontendManager = fm;
            Host = host;
            Port = port;
            
            _OscarSession = new OscarSession(username, password);
            _OscarSession.ClientCapabilities = Capabilities.Chat | Capabilities.OscarLib;
            _OscarSession.LoginCompleted += new LoginCompletedHandler(_OnLoginCompleted);
            _OscarSession.ErrorMessage += new ErrorMessageHandler(_OnErrorMessage);
            _OscarSession.WarningMessage += new WarningMessageHandler(_OnWarningMessage);
            //_OscarSession.StatusUpdate += new InformationMessageHandler(_OnStatusUpdate);
            _OscarSession.LoginFailed += new LoginFailedHandler(_OnLoginFailed);

            _OscarSession.Logon("login.oscar.aol.com", 5190);
            
            // TODO: use config for single network chat or once per network manager
            _NetworkChat = new NetworkChatModel(NetworkID, "OSCAR", this);
            Session.AddChat(_NetworkChat);
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
            string result = "OSCAR ";
            /*
            if (_JabberClient != null) {
                result += _JabberClient.Server + ":" + _JabberClient.Port;
            }
            */
            
            if (!IsConnected) {
                result += " (" + _("not connected") + ")";
            }
            return result;
        }
        
        public override bool Command(CommandModel command)
        {
            bool handled = false;
            if (IsConnected) {
                if (command.IsCommand) {
                } else {
                    //_Say(command, command.Data);
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
                            //CommandConnect(command);
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
            fmsgti.Text = _("[OscarProtocolManager Commands]");
            fmsgti.Bold = true;
            fmsg.MessageParts.Add(fmsgti);
            
            this.Session.AddMessageToChat(cd.FrontendManager.CurrentChat, fmsg);
            
            string[] help = {
            "help",
            "connect aim/icq/oscar server port username passwort",
            };
            
            foreach (string line in help) { 
                cd.FrontendManager.AddTextToCurrentChat("-!- " + line);
            }
        }
        
        private void _OnLoginCompleted(OscarSession sess)
        {
            string msg = _("Login successful");
            Session.AddTextToChat(_NetworkChat, "-!- " + msg);
        }

        private void _OnLoginFailed(OscarSession sess, LoginErrorCode errorCode)
        {
            string msg = String.Format(_("Login failed: {0}"), errorCode);
            Session.AddTextToChat(_NetworkChat, "-!- " + msg);
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
        
        private static string _(string msg)
        {
            return Mono.Unix.Catalog.GetString(msg);
        }
    }
}
