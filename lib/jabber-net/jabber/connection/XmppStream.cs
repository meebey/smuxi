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

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Xml;
using bedrock.util;
//using bedrock.net;

using jabber.protocol;
using jabber.protocol.stream;
using jabber.connection.sasl;

#if NET20 || __MonoCS__
using System.Security.Cryptography.X509Certificates;
#elif !NO_SSL
using System.IO;
using Org.Mentalis.Security.Certificates;
#endif

namespace jabber.connection
{
    /// <summary>
    /// A handler for events that happen on an ElementStream.
    /// </summary>
    public delegate void StreamHandler(Object sender, ElementStream stream);

    /// <summary>
    /// Option names.  These must be well-formed XML element names.
    /// </summary>
    [SVN(@"$Id: XmppStream.cs 342 2007-03-06 00:15:44Z hildjj $")]
    public abstract class Options
    {
        /// <summary>
        /// Default namespace.
        /// </summary>
        public const string NAMESPACE = "namespace";

        /// <summary>
        /// The IP or hostname of the machine to connect to.
        /// </summary>
        public const string NETWORK_HOST = "network_host";
        /// <summary>
        /// The identity of the thing we're connecting to.  For components, the component ID.
        /// </summary>
        public const string TO = "to";
        /// <summary>
        /// The identity that we expect on the X.509 certificate on the other side.
        /// </summary>
        public const string SERVER_ID    = "tls.cn";
        /// <summary>
        /// How often to do keep-alive spaces (seconds).
        /// </summary>
        public const string KEEP_ALIVE   = "keep_alive";
        /// <summary>
        /// Port number to connect to or listen on.
        /// </summary>
        public const string PORT         = "port";
        /// <summary>
        /// Do SSL on connection?
        /// </summary>
        public const string SSL          = "ssl";
        /// <summary>
        /// Automatically negotiate TLS.
        /// </summary>
        public const string AUTO_TLS     = "tls.auto";
        /// <summary>
        /// Allow plaintext logins.
        /// </summary>
        public const string PLAINTEXT    = "plaintext";
        /// <summary>
        /// Do SASL connection?
        /// </summary>
        public const string SASL = "sasl";
        /// <summary>
        /// Only allow SASL authentication, but not old-style IQ/auth.
        /// </summary>
        public const string REQUIRE_SASL = "sasl.require";
        /// <summary>
        /// SASL Mechanisms.
        /// </summary>
        public const string SASL_MECHANISMS = "sasl.mechanisms";

        /// <summary>
        /// The user to log in as.
        /// </summary>
        public const string USER     = "user";
        /// <summary>
        /// The password for the user, or secret for the component.
        /// </summary>
        public const string PASSWORD = "password";
        /// <summary>
        /// The resource to bind to.
        /// </summary>
        public const string RESOURCE = "resource";
        /// <summary>
        /// Default priority for presence.
        /// </summary>
        public const string PRIORITY = "priority";

        /// <summary>
        /// Automatically login.
        /// </summary>
        public const string AUTO_LOGIN    = "auto.login";
        /// <summary>
        /// Automatically retrieve the roster.
        /// </summary>
        public const string AUTO_ROSTER   = "auto.roster";
        /// <summary>
        /// Automatically send presence.
        /// </summary>
        public const string AUTO_PRESENCE = "auto.presence";

        /// <summary>
        /// The certificate for our side of the SSL/TLS negotiation.
        /// </summary>
        public const string LOCAL_CERTIFICATE   = "certificate.local";
        /// <summary>
        /// The certificate that the other side sent us.
        /// </summary>
        public const string REMOTE_CERTIFICATE  = "certificate.remote";
        /// <summary>
        /// Enable x509 selection from dialog.
        /// </summary>
        public const string CERTIFICATE_GUI = "certificate.gui";
        /// <summary>
        /// How long to wait before reconnecting.
        /// </summary>
        public const string RECONNECT_TIMEOUT   = "reconnect_timeout";
        /// <summary>
        /// Do we use sockets, HTTP polling, or HTTP binding?
        /// </summary>
        public const string CONNECTION_TYPE     = "connection";
        /// <summary>
        /// URL to poll on, or bind to.
        /// </summary>
        public const string POLL_URL            = "poll.url";
        /// <summary>
        /// Connect to the server or listen for connections.
        /// </summary>
        public const string COMPONENT_DIRECTION = "component.dir";

        /// <summary>
        /// Type of proxy
        /// </summary>
        public const string PROXY_TYPE = "proxy.type";
        /// <summary>
        /// Hostname or IP address of the proxy
        /// </summary>
        public const string PROXY_HOST = "proxy.host";
        /// <summary>
        /// Port number for the proxy
        /// </summary>
        public const string PROXY_PORT = "proxy.port";
        /// <summary>
        /// User name for the proxy
        /// </summary>
        public const string PROXY_USER = "proxy.user";
        /// <summary>
        /// Password for the proxy
        /// </summary>
        public const string PROXY_PW   = "proxy.password";
    }

