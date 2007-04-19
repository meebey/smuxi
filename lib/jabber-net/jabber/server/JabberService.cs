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
using System.Diagnostics;
using System.Threading;
using System.Xml;

using bedrock.net;
using bedrock.util;

using jabber.connection;
using jabber.protocol;
using jabber.protocol.accept;
using jabber.protocol.stream;

namespace jabber.server
{
    /// <summary>
    /// Type of connection to the server, with respect to jabberd.
    /// This list will grow over time to include
    /// queued connections, direct (in-proc) connections, etc.
    /// </summary>
    [SVN(@"$Id: JabberService.cs 342 2007-03-06 00:15:44Z hildjj $")]
    public enum ComponentType
    {
        /// <summary>
        /// Jabberd will accept the connetion; the component will
        /// initiate the connection.  </summary>
        Accept,
        /// <summary>
        /// Jabberd will connect to the component; jabberd will
        /// initiate the connection.  </summary>
        Connect
    }

    /// <summary>
    /// Received a route element
    /// </summary>
    public delegate void RouteHandler(object sender, jabber.protocol.accept.Route route);

    /// <summary>
    /// Received an XDB element.
    /// </summary>
    public delegate void XdbHandler(object sender, jabber.protocol.accept.Xdb xdb);

    /// <summary>
    /// Received a Log element.
    /// </summary>
    public delegate void LogHandler(object sender, jabber.protocol.accept.Log log);

    /// <summary>
    /// Summary description for ServerComponent.
    /// </summary>
    [SVN(@"$Id: JabberService.cs 342 2007-03-06 00:15:44Z hildjj $")]
    public class JabberService : jabber.connection.XmppStream
    {
        private static readonly object[][] DEFAULTS = new object[][] {
            new object[] {Options.COMPONENT_DIRECTION, ComponentType.Accept},
            new object[] {Options.PORT, 7400},
        };

        private void init()
        {
            SetDefaults(DEFAULTS);
            this.OnStreamInit += new jabber.connection.StreamHandler(JabberService_OnStreamInit);
            this.OnSASLStart += new jabber.connection.sasl.SASLProcessorHandler(JabberService_OnSASLStart);
        }

        /// <summary>
        /// Create a a connect component.
        /// </summary>
        public JabberService() : base()
        {
            init();
        }

        /// <summary>
        /// Create an accept component.  (Component connects to server)
        /// </summary>
        /// <param name="host">Jabberd host to connect to</param>
        /// <param name="port">Jabberd port to connect to</param>
        /// <param name="name">Component name</param>
        /// <param name="secret">Component secret</param>
        public JabberService(string host,
            int port,
            string name,
            string secret) : base()
        {
            init();
            this.Server = name;
            this.NetworkHost = host;
            this.Port = port;

            this[Options.PASSWORD] = secret;
            this[Options.COMPONENT_DIRECTION] = ComponentType.Accept;
        }

        /// <summary>
        /// Create a connect component. (Server connects to component)
        /// </summary>
        /// <param name="port">Port jabberd will connect to</param>
        /// <param name="name">Component name</param>
        /// <param name="secret">Component secret</param>
        public JabberService(int port, string name, string secret) : base()
        {
            init();
            this.Server = name;
            this.Port = port;

            this[Options.PASSWORD] = secret;
            this[Options.COMPONENT_DIRECTION] = ComponentType.Connect;
        }

        /// <summary>
        /// We received a route packet.
        /// </summary>
        [Category("Protocol")]
        [Description("We received a route packet.")]
        public event RouteHandler OnRoute;

        /// <summary>
        /// We received an XDB packet.
        /// </summary>
        [Category("Protocol")]
        [Description("We received an XDB packet.")]
        public event XdbHandler OnXdb;

        /// <summary>
        /// We received a Log packet.
        /// </summary>
        [Category("Protocol")]
        [Description("We received a Log packet.")]
        public event LogHandler OnLog;

        /// <summary>
        /// The service name.  Needs to be in the id attribute in the
        /// jabber.xml file.  </summary>
        [Description("The service name.  The id attribute in the jabber.xml file.")]
        [DefaultValue(null)]
        [Category("Component")]
        public string ComponentID
        {
            get { return Server; }
            set { Server = value; }
        }

        /// <summary>
        /// The name of the server to connect to.
        /// </summary>
        [Description("The name of the Jabber server.")]
        [DefaultValue("jabber.com")]
        [Category("Jabber")]
        [Browsable(false)]
        public override string Server
        {
            get { return base.Server; }
            set { base.Server = value; }
        }

        /// <summary>
        /// Component secret.
        /// </summary>
        [Description("Component secret.")]
        [DefaultValue(null)]
        [Category("Component")]
        public string Secret
        {
            get { return (string)this[Options.PASSWORD]; }
            set { this[Options.PASSWORD] = value; }
        }

        /// <summary>
        /// Is this an outgoing connection (base_accept), or an incoming
        /// connection (base_connect).
        /// </summary>
        [Description("Is this an outgoing connection (base_accept), or an incoming connection (base_connect).")]
        [DefaultValue(ComponentType.Accept)]
        [Category("Component")]
        public ComponentType Type
        {
            get { return (ComponentType)this[Options.COMPONENT_DIRECTION]; }
            set
            {
                if ((ComponentType)this[Options.COMPONENT_DIRECTION] != value)
                {
                    this[Options.COMPONENT_DIRECTION] = value;
                    if ((ComponentType)this[Options.COMPONENT_DIRECTION] == ComponentType.Connect)
                    {
                        this.AutoReconnect = 0;
                    }
                }
            }
        }



