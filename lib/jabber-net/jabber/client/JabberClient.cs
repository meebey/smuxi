/* --------------------------------------------------------------------------
 * Copyrights
 *
 * Portions created by or assigned to Cursive Systems, Inc. are
 * Copyright (c) 2002-2007 Cursive Systems, Inc.  All Rights Reserved.  Contact
 * information for Cursive Systems, Inc. is available at
 * http://www.cursive.net/.
 *
 * License
 *
 * Jabber-Net can be used under either JOSL or the GPL.
 * See LICENSE.txt for details.
 * --------------------------------------------------------------------------*/
using System;

using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Xml;

using bedrock.util;
using bedrock.net;

using jabber.connection;
using jabber.protocol;
using jabber.protocol.client;
using jabber.protocol.iq;
using jabber.connection.sasl;

namespace jabber.client
{
    /// <summary>
    /// Received a presence packet
    /// </summary>
    public delegate void PresenceHandler(Object sender, Presence pres);
    /// <summary>
    /// Received a message
    /// </summary>
    public delegate void MessageHandler(Object sender, Message msg);
    /// <summary>
    /// Received an IQ
    /// </summary>
    public delegate void IQHandler(Object sender, IQ iq);

    /// <summary>
    /// A component for clients to use to access the Jabber server.
    /// Install this in your Toolbox, drop onto a form, a service,
    /// etc.  Hook into the OnProtocol event.  Call Connect().
    /// </summary>
    [SVN(@"$Id: JabberClient.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class JabberClient : XmppStream
    {
        private static readonly object[][] DEFAULTS = new object[][] {
            new object[] {Options.RESOURCE, "Jabber.Net"},
            new object[] {Options.PRIORITY, 0},
            new object[] {Options.AUTO_LOGIN, true},
            new object[] {Options.AUTO_ROSTER, true},
            new object[] {Options.AUTO_PRESENCE, true},
            new object[] {Options.PROXY_PORT, 1080},
        };

        private void init()
        {
            SetDefaults(DEFAULTS);

            this.OnSASLStart += new jabber.connection.sasl.SASLProcessorHandler(JabberClient_OnSASLStart);
            this.OnSASLEnd += new jabber.protocol.stream.FeaturesHandler(JabberClient_OnSASLEnd);
            this.OnStreamInit += new StreamHandler(JabberClient_OnStreamInit);
        }

        /// <summary>
        /// Required for Windows.Forms Class Composition Designer support
        /// </summary>
        /// <param name="container"></param>
        public JabberClient(System.ComponentModel.IContainer container) :
            base(container)
        {
            init();
        }

        /// <summary>
        /// Required for Windows.Forms Class Composition Designer support
        /// </summary>
        public JabberClient() : base()
        {
            init();
        }

        /*
        /// <summary>
        /// Create a new JabberClient, reusing an existing SocketWatcher.
        /// </summary>
        /// <param name="watcher">SocketWatcher to use.</param>
        public JabberClient(SocketWatcher watcher) : base(watcher)
        {
            init();
        }
        */

        /// <summary>
        /// We received a presence packet.
        /// </summary>
        [Category("Protocol")]
        [Description("We received a presence packet.")]
        public event PresenceHandler OnPresence;

        /// <summary>
        /// We received a message packet.
        /// </summary>
        [Category("Protocol")]
        [Description("We received a message packet.")]
        public event MessageHandler OnMessage;

        /// <summary>
        /// We received an IQ packet.
        /// </summary>
        [Category("Protocol")]
        [Description("We received an IQ packet.")]
        public event IQHandler OnIQ;

        /// <summary>
        /// Authentication failed.  The connection is not
        /// terminated if there is an auth error and there
        /// is at least one event handler for this event.
        /// </summary>
        [Category("Protocol")]
        [Description("Authentication failed.")]
        public event IQHandler OnAuthError;

        /// <summary>
        /// AutoLogin is false, and it's time to log in.
        /// This callback will receive the results of the IQ type=get
        /// in the jabber:iq:auth namespace.  When login is complete,
        /// set IsConnected to true.  If there is a login error, call
        /// FireAuthError().
        /// </summary>
        [Category("Protocol")]
        [Description("AutoLogin is false, and it's time to log in.")]
        public event bedrock.ObjectHandler OnLoginRequired;

        /// <summary>
        /// After calling Register(), the registration succeeded or failed.
        /// </summary>
        [Category("Protocol")]
        [Description("After calling Register(), the registration succeeded or failed.")]
        public event IQHandler OnRegistered;

        /// <summary>
        /// After calling Register, information about the user is required.  Fill in the given IQ
        /// with the requested information.
        ///
        /// WARNING: do not perform GUI actions in this callback, since even if your InvokeControl is set,
        /// this is not guaranteed to be called in the GUI thread.  The IQ modification side-effect is used when
        /// your event handler returns, so if you need to hop over to the GUI thread, pause the thread where
        /// you are called back, then join with it when you are done.
        /// </summary>
        [Category("Protocol")]
        [Description("After calling Register, information about the user is required.")]
        public event IQHandler OnRegisterInfo;


        /// <summary>
        /// The username to connect as.
        /// </summary>
        [Description("The username to connect as.")]
        [Category("Jabber")]
        public string User
        {
            get { return this[Options.USER] as string; }
            set { this[Options.USER] = value; }
        }

        /// <summary>
        /// Priority for this connection.
        /// </summary>
        [Description("Priority for this connection.")]
        [Category("Jabber")]
        [DefaultValue(0)]
        public int Priority
        {
            get { return (int)this[Options.PRIORITY]; }
            set { this[Options.PRIORITY] = value; }
        }

        /// <summary>
        /// The password to use for connecting.
        /// This may be sent across the wire plaintext, if the
        /// server doesn't support digest and PlaintextAuth is true.
        /// </summary>
        [Description("The password to use for connecting.  " +
             "This may be sent across the wire plaintext, " +
             "if the server doesn't support digest, " +
             "and PlaintextAuth is true")]
        [Category("Jabber")]
        public string Password
        {
            get { return this[Options.PASSWORD] as string; }
            set { this[Options.PASSWORD] = value; }
        }

        /// <summary>
        /// Automatically log in on connection.
        /// </summary>
        [Description("Automatically log in on connection.")]
        [DefaultValue(true)]
        [Category("Automation")]
        public bool AutoLogin
        {
            get { return (bool)this[Options.AUTO_LOGIN]; }
            set { this[Options.AUTO_LOGIN] = value; }
        }

        /// <summary>
        /// Automatically retrieve roster on connection.
        /// </summary>
        [Description("Automatically retrieve roster on connection.")]
        [DefaultValue(true)]
        [Category("Automation")]
        public bool AutoRoster
        {
            get { return (bool)this[Options.AUTO_ROSTER]; }
            set { this[Options.AUTO_ROSTER] = value; }
        }

        /// <summary>
        /// Automatically send presence on connection.
        /// </summary>
        [Description("Automatically send presence on connection.")]
        [DefaultValue(true)]
        [Category("Automation")]
        public bool AutoPresence
        {
            get { return (bool)this[Options.AUTO_PRESENCE]; }
            set { this[Options.AUTO_PRESENCE] = value; }
        }

        /// <summary>
        /// The connecting resource.
        /// Used to identify a unique connection.
        /// </summary>
        [Description("The connecting resource.  " +
             "Used to identify a unique connection.")]
        [DefaultValue("Jabber.Net")]
        [Category("Jabber")]
        public string Resource
        {
            get { return this[Options.RESOURCE] as string; }
            set { this[Options.RESOURCE] = value; }
        }

        /// <summary>
        /// The stream namespace for this connection.
        /// </summary>
        [Browsable(false)]
        protected override string NS
        {
            get { return URI.CLIENT; }
        }

        /// <summary>
        /// Are we currently connected?
        /// </summary>
        [Browsable(false)]
        [DefaultValue(false)]
        public override bool IsAuthenticated
        {
            get { return base.IsAuthenticated; }
            set
            {
                base.IsAuthenticated = value;
                if (value)
                {
                    if (AutoRoster)
                        GetRoster();
                    if (AutoPresence)
                        Presence(PresenceType.available,
                            "online", null, Priority);
                }
            }
        }

        /// <summary>
        /// Connect to the server.  This happens asynchronously, and
        /// could take a couple of seconds to get the full handshake
        /// completed.  This will auth, send presence, and request
        /// roster info, if the Auto* properties are set.
        /// </summary>
        public override void Connect()
        {
            this[Options.SERVER_ID] = this[Options.TO];
            base.Connect();
        }

        /// <summary>
        /// Close down the connection, as gracefully as possible.
        /// </summary>
        public override void Close()
        {
            if (IsAuthenticated)
            {
                Presence p = new Presence(Document);
                p.Status = "offline";
                Write(p);
            }
            base.Close();
        }

        /// <summary>
        /// Initiate the auth process.
        /// </summary>
        public void Login()
        {
            Debug.Assert(User != null);
            Debug.Assert(Password != null);
            Debug.Assert(Resource != null);

            AuthIQ aiq = new AuthIQ(Document);
            aiq.Type = IQType.get;
            Auth a = (Auth) aiq.Query;
            a.Username = User;

            lock (StateLock)
            {
                State = GetAuthState.Instance;
            }
            Tracker.BeginIQ(aiq, new IqCB(OnGetAuth), null);
        }

        /// <summary>
        /// Send a presence packet to the server
        /// </summary>
        /// <param name="t">What kind?</param>
        /// <param name="status">How to show us?</param>
        /// <param name="show">away, dnd, etc.</param>
        /// <param name="priority">How to prioritize this connection.
        /// Higher number mean higher priority.  0 minumum.</param>
        public void Presence(PresenceType t,
            string status,
            string show,
            int priority)
        {
            if (IsAuthenticated)
            {
                Presence p = new Presence(Document);
                if (status != null)
                    p.Status = status;
                if (t != PresenceType.available)
                {
                    p.Type = t;
                }
                if (show != null)
                    p.Show = show;
                p.Priority = priority.ToString();
                Write(p);
            }
            else
            {
                throw new InvalidOperationException("Client must be authenticated before sending presence.");
            }
        }

        /// <summary>
        /// Send a message packet to another user
        /// </summary>
        /// <param name="t">What kind?</param>
        /// <param name="to">Who to send it to?</param>
        /// <param name="body">The message.</param>
        public void Message(MessageType t,
            string to,
            string body)
        {
            if (IsAuthenticated)
            {
                Message msg = new Message(Document);
                msg.Type = t;
                msg.To = to;
                msg.Body = body;
                Write(msg);
            }
            else
            {
                throw new InvalidOperationException("Client must be authenticated before sending messages.");
            }
        }

        /// <summary>
        /// Send a message packet to another user
        /// </summary>
        /// <param name="to">Who to send it to?</param>
        /// <param name="body">The message.</param>
        public void Message(
            string to,
            string body)
        {
            Message(MessageType.chat, to, body);
        }

        /// <summary>
        /// Request a new copy of the roster.
        /// </summary>
        public void GetRoster()
        {
            if (IsAuthenticated)
            {
                RosterIQ riq = new RosterIQ(Document);
                riq.Type = IQType.get;
                Write(riq);
            }
            else
            {
                throw new InvalidOperationException("Client must be authenticated before getting roster.");
            }
        }

        /// <summary>
        /// Request a list of agents from the server
        /// </summary>
        public void GetAgents()
        {
            DiscoInfoIQ diq = new DiscoInfoIQ(Document);
            diq.Type = IQType.get;
            diq.To = this.Server;
            Tracker.BeginIQ(diq, new IqCB(GotDiscoInfo), null);
        }

        private void GotDiscoInfo(object sender, IQ iq, object state)
        {
            bool error = false;
            if (iq.Type == IQType.error)
                error = true;
            else
            {
                DiscoInfo info = iq.Query as DiscoInfo;
                if (info == null)
                    error = true;
                else
                {
                    if (!info.HasFeature(URI.DISCO_ITEMS))
                        error = true;  // wow.  weird server.

                    // TODO: stash away features for this node in discomanager?
                }
            }

            if (error)
            {
                // TODO: check the error type that jabberd1.4 or XCP 2.x return
            }
        }


        /// <summary>
        /// Attempt to register a new user.  This will fire OnRegisterInfo to retrieve
        /// information about the new user, and OnRegistered when the registration is complete or failed.
        /// </summary>
        /// <param name="jid">The user to register</param>
        public void Register(JID jid)
        {
            RegisterIQ iq = new RegisterIQ(Document);
            Register reg = (Register)iq.Query;
            iq.Type = IQType.get;
            iq.To = jid.Server;

            reg.Username = jid.User;
            Tracker.BeginIQ(iq, new IqCB(OnGetRegister), jid);
        }

        private void OnGetRegister(object sender, IQ iq, object data)
        {
            if (iq == null)
            {
                FireOnError(new IQTimeoutException((JID) data));
                return;
            }

            if (iq.Type == IQType.error)
            {
                if (OnRegistered != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnRegistered, new object[] {this, iq});
                    else
                        OnRegistered(this, iq);
                }
            }
            else if (iq.Type == IQType.result)
            {
                if (OnRegisterInfo == null)
                    throw new InvalidOperationException("Please set OnRegisterInfo if you are going to use Register()");

                JID jid = (JID) data;
                iq.Type = IQType.set;
                iq.From = null;
                iq.To = jid.Server;
                iq.ID = Element.NextID();
                Register r = iq.Query as Register;
                Debug.Assert(r != null);
                r.Username = jid.User;

                // Note: Don't do a CheckedInvoke, since we need the result back here synchronously.
                // Side effect:  OnRegisterInfo can't do GUI actions.
                OnRegisterInfo(this, iq);

                Tracker.BeginIQ(iq, new IqCB(OnSetRegister), jid);
            }
        }

        private void OnSetRegister(object sender, IQ iq, object data)
        {
            if (OnRegistered == null)
                return;

            if (InvokeRequired)
                CheckedInvoke(OnRegistered, new object[] {this, iq});
            else
                OnRegistered(this, iq);
        }

        private void OnGetAuth(object sender, IQ i, object data)
        {
            if ((i == null) || (i.Type != IQType.result))
            {
                FireAuthError(i);
                return;
            }

            Auth res = i.Query as Auth;
            if (res == null)
            {
                FireOnError(new InvalidOperationException("Invalid IQ result type"));
                return;
            }

            AuthIQ aiq = new AuthIQ(Document);
            aiq.Type = IQType.set;
            Auth a = (Auth) aiq.Query;

            if ((res["sequence"] != null) && (res["token"] != null))
            {
                a.SetZeroK(User, Password, res.Token, res.Sequence);
            }
            else if (res["digest"] != null)
            {
                a.SetDigest(User, Password, StreamID);
            }
            else if (res["password"] != null)
            {
                if (!SSLon && !this.PlaintextAuth)
                {
                    FireOnError(new AuthenticationFailedException("Plaintext authentication forbidden."));
                    return;
                }
                a.SetAuth(User, Password);
            }
            else
            {
                FireOnError(new NotImplementedException("Authentication method not implemented for:\n" + i));
                return;
            }
            if (res["resource"] != null)
                a.Resource = Resource;
            a.Username = User;

            lock (StateLock)
            {
                State = SetAuthState.Instance;
            }
            Tracker.BeginIQ(aiq, new IqCB(OnSetAuth), null);
        }

        private void OnSetAuth(object sender, IQ i, object data)
        {
            if ((i == null) || (i.Type != IQType.result))
                FireAuthError(i);
            else
                IsAuthenticated = true;
        }

        /// <summary>
        /// An element was received.
        /// Look for Presence, Message, and IQ.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tag"></param>
        protected override void OnElement(object sender, System.Xml.XmlElement tag)
        {
            base.OnElement(sender, tag);

            if (OnPresence != null)
            {
                Presence p = tag as Presence;
                if (p != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnPresence, new object[] {this, p});
                    else
                        OnPresence(this, p);
                    return;
                }
            }
            if (OnMessage != null)
            {
                Message m = tag as Message;
                if (m != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnMessage, new object[] {this, m});
                    else
                        OnMessage(this, m);
                    return;
                }
            }
            if (OnIQ != null)
            {
                IQ i = tag as IQ;
                if (i != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnIQ, new object[] {this, i});
                    else
                        OnIQ(this, i);
                    return;
                }
            }
        }

        /// <summary>
        /// An error occurred authenticating.
        /// This is public so that manual authenticators
        /// can fire errors using the same events.
        /// </summary>
        /// <param name="i"></param>
        public void FireAuthError(IQ i)
        {
            if (OnAuthError != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnAuthError, new object[] {this, i});
                else
                    OnAuthError(this, i);
            }
            else
                FireOnError(new ProtocolException(i));
        }

        private void JabberClient_OnSASLStart(Object sender, jabber.connection.sasl.SASLProcessor proc)
        {

            BaseState s = null;
            lock (StateLock)
            {
                s = State;
            }

            // HACK: fire OnSASLStart with state of NonSASLAuthState to initiate old-style auth.
            if (s == NonSASLAuthState.Instance)
            {
                if (AutoLogin)
                    Login();
                else
                {
                    lock (StateLock)
                    {
                        State = ManualLoginState.Instance;
                    }
                    if (OnLoginRequired != null)
                    {
                        if (InvokeRequired)
                            CheckedInvoke(OnLoginRequired, new object[]{this});
                        else
                            OnLoginRequired(this);
                    }
                    else
                    {
                        FireOnError(new InvalidOperationException("If AutoLogin is false, you must supply a OnLoginRequired event handler"));
                        return;
                    }
                }
            }
            else
            {
                // TODO: integrate SASL params into XmppStream params
                proc[SASLProcessor.USERNAME] = User;
                proc[SASLProcessor.PASSWORD] = Password;
                proc[MD5Processor.REALM] = this.Server;
            }
        }

        private void JabberClient_OnSASLEnd(Object sender, jabber.protocol.stream.Features feat)
        {
            lock (StateLock)
            {
                State = BindState.Instance;
            }
            if (feat["bind", URI.BIND] != null)
            {
                IQ iq = new IQ(this.Document);
                iq.To = this.Server;
                iq.Type = IQType.set;

                jabber.protocol.stream.Bind bind = new jabber.protocol.stream.Bind(this.Document);
                if ((Resource != null) && (Resource != ""))
                    bind.Resource = Resource;

                iq.AddChild(bind);
                this.Tracker.BeginIQ(iq, new IqCB(GotResource), feat);
            }
            else if (feat["session", URI.SESSION] != null)
            {
                IQ iq = new IQ(this.Document);
                iq.To = this.Server;
                iq.Type = IQType.set;
                iq.AddChild(new jabber.protocol.stream.Session(this.Document));
                this.Tracker.BeginIQ(iq, new IqCB(GotSession), feat);
            }
            else
                IsAuthenticated = true;
        }

        private void GotResource(object sender, IQ iq, object state)
        {

            jabber.protocol.stream.Features feat =
                state as jabber.protocol.stream.Features;

            if (iq == null)
            {
                FireOnError(new AuthenticationFailedException("Timeout authenticating"));
                return;
            }
            if (iq.Type != IQType.result)
            {
                Error err = iq.Error;
                if (err == null)
                    FireOnError(new AuthenticationFailedException("Unknown error binding resource"));
                else
                    FireOnError(new AuthenticationFailedException("Error binding resource: " + err.OuterXml));
                return;
            }

            if (feat["session", URI.SESSION] != null)
            {
                IQ iqs = new IQ(this.Document);
                iqs.To = this.Server;
                iqs.Type = IQType.set;
                iqs.AddChild(new jabber.protocol.stream.Session(this.Document));
                this.Tracker.BeginIQ(iqs, new IqCB(GotSession), feat);
            }
            else
                IsAuthenticated = true;
        }

        private void GotSession(object sender, IQ iq, object state)
        {
            if ((iq != null) && (iq.Type == IQType.result))
                IsAuthenticated = true;
            else
                FireOnError(new AuthenticationFailedException());
        }

        private void JabberClient_OnStreamInit(Object sender, ElementStream stream)
        {
            stream.AddFactory(new jabber.protocol.client.Factory());
            stream.AddFactory(new jabber.protocol.iq.Factory());
            stream.AddFactory(new jabber.protocol.x.Factory());

        }



    }

    /// <summary>
    /// Getting authorization information
    /// </summary>
    [SVN(@"$Id: JabberClient.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class GetAuthState : jabber.connection.BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly jabber.connection.BaseState Instance = new GetAuthState();
    }

    /// <summary>
    /// Setting authorization information
    /// </summary>
    [SVN(@"$Id: JabberClient.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SetAuthState : jabber.connection.BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly jabber.connection.BaseState Instance = new SetAuthState();
    }

    /// <summary>
    /// Waiting for manual login.
    /// </summary>
    [SVN(@"$Id: JabberClient.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ManualLoginState : jabber.connection.BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly jabber.connection.BaseState Instance = new ManualLoginState();
    }
}