    /// <summary>
    /// Summary description for SocketElementStream.
    /// </summary>
    [SVN(@"$Id: XmppStream.cs 342 2007-03-06 00:15:44Z hildjj $")]
    abstract public class XmppStream :
        System.ComponentModel.Component,
        IStanzaEventListener
    {
        private static readonly object[][] DEFAULTS = new object[][] {
            new object[] {Options.TO, "jabber.com"},
            new object[] {Options.KEEP_ALIVE, 20000},
            new object[] {Options.PORT, 5222},
            new object[] {Options.RECONNECT_TIMEOUT, 30000},
            new object[] {Options.PROXY_PORT, 1080},
            new object[] {Options.SSL, false},
            new object[] {Options.SASL, true},
            new object[] {Options.REQUIRE_SASL, false},
            new object[] {Options.PLAINTEXT, false},
            new object[] {Options.AUTO_TLS, true},
            new object[] {Options.CERTIFICATE_GUI, true},
            new object[] {Options.PROXY_TYPE, ProxyType.None},
            new object[] {Options.CONNECTION_TYPE, ConnectionType.Socket},
        };

        /// <summary>
        /// Character encoding.  UTF-8.
        /// </summary>
        protected static readonly System.Text.Encoding ENC = System.Text.Encoding.UTF8;

        private StanzaStream m_stanzas = null;
        private IQTracker m_tracker = null;

        private XmlDocument m_doc        = new XmlDocument();
        private BaseState   m_state      = ClosedState.Instance;
        private IDictionary m_properties = new Hashtable();

        private string m_streamID = null;
        private object m_stateLock = new object();
        private ArrayList m_callbacks = new ArrayList();

        private Timer m_reconnectTimer = null;
        private bool m_reconnect = false;
        private bool m_sslOn = false;

        private XmlNamespaceManager m_ns;
        private ISynchronizeInvoke m_invoker = null;

        // XMPP v1 stuff
        private string m_serverVersion = null;
        private SASLProcessor m_saslProc = null;


        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = new System.ComponentModel.Container();

        /// <summary>
        /// Required for Windows.Forms Class Composition Designer support
        /// </summary>
        /// <param name="container"></param>
        public XmppStream(System.ComponentModel.IContainer container) : this()
        {
            container.Add(this);
        }

        /// <summary>
        /// Bulk set defaults.
        /// </summary>
        /// <param name="defaults"></param>
        protected void SetDefaults(object[][] defaults)
        {
            foreach (object[] def in defaults)
            {
                this[(string)def[0]] = def[1];
            }
        }

        /// <summary>
        /// Create a SocketElementStream
        /// </summary>
        public XmppStream()
        {
            m_ns = new XmlNamespaceManager(m_doc.NameTable);
            m_tracker = new IQTracker(this);

            SetDefaults(DEFAULTS);
        }

        /// <summary>
        /// Set or retrieve a connection property.
        /// You have to know the type of the property based on the name.
        /// For example, PORT is an integer.
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        public object this[string prop]
        {
            get
            {
                if (!m_properties.Contains(prop))
                    return null;
                return m_properties[prop];
            }
            set
            {
                m_properties[prop] = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(prop));
                }
            }
        }

        /// <summary>
        /// A property changed on the instance.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /*
        /// <summary>
        /// Create a SocketElementStream out of an accepted socket.
        /// </summary>
        /// <param name="aso"></param>
        public XmppStream(BaseSocket aso)
        {
            m_accept = m_sock = null;
            if (aso is AsyncSocket)
            {
                m_watcher = ((AsyncSocket)aso).SocketWatcher;
            }
            m_ns = new XmlNamespaceManager(m_doc.NameTable);
            m_timer = new Timer(new TimerCallback(DoKeepAlive), null, Timeout.Infinite, Timeout.Infinite);
            InitializeStream();
            m_state = jabber.connection.AcceptingState.Instance;
        }

        /// <summary>
        /// Create a SocketElementStream with an existing SocketWatcher, so that you can do
        /// lots of concurrent connections.
        /// </summary>
        /// <param name="watcher"></param>
        public XmppStream(SocketWatcher watcher)
        {
            m_watcher = watcher;
            m_ns = new XmlNamespaceManager(m_doc.NameTable);
            m_timer = new Timer(new TimerCallback(DoKeepAlive), null, Timeout.Infinite, Timeout.Infinite);
        }
        */

        /// <summary>
        /// Text was written to the server.  Use for debugging only.
        /// Will NOT be complete nodes at a time.
        /// </summary>
        [Category("Debug")]
        public event bedrock.TextHandler OnWriteText;

        /// <summary>
        /// Text was read from the server.  Use for debugging only.
        /// Will NOT be complete nodes at a time.
        /// </summary>
        [Category("Debug")]
        public event bedrock.TextHandler OnReadText;

        /// <summary>
        /// A new stream was initialized.  Add your packet factories to it.
        /// NOTE: you may NOT make calls to the GUI in this callback, unless you
        /// call Invoke yourself.  Make sure you add your packet factories before
        /// calling Invoke however.
        /// </summary>
        [Category("Stream")]
        public event StreamHandler OnStreamInit;

        /// <summary>
        /// Some error occurred when processing.
        /// The connection has been closed.
        /// </summary>
        [Category("Stream")]
        public event bedrock.ExceptionHandler OnError;

        /// <summary>
        /// Get notified for every jabber packet.
        /// This is a union of OnPresence, OnMessage, and OnIQ.
        /// Use this *or* the others, but not both, as a matter of style.
        /// </summary>
        [Category("Stream")]
        public event ProtocolHandler OnProtocol;

        /// <summary>
        /// Get notified of the stream header, as a packet.  Can be called multiple
        /// times for a single session, with XMPP.
        /// </summary>
        [Category("Stream")]
        public event ProtocolHandler OnStreamHeader;

        /// <summary>
        /// Get notified of the start of a SASL handshake.
        /// </summary>
        protected event SASLProcessorHandler OnSASLStart;

        /// <summary>
        /// Get notified of the end of a SASL handshake.
        /// </summary>
        protected event FeaturesHandler OnSASLEnd;

        /// <summary>
        /// Get notified of a SASL error.
        /// </summary>
        protected event SASLProcessorHandler OnSASLFailure;

        /// <summary>
        /// We received a stream:error packet.
        /// </summary>
        [Category("Stream")]
        [Description("We received stream:error packet.")]
        public event ProtocolHandler OnStreamError;

        /// <summary>
        /// The connection is complete, and the user is authenticated.
        /// </summary>
        [Category("Stream")]
        public event bedrock.ObjectHandler OnAuthenticate;

        /// <summary>
        /// The connection is connected, but no stream:stream has been sent, yet.
        /// </summary>
        [Category("Stream")]
        public event StanzaStreamHandler OnConnect;

        /// <summary>
        /// The connection is disconnected
        /// </summary>
        [Category("Stream")]
        public event bedrock.ObjectHandler OnDisconnect;

        /// <summary>
        /// Let's track IQ packets.
        /// </summary>
        [Browsable(false)]
        public IQTracker Tracker
        {
            get { return m_tracker; }
        }

        /// <summary>
        /// The name of the server to connect to.
        /// </summary>
        [Description("The name of the Jabber server.")]
        [DefaultValue("jabber.com")]
        [Category("Jabber")]
        public virtual string Server
        {
            get { return this[Options.TO] as string; }
            set { this[Options.TO] = value; }
        }

        /// <summary>
        /// The address to use on the "to" attribute of the stream:stream.
        /// You can put the network hostname or IP address of the server to connect to.
        /// If none is specified, the Server will be used.
        /// Eventually, when SRV is supported, this will be deprecated.
        /// </summary>
        [Description("")]
        [DefaultValue(null)]
        [Category("Jabber")]
        public string NetworkHost
        {
            get { return this[Options.NETWORK_HOST] as string; }
            set { this[Options.NETWORK_HOST] = value; }
        }

        /// <summary>
        /// The TCP port to connect to.
        /// </summary>
        [Description("The TCP port to connect to.")]
        [DefaultValue(5222)]
        [Category("Jabber")]
        public int Port
        {
            get { return (int)this[Options.PORT]; }
            set { this[Options.PORT] = value; }
        }

        /// <summary>
        /// Allow plaintext authentication?
        /// </summary>
        [Description("Allow plaintext authentication?")]
        [DefaultValue(false)]
        [Category("Jabber")]
        public bool PlaintextAuth
        {
            get { return (bool)this[Options.PLAINTEXT]; }
            set { this[Options.PLAINTEXT] = value; }
        }

        /// <summary>
        /// Is the current connection SSL/TLS protected?
        /// </summary>
        [Description("Is the current connection SSL/TLS protected?")]
        [DefaultValue(false)]
        [Category("Jabber")]
        public bool SSLon
        {
            get { return m_sslOn; }
        }

        /// <summary>
        /// Do SSL3/TLS1 on startup.
        /// </summary>
        [Description("Do SSL3/TLS1 on startup.")]
        [DefaultValue(false)]
        [Category("Jabber")]
        public bool SSL
        {
            get { return (bool)this[Options.SSL]; }
            set
            {
#if NO_SSL
                Debug.Assert(!value, "SSL support not compiled in");
#endif
                this[Options.SSL] = value;
            }
        }

        /// <summary>
        /// Allow Start-TLS on connection, if the server supports it
        /// </summary>
        [Browsable(false)]
        public bool AutoStartTLS
        {
            get { return (bool)this[Options.AUTO_TLS]; }
            set { this[Options.AUTO_TLS] = value; }
        }