        /// <summary>
        /// The stream namespace for this connection.
        /// </summary>
        [Browsable(false)]
        protected override string NS
        {
            get
            {
                return (this.Type == ComponentType.Accept) ? URI.ACCEPT : URI.CONNECT;
            }
        }

        /// <summary>
        /// Connect to the jabberd, or wait for it to connect to us.
        /// Either way, this call returns immediately.
        /// </summary>
        /// <param name="address">The address to connect to.</param>
        public void Connect(bedrock.net.Address address)
        {
            this.Server = address.Hostname;
            this.Port = address.Port;

            Connect();
        }

        /// <summary>
        /// Connect to the jabberd, or wait for it to connect to us.
        /// Either way, this call returns immediately.
        /// </summary>
        public override void Connect()
        {
            this[Options.SERVER_ID] = this[Options.NETWORK_HOST];
            if (this.Type == ComponentType.Accept)
                base.Connect();
            else
            {
                Accept();
            }
        }

        /// <summary>
        /// Got the stream:stream.  Start the handshake.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tag"></param>
        protected override void OnDocumentStart(object sender, System.Xml.XmlElement tag)
        {
            base.OnDocumentStart(sender, tag);
            if (this.Type == ComponentType.Connect)
            {
                lock (StateLock)
                {
                    State = HandshakingState.Instance;
                }

                jabber.protocol.stream.Stream str = new jabber.protocol.stream.Stream(this.Document, NS);
                str.To = this.Server;
                this.StreamID = str.ID;
                if (ServerVersion.StartsWith("1."))
                    str.Version = "1.0";


                WriteStartTag(str);

                if (ServerVersion.StartsWith("1."))
                {
                    Features f = new Features(this.Document);
                    if (AutoStartTLS && !SSLon && (this[Options.LOCAL_CERTIFICATE] != null))
                        f.StartTLS = new StartTLS(this.Document);
                    Write(f);
                }
            }
        }

        private void Handshake(System.Xml.XmlElement tag)
        {
            Handshake hs = tag as Handshake;

            if (hs == null)
            {
                FireOnError(new System.Security.SecurityException("Bad protocol.  Needs handshake, got: " + tag.OuterXml));
                return;
            }

            if (this.Type == ComponentType.Accept)
                IsAuthenticated = true;
            else
            {
                string test = hs.Digest;
                string good = Element.ShaHash(StreamID, this.Secret);
                if (test == good)
                {
                    IsAuthenticated = true;
                    Write(new Handshake(this.Document));
                }
                else
                {
                    Write(new Error(this.Document));
                    FireOnError(new System.Security.SecurityException("Bad handshake."));
                }
            }
        }

        /// <summary>
        /// Received an element.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tag"></param>
        protected override void OnElement(object sender, System.Xml.XmlElement tag)
        {
            lock (StateLock)
            {
                StartTLS start = tag as StartTLS;
                if (start != null)
                {
                    State = ConnectedState.Instance;
                    InitializeStream();
                    this.Write(new Proceed(this.Document));
                    this.StartTLS();
                    return;
                }
                if (State == HandshakingState.Instance)
                {
                    // sets IsConnected
                    Handshake(tag);
                    return;
                }
            }

            base.OnElement(sender, tag);

            if (OnRoute != null)
            {
                Route route = tag as Route;
                if (route != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnRoute, new object[] {this, route});
                    else
                        OnRoute(this, route);
                }
            }
            // TODO: add XdbTracker stuff
            if (OnXdb != null)
            {
                Xdb xdb = tag as Xdb;
                if (xdb != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnXdb, new object[] {this, xdb});
                    else
                        OnXdb(this, xdb);
                }
            }
            if (OnLog != null)
            {
                Log log = tag as Log;
                if (log != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnLog, new object[] {this, log});
                    else
                        OnLog(this, log);
                }
            }
        }

        private void JabberService_OnSASLStart(object sender, jabber.connection.sasl.SASLProcessor proc)
        {
            jabber.connection.BaseState s = null;
            lock (StateLock)
            {
                s = State;
            }

            if (s == jabber.connection.NonSASLAuthState.Instance)
            {
                lock (StateLock)
                {
                    State = HandshakingState.Instance;
                }

                if (this.Type == ComponentType.Accept)
                {
                    Handshake hand = new Handshake(this.Document);
                    hand.SetAuth(this.Secret, StreamID);
                    Write(hand);
                }
            }
        }

        private void JabberService_OnStreamInit(Object sender, ElementStream stream)
        {
            stream.AddFactory(new jabber.protocol.accept.Factory());
        }
    }

    /// <summary>
    /// Waiting for handshake result.
    /// </summary>
    [SVN(@"$Id: JabberService.cs 342 2007-03-06 00:15:44Z hildjj $")]
    public class HandshakingState : jabber.connection.BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly jabber.connection.BaseState Instance = new HandshakingState();
    }

    /// <summary>
    /// Waiting for socket connection.
    /// </summary>
    [SVN(@"$Id: JabberService.cs 342 2007-03-06 00:15:44Z hildjj $")]
    public class AcceptingState : jabber.connection.BaseState
    {
        /// <summary>
        /// The instance that is always used.
        /// </summary>
        public static readonly jabber.connection.BaseState Instance = new AcceptingState();
    }
}