#if NET20 || __MonoCS__
        /// <summary>
        /// The certificate to be used for the local side of sockets, with SSL on.
        /// </summary>
        [Browsable(false)]
        public X509Certificate LocalCertificate
        {
            get { return this[Options.LOCAL_CERTIFICATE] as X509Certificate; }
            set { this[Options.LOCAL_CERTIFICATE] = value; }
        }

        /// <summary>
        /// Set the certificate to be used for accept sockets.  To
        /// generate a test .pfx file using openssl, add this to
        /// openssl.conf:
        ///   <blockquote>
        ///   [ serverex ]
        ///   extendedKeyUsage=1.3.6.1.5.5.7.3.1
        ///   </blockquote>
        /// and run the following commands:
        ///   <blockquote>
        ///   openssl req -new -x509 -newkey rsa:1024 -keyout
        ///     privkey.pem -out key.pem -extensions serverex
        ///   openssl pkcs12 -export -in key.pem -inkey privkey.pem
        ///     -name localhost -out localhost.pfx
        ///   </blockquote>
        /// If you leave the certificate null, and you are doing
        /// Accept, the SSL class will try to find a default server
        /// cert on your box.  If you have IIS installed with a cert,
        /// this might just go...
        /// </summary>
        /// <param name="filename">A .pfx or .cer file</param>
        /// <param name="password">The password, if this is a .pfx
        /// file, null if .cer file.</param>
        public void SetCertificateFile(string filename,
                                       string password)
        {
#if __MonoCS__
            byte[] data = null;
            using (FileStream fs = File.OpenRead (filename))
            {
                data = new byte [fs.Length];
                fs.Read (data, 0, data.Length);
                fs.Close ();
            }

            Mono.Security.X509.PKCS12 pfx = new Mono.Security.X509.PKCS12(data, password);
            if (pfx.Certificates.Count > 0)
                this[Options.LOCAL_CERTIFICATE] = new X509Certificate(pfx.Certificates[0].RawData);
#else
            this[Options.LOCAL_CERTIFICATE] = new X509Certificate2(filename, password);
#endif
        }

#elif !NO_SSL
        /// <summary>
        /// The certificate to be used for the local side of sockets,
        /// with SSL on.
        /// </summary>
        [Browsable(false)]
        public Certificate LocalCertificate
        {

            get { return this[Options.LOCAL_CERTIFICATE] as Certificate; }
            set { this[Options.LOCAL_CERTIFICATE] = value; }
        }


        /// <summary>
        /// Set the certificate to be used for accept sockets.  To generate a test .pfx file using openssl,
        /// add this to openssl.conf:
        ///   <blockquote>
        ///   [ serverex ]
        ///   extendedKeyUsage=1.3.6.1.5.5.7.3.1
        ///   </blockquote>
        /// and run the following commands:
        ///   <blockquote>
        ///   openssl req -new -x509 -newkey rsa:1024 -keyout privkey.pem -out key.pem -extensions serverex
        ///   openssl pkcs12 -export -in key.pem -inkey privkey.pem -name localhost -out localhost.pfx
        ///   </blockquote>
        /// If you leave the certificate null, and you are doing
        /// Accept, the SSL class will try to find a default server
        /// cert on your box.  If you have IIS installed with a cert,
        /// this might just go...
        /// </summary>
        /// <param name="filename">A .pfx or .cer file</param>
        /// <param name="password">The password, if this is a .pfx
        /// file, null if .cer file.</param>
        public void SetCertificateFile(string filename, string password)
        {
                        if (!File.Exists(filename))
                        {
                                throw new CertificateException("File does not exist: " + filename);
                        }
                        CertificateStore store;
                        if (password != null)
                        {
                                store = CertificateStore.CreateFromPfxFile(filename, password);
                        }
                        else
                        {
                                store = CertificateStore.CreateFromCerFile(filename);
                        }
                        this[Options.LOCAL_CERTIFICATE] = bedrock.net.CertUtil.FindServerCert(store);
                        if (this[Options.LOCAL_CERTIFICATE] == null)
                                throw new CertificateException("The certificate file does not contain a server authentication certificate.");
                }
#endif

        /// <summary>
        /// Invoke() all callbacks on this control.
        /// </summary>
        [Description("Invoke all callbacks on this control")]
        [DefaultValue(null)]
        [Category("Jabber")]
        public ISynchronizeInvoke InvokeControl
        {
            get
            {
                // If we are running in the designer, let's try to get
                // an invoke control from the environment.  VB
                // programmers can't seem to follow directions.
                if ((this.m_invoker == null) && DesignMode)
                {
                    IDesignerHost host = (IDesignerHost)base.GetService(typeof(IDesignerHost));
                    if (host != null)
                    {
                        object root = host.RootComponent;
                        if ((root != null) && (root is ISynchronizeInvoke))
                        {
                            m_invoker = (ISynchronizeInvoke)root;
                            // TODO: fire some sort of propertyChanged event,
                            // so that old code gets cleaned up correctly.
                        }
                    }
                }
                return m_invoker;
            }
            set { m_invoker = value; }
        }

        /// <summary>
        /// Time, in seconds, between keep-alive spaces.
        /// </summary>
        [Description("Time, in seconds, between keep-alive spaces.")]
        [Category("Jabber")]
        [DefaultValue(20f)]
        public float KeepAlive
        {
            get { return ((int)this[Options.KEEP_ALIVE]) / 1000f; }
            set { this[Options.KEEP_ALIVE] = (int)(value * 1000f); }
        }

        /// <summary>
        /// Seconds before automatically reconnecting if the connection drops.  -1 to disable, 0 for immediate.
        /// </summary>
        [Description("Automatically reconnect a connection.")]
        [DefaultValue(30)]
        [Category("Automation")]
        public float AutoReconnect
        {
            get { return ((int)this[Options.RECONNECT_TIMEOUT]) / 1000f; }
            set { this[Options.RECONNECT_TIMEOUT] = (int)(value * 1000f); }
        }

        /// <summary>
        /// the type of proxy... none, socks5
        /// </summary>
        [Description("The type of proxy... none, socks5, etc.")]
        [DefaultValue(ProxyType.None)]
        [Category("Proxy")]
        public ProxyType Proxy
        {
            get { return (ProxyType)this[Options.PROXY_TYPE]; }
            set { this[Options.PROXY_TYPE] = value; }
        }

        /// <summary>
        /// Connection type.  Socket, HTTP polling, etc.
        /// </summary>
        [Description("The type of connection... Socket, HTTP polling, etc.")]
        [DefaultValue(ConnectionType.Socket)]
        [Category("Proxy")]
        public ConnectionType Connection
        {
            get { return (ConnectionType)this[Options.CONNECTION_TYPE]; }
            set { this[Options.CONNECTION_TYPE] = value; }
        }

        /// <summary>
        /// the host running the proxy
        /// </summary>
        [Description("the host running the proxy")]
        [DefaultValue(null)]
        [Category("Proxy")]
        public string ProxyHost
        {
            get { return this[Options.PROXY_HOST] as string; }
            set { this[Options.PROXY_HOST] = value; }
        }

        /// <summary>
        /// the port to talk to the proxy host on
        /// </summary>
        [Description("the port to talk to the proxy host on")]
        [DefaultValue(1080)]
        [Category("Proxy")]
        public int ProxyPort
        {
            get { return (int)this[Options.PROXY_PORT]; }
            set { this[Options.PROXY_PORT] = value; }
        }

        /// <summary>
        /// the auth username for the socks5 proxy
        /// </summary>
        [Description("the auth username for the socks5 proxy")]
        [DefaultValue(null)]
        [Category("Proxy")]
        public string ProxyUsername
        {
            get { return this[Options.PROXY_USER] as string; }
            set { this[Options.PROXY_USER] = value; }
        }

        /// <summary>
        /// the auth password for the socks5 proxy
        /// </summary>
        [Description("the auth password for the socks5 proxy")]
        [DefaultValue(null)]
        [Category("Proxy")]
        public string ProxyPassword
        {
            get { return this[Options.PROXY_PW] as string; }
            set { this[Options.PROXY_PW] = value; }
        }

        /// <summary>
        /// The id attribute from the stream:stream element sent by the server.
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        public string StreamID
        {
            get { return m_streamID; }
            set { m_streamID = value; }
        }

        /// <summary>
        /// The outbound document.
        /// </summary>
        [Browsable(false)]
        public XmlDocument Document
        {
            get { return m_doc; }
        }

        /// <summary>
        /// The current state of the connection.
        /// Lock on StateLock before accessing.
        /// </summary>
        [Browsable(false)]
        protected virtual BaseState State
        {
            get { return m_state; }
            set { m_state = value; }
        }

        /// <summary>
        /// A lock for the state info.
        /// </summary>
        [Browsable(false)]
        protected object StateLock
        {
            get { return m_stateLock; }
        }

        /// <summary>
        /// Have we authenticated?  Locks on StateLock
        /// </summary>
        [Browsable(false)]
        [DefaultValue(false)]
        public virtual bool IsAuthenticated
        {
            get
            {
                lock (StateLock)
                {
                    return (m_state == RunningState.Instance);
                }
            }
            set
            {
                bool close = false;
                lock (StateLock)
                {
                    if (value)
                    {
                        m_state = RunningState.Instance;
                    }
                    else
                        close = true;
                }
                if (close)
                    Close();
                if (value && (OnAuthenticate != null))
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnAuthenticate, new object[] { this });
                    else
                        OnAuthenticate(this);
                }
            }
        }

        /// <summary>
        /// The namespace for this connection.
        /// </summary>
        [Browsable(false)]
        protected abstract string NS
        {
            get;
        }

        /// <summary>
        /// Is SASL required?  This will default to true in the future.
        /// </summary>
        [Description("Is SASL required?  This will default to true in the future.")]
        [DefaultValue(false)]
        public bool RequiresSASL
        {
            get { return (bool)this[Options.REQUIRE_SASL]; }
            set { this[Options.REQUIRE_SASL] = value; }
        }

        /// <summary>
        ///
        /// </summary>
        [Description("The version string returned in the server's open stream element")]
        [DefaultValue(null)]
        public string ServerVersion
        {
            get { return m_serverVersion; }
        }

        /// <summary>
        /// Write just the start tag of the given element.
        /// Typically only used for &lt;stream:stream&gt;.
        /// </summary>
        /// <param name="elem"></param>
        public void WriteStartTag(jabber.protocol.stream.Stream elem)
        {
            m_stanzas.WriteStartTag(elem);
        }

        /// <summary>
        /// Send the given packet to the server.
        /// </summary>
        /// <param name="elem"></param>
        public void Write(XmlElement elem)
        {
            m_stanzas.Write(elem);
        }

        /// <summary>
        /// Send raw string.
        /// </summary>
        public void Write(string str)
        {
            m_stanzas.Write(str);
        }

        /// <summary>
        /// Start connecting to the server.  This is async.
        /// </summary>
        public virtual void Connect()
        {
            m_stanzas = StanzaStream.Create(this.Connection, this);
            lock (StateLock)
            {
                m_state = ConnectingState.Instance;
                m_reconnect = ((int)this[Options.RECONNECT_TIMEOUT] >= 0);
            }
            m_stanzas.Connect();
        }

        /// <summary>
        /// This is only for components, to listen for connections from the server.
        /// </summary>
        protected virtual void Accept()
        {
            if ((m_stanzas == null) || (!m_stanzas.Acceptable))
                m_stanzas = StanzaStream.Create(this.Connection, this);
            lock (StateLock)
            {
                this.State = AcceptingState.Instance;
                m_reconnect = ((int)this[Options.RECONNECT_TIMEOUT] >= 0);
            }
            m_stanzas.Accept();
        }

        /// <summary>
        /// If autoReconnect is on, start the timer for reconnect now.
        /// </summary>
        private void TryReconnect()
        {
            // close was not requested, or autoreconnect turned on.
            if (m_reconnect)
            {
                if (m_reconnectTimer != null)
                    m_reconnectTimer.Dispose();

                m_reconnectTimer = new System.Threading.Timer(
                        new System.Threading.TimerCallback(Reconnect),
                        null,
                        (int)this[Options.RECONNECT_TIMEOUT],
                        System.Threading.Timeout.Infinite);
            }
        }

        /// <summary>
        /// Close down the connection, as gracefully as possible.
        /// </summary>
        public virtual void Close()
        {
            Close(true);
        }

        /// <summary>
        /// Close down the connection
        /// </summary>
        /// <param name="clean">true for graceful shutdown</param>
        public virtual void Close(bool clean)
        {
            bool doClose = false;
            bool doStream = false;

            lock (StateLock)
            {
                if ((m_state == RunningState.Instance) && (clean))
                {
                    m_reconnect = false;
                    doStream = true;
                }
                if (m_state != ClosedState.Instance)
                {
                    m_state = ClosingState.Instance;
                    doClose = true;
                }
            }

            if ((m_stanzas != null) && m_stanzas.Connected && doClose)
            {
                m_stanzas.Close(doStream);
            }
        }

        /// <summary>
        /// Invokes the given method on the Invoker, and does some exception handling.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="args"></param>
        protected void CheckedInvoke(MulticastDelegate method, object[] args)
        {
            try
            {
                Debug.Assert(m_invoker != null, "Check for this.InvokeControl == null before calling CheckedInvoke");
                Debug.Assert(m_invoker.InvokeRequired, "Check for InvokeRequired before calling CheckedInvoke");

                m_invoker.BeginInvoke(method, args);
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                Debug.WriteLine("Exception passed along by XmppStream: " + e.ToString());
                throw e.InnerException;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception in XmppStream: " + e.ToString());
                throw e;
            }
        }

        /// <summary>
        /// To call callbacks, do we need to call Invoke to get onto the GUI thread?
        /// Only if InvokeControl is set, and we aren't on the GUI thread already.
        /// </summary>
        /// <returns></returns>
        protected bool InvokeRequired
        {
            get
            {
                if (m_invoker == null)
                    return false;
                return m_invoker.InvokeRequired;
            }
        }

        /// <summary>
        /// Document start received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="elem"></param>
        protected virtual void OnDocumentStart(object sender, System.Xml.XmlElement elem)
        {
            bool hack = false;

            if (elem is jabber.protocol.stream.Stream)
            {
                jabber.protocol.stream.Stream str = elem as jabber.protocol.stream.Stream;

                m_streamID = str.ID;
                m_serverVersion = str.Version;

                // See XMPP-core section 4.4.1.  We'll accept 1.x
                if (m_serverVersion.StartsWith("1."))
                {
                    lock (m_stateLock)
                    {
                        if (m_state == SASLState.Instance)
                            // already authed.  last stream restart.
                            m_state = SASLAuthedState.Instance;
                        else
                            m_state = jabber.connection.ServerFeaturesState.Instance;
                    }
                }
                else
                {
                    lock (m_stateLock)
                    {
                        m_state = NonSASLAuthState.Instance;
                    }
                    hack = true;
                }
                if (OnStreamHeader != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnStreamHeader, new object[] { this, elem });
                    else
                        OnStreamHeader(this, elem);
                }
                CheckAll(elem);

                if (hack && (OnSASLStart != null))
                {
                    OnSASLStart(this, null); // Hack.  Old-style auth for jabberclient.
                }
            }
// TODO: Fix broken build
/*
            else if (elem is jabber.protocol.httpbind.Body)
            {
                jabber.protocol.httpbind.Body body = elem as jabber.protocol.httpbind.Body;

                m_streamID = body.AuthID;
            }
*/
        }

        /// <summary>
        /// We received an element.  Invoke the OnProtocol event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tag"></param>
        protected virtual void OnElement(object sender, System.Xml.XmlElement tag)
        {
            //Debug.WriteLine(tag.OuterXml);

            if (tag is jabber.protocol.stream.Error)
            {
                // Stream error.  Race condition!  Two cases:
                // 1) OnClose has already fired, in which case we are in ClosedState, and the reconnect timer is pending.
                // 2) OnClose hasn't fired, in which case we trick it into not starting the reconnect timer.
                lock (m_stateLock)
                {
                    if (m_state != ClosedState.Instance)
                    {
                        m_state = ClosingState.Instance;
                    }
                    else if (m_reconnectTimer != null)
                    {
                        Debug.WriteLine("Disposing of timer");
                        m_reconnectTimer.Dispose();
                    }
                }

                if (OnStreamError != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnStreamError, new object[] { this, tag });
                    else
                        OnStreamError(this, tag);
                }
                return;
            }

            if (m_state == ServerFeaturesState.Instance)
            {
                Features f = tag as Features;
                if (f == null)
                {
                    FireOnError(new InvalidOperationException("Expecting stream:features from a version='1.0' server"));
                    return;
                }

#if !NO_SSL || NET20 || __MonoCS__
                // don't do starttls if we're already on an SSL socket.
                // bad server setup, but no skin off our teeth, we're already
                // SSL'd.  Also, start-tls won't work when polling.
                if ((bool)this[Options.AUTO_TLS] &&
                    (f.StartTLS != null) &&
                    (!m_sslOn) &&
                    m_stanzas.SupportsTLS)
                {
                    // start-tls
                    lock (m_stateLock)
                    {
                        m_state = StartTLSState.Instance;
                    }
                    this.Write(new StartTLS(m_doc));
                    return;
                }
#endif

                // not authenticated yet.  Note: we'll get a stream:features
                // after the last sasl restart, so we shouldn't try to iq:auth
                // at that point.
                if (!IsAuthenticated)
                {
                    Mechanisms ms = f.Mechanisms;
                    m_saslProc = null;

                    MechanismType types = MechanismType.NONE;
                    try
                    {
                        types = (MechanismType)this[Options.SASL_MECHANISMS];
                        types &= ms.Types;
                    }
                    catch
                    {
                        if (ms != null)
                            types = ms.Types;
                    }



                    if ((types != MechanismType.NONE) && ((bool)this[Options.SASL]))
                    {
                        lock (m_stateLock)
                        {
                            m_state = SASLState.Instance;
                        }
                        m_saslProc = SASLProcessor.createProcessor(types, m_sslOn || (bool)this[Options.PLAINTEXT]);
                        if (m_saslProc == null)
                        {

                            FireOnError(new NotImplementedException("No implemented mechanisms in: " + types.ToString()));
                            return;
                        }
                        if (OnSASLStart != null)
                            OnSASLStart(this, m_saslProc);

                        try
                        {
                            Step s = m_saslProc.step(null, this.Document);
                            if (s != null)
                                this.Write(s);
                        }
                        catch (Exception e)
                        {
                            FireOnError(new SASLException(e.Message));
                            return;
                        }
                    }

                    if (m_saslProc == null)
                    { // no SASL mechanisms.  Try iq:auth.
                        if ((bool)this[Options.REQUIRE_SASL])
                        {
                            FireOnError(new SASLException("No SASL mechanisms available"));
                            return;
                        }
                        lock (m_stateLock)
                        {
                            m_state = NonSASLAuthState.Instance;
                        }
                        if (OnSASLStart != null)
                            OnSASLStart(this, null); // HACK: old-style auth for jabberclient.
                    }
                }
            }
            else if (m_state == SASLState.Instance)
            {
                if (tag is Success)
                {
                    // restart the stream again
                    SendNewStreamHeader();
                }
                else if (tag is SASLFailure)
                {
                    m_saslProc = null;
                    // TODO: Add an OnSASLAuthFailure
                    SASLFailure sf = tag as SASLFailure;
                    // TODO: I18N
                    FireOnError(new SASLException("SASL failure: " + sf.InnerXml));
                    return;
                }
                else if (tag is Step)
                {
                    try
                    {
                        Step s = m_saslProc.step(tag as Step, this.Document);
                        if (s != null)
                            Write(s);
                    }
                    catch (Exception e)
                    {
                        FireOnError(new SASLException(e.Message));
                        return;
                    }
                }
                else
                {
                    m_saslProc = null;
                    FireOnError(new SASLException("Invalid SASL protocol"));
                    return;
                }
            }
#if !NO_SSL || NET20 || __MonoCS__
            else if (m_state == StartTLSState.Instance)
            {
                switch (tag.Name)
                {
                case "proceed":
                    if (!StartTLS())
                        return;
                    SendNewStreamHeader();
                    break;
                case "failure":
                    FireOnError(new AuthenticationFailedException());
                    return;
                }
            }
#endif
            else if (m_state == SASLAuthedState.Instance)
            {
                Features f = tag as Features;
                if (f == null)
                {
                    FireOnError(new InvalidOperationException("Expecting stream:features from a version='1.0' server"));
                    return;
                }
                if (OnSASLEnd != null)
                    OnSASLEnd(this, f);
                m_saslProc = null;
            }
            else
            {
                if (OnProtocol != null)
                {
                    if (InvokeRequired)
                        CheckedInvoke(OnProtocol, new object[] { this, tag });
                    else
                        OnProtocol(this, tag);
                }
            }
            CheckAll(tag);
        }

        /// <summary>
        /// Begin the TLS handshake, either client- or server- side.
        /// </summary>
        protected bool StartTLS()
        {
            try
            {
                m_stanzas.StartTLS();
            }
            catch (Exception e)
            {
                m_reconnect = false;
                FireOnError(e);
                return false;
            }
            m_sslOn = true;
            return true;
        }

        /// <summary>
        /// The SASLClient is reporting an exception
        /// </summary>
        /// <param name="e"></param>
        public void OnSASLException(ApplicationException e)
        {
            // lets throw the exception
            FireOnError(e);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        public void OnSASLException(string message)
        {
            // lets throw it!
            FireOnError(new ApplicationException(message));
        }

        /// <summary>
        /// Get ready for a new stream:stream by starting a new XML document.  Needed after start-tls or compression, for example.
        /// </summary>
        protected void InitializeStream()
        {
            try
            {
                m_stanzas.InitializeStream();
            }
            catch (Exception e)
            {
                FireOnError(e);
            }
        }

        /// <summary>
        /// Send a stream:stream
        /// </summary>
        protected void SendNewStreamHeader()
        {
            jabber.protocol.stream.Stream str = new jabber.protocol.stream.Stream(m_doc, NS);
            str.To = new JID((string)this[Options.TO]);
            str.Version = "1.0";
            m_stanzas.WriteStartTag(str);
            InitializeStream();
        }

        /// <summary>
        /// Fire the OnError event.
        /// </summary>
        /// <param name="e"></param>
        protected void FireOnError(Exception e)
        {
            m_reconnect = false;

            // ignore spurious IO errors on shutdown.
            if (((State == ClosingState.Instance) || (State == ClosedState.Instance)) &&
                ((e is System.IO.IOException) || (e.InnerException is System.IO.IOException)))
                return;

            if (OnError != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnError, new object[] { this, e });
                else
                    OnError(this, e);
            }

            if ((State != ClosingState.Instance) && (State == ClosedState.Instance))
                Close(false);
        }

        private void Reconnect(object state)
        {
            // prevent double-connects
            if (this.State == ClosedState.Instance)
                Connect();
        }

        /// <summary>
        /// Register a callback, so that if a packet arrives that matches the given xpath expression,
        /// the callback fires.  Use <see cref="AddNamespace"/> to add namespace prefixes.
        /// </summary>
        /// <example>jc.AddCallback("self::iq[@type='result']/roster:query", new ProtocolHandler(GotRoster));</example>
        /// <param name="xpath">The xpath expression to search for</param>
        /// <param name="cb">The callback to call when the xpath matches</param>
        /// <returns>A guid that can be used to unregister the callback</returns>
        public Guid AddCallback(string xpath, ProtocolHandler cb)
        {
            CallbackData cbd = new CallbackData(xpath, cb);
            m_callbacks.Add(cbd);
            return cbd.Guid;
        }

        /// <summary>
        /// Remove a callback added with <see cref="AddCallback"/>.
        /// </summary>
        /// <param name="guid"></param>
        public void RemoveCallback(Guid guid)
        {
            int count = 0;
            foreach (CallbackData cbd in m_callbacks)
            {
                if (cbd.Guid == guid)
                {
                    m_callbacks.RemoveAt(count);
                    return;
                }
                count++;
            }
            throw new ArgumentException("Unknown Guid", "guid");
        }

        /// <summary>
        /// Add a namespace prefix, for use with callback xpath expressions added with <see cref="AddCallback"/>.
        /// </summary>
        /// <param name="prefix">The prefix to use</param>
        /// <param name="uri">The URI associated with the prefix</param>
        public void AddNamespace(string prefix, string uri)
        {
            m_ns.AddNamespace(prefix, uri);
        }

        private void CheckAll(XmlElement elem)
        {
            foreach (CallbackData cbd in m_callbacks)
            {
                cbd.Check(this, elem);
            }
        }

        private class CallbackData
        {
            private Guid m_guid = Guid.NewGuid();
            private ProtocolHandler m_cb;
            private string m_xpath;

            public CallbackData(string xpath, ProtocolHandler cb)
            {
                Debug.Assert(cb != null);
                m_cb = cb;
                m_xpath = xpath;
            }

            public Guid Guid
            {
                get { return m_guid; }
            }

            public string XPath
            {
                get { return m_xpath; }
            }

            public void Check(XmppStream sender, XmlElement elem)
            {
                try
                {
                    XmlNode n = elem.SelectSingleNode(m_xpath, sender.m_ns);
                    if (n != null)
                    {
                        if (sender.InvokeRequired)
                            sender.CheckedInvoke(m_cb, new object[] { sender, elem });
                        else
                            m_cb(sender, elem);
                    }
                }
                catch (Exception e)
                {
                    sender.FireOnError(e);
                }
            }
        }

        #region IStanzaEventListener Members

        void IStanzaEventListener.Connected()
        {
            lock (m_stateLock)
            {
                this.State = ConnectedState.Instance;
                if ((bool)this[Options.SSL])
                    m_sslOn = true;
            }

            if (OnConnect != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnConnect, new Object[] { this, m_stanzas });
                else
                    OnConnect(this, m_stanzas);
            }

            SendNewStreamHeader();
        }

        void IStanzaEventListener.Accepted()
        {
            lock (StateLock)
            {
                Debug.Assert(this.State == AcceptingState.Instance, this.State.GetType().ToString());

                this.State = ConnectedState.Instance;
            }

            if (OnConnect != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnConnect, new object[] { this, m_stanzas });
                else
                {
                    // Um.  This cast might not be right, but I don't want to break backward compatibility
                    // if I don't have to by changing the delegate interface.
                    OnConnect(this, m_stanzas);
                }
            }
        }

        void IStanzaEventListener.BytesRead(byte[] buf, int offset, int count)
        {
            if (OnReadText != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnReadText, new object[] { this, ENC.GetString(buf, offset, count) });
                else
                    OnReadText(this, ENC.GetString(buf, offset, count));
            }
        }

        void IStanzaEventListener.BytesWritten(byte[] buf, int offset, int count)
        {
            if (OnWriteText != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnWriteText, new object[] { this, ENC.GetString(buf, offset, count) });
                else
                    OnWriteText(this, ENC.GetString(buf, offset, count));
            }
        }

        void IStanzaEventListener.StreamInit(ElementStream stream)
        {
            if (OnStreamInit != null)
            {
                // Race condition.  Make sure not to make GUI calls in OnStreamInit
                /*
                if (InvokeRequired)
                    CheckedInvoke(OnStreamInit, new object[] { this, stream });
                else
              */
                    OnStreamInit(this, stream);
            }
        }

        void IStanzaEventListener.Errored(Exception ex)
        {
            m_reconnect = false;

            lock (m_stateLock)
            {
                m_state = ClosedState.Instance;
                if ((m_stanzas != null) && (!m_stanzas.Acceptable))
                    m_stanzas = null;
            }

            if (OnError != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnError, new object[] { this, ex });
                else
                    OnError(this, ex);
            }

            // TODO: Figure out what the "good" errors are, and try to
            // reconnect.  There are too many "bad" errors to just let this fly.
            //TryReconnect();
        }

        void IStanzaEventListener.Closed()
        {
            lock (StateLock)
            {
                m_state = ClosedState.Instance;
                if ((m_stanzas != null) && (!m_stanzas.Acceptable))
                    m_stanzas = null;
                m_sslOn = false;
            }

            if (OnDisconnect != null)
            {
                if (InvokeRequired)
                    CheckedInvoke(OnDisconnect, new object[] { this });
                else
                    OnDisconnect(this);
            }

            TryReconnect();
        }

        void IStanzaEventListener.DocumentStarted(XmlElement elem)
        {
            // The OnDocumentStart logic stays outside the listener, so that it can be
            // more easily overriden by subclasses.
            OnDocumentStart(m_stanzas, elem);
        }

        void IStanzaEventListener.DocumentEnded()
        {
            lock (StateLock)
            {
                m_state = ClosingState.Instance;
                // TODO: Validate this, with current parser:

                // No need to close stream any more.  AElfred does this for us, even though
                // the docs say it doesn't.

                //if (m_sock != null)
                //m_sock.Close();
            }
        }

        void IStanzaEventListener.StanzaReceived(XmlElement elem)
        {
            // The OnElement logic stays outside the listener, so that it can be
            // more easily overriden by subclasses.
                OnElement(m_stanzas, elem);
        }

        #endregion
    }
}
