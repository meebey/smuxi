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
using System.Diagnostics;
using System.Threading;
using System.Xml;

using bedrock.net;
using bedrock.util;
using jabber.protocol;

namespace jabber.connection
{
    /// <summary>
    /// The types of proxies we support.  This is only for socket connections.
    /// </summary>
    [SVN(@"$Id: SocketStanzaStream.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public enum ProxyType
    {
        /// <summary>
        /// no proxy
        /// </summary>
        None,
        /// <summary>
        /// socks4 as in http://archive.socks.permeo.com/protocol/socks4.protocol
        /// </summary>
        Socks4,
        /// <summary>
        /// socks5 as in http://archive.socks.permeo.com/rfc/rfc1928.txt
        /// </summary>
        Socks5,
        /// <summary>
        /// HTTP CONNECT
        /// </summary>
        CONNECT,
    }


    /// <summary>
    /// "Standard" XMPP socket, connecting outward.
    /// </summary>
    [SVN(@"$Id: SocketStanzaStream.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class SocketStanzaStream : StanzaStream, ISocketEventListener
    {
        private AsynchElementStream m_elements = null;
        private BaseSocket          m_sock     = null;
        private BaseSocket          m_accept   = null;
        private Timer               m_timer    = null;

        /// <summary>
        /// Create a new one.
        /// </summary>
        /// <param name="listener"></param>
        internal SocketStanzaStream(IStanzaEventListener listener) : base(listener)
        {
            m_timer = new Timer(new TimerCallback(DoKeepAlive), null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Is the socket connected?
        /// </summary>
        public override bool Connected
        {
            get { return ASock.Connected;  }
        }

        /// <summary>
        /// If SSL is on, we support Start-TLS.
        /// </summary>
        public override bool SupportsTLS
        {
            get
            {
#if NO_SSL
                return false;
#else
                return true;
#endif
            }
        }

        private AsyncSocket ASock
        {
            get
            {
                if (m_sock is ProxySocket)
                    return ((ProxySocket)m_sock).Socket as AsyncSocket;
                else
                    return m_sock as AsyncSocket;
            }
        }

        /// <summary>
        /// Set up the element stream.  This is the place to add factories.
        /// </summary>
        public override void InitializeStream()
        {
            bool first = (m_elements == null);
            m_elements = new AsynchElementStream();
            m_elements.OnDocumentStart += new ProtocolHandler(m_elements_OnDocumentStart);
            m_elements.OnDocumentEnd += new bedrock.ObjectHandler(m_elements_OnDocumentEnd);
            m_elements.OnElement += new ProtocolHandler(m_elements_OnElement);
            m_elements.OnError += new bedrock.ExceptionHandler(m_elements_OnError);

            m_listener.StreamInit(m_elements);

            Debug.Assert(this.Connected);
            if (first)
                m_sock.RequestRead();

        }

        /// <summary>
        /// Connect the socket, outbound.
        /// </summary>
        public override void Connect()
        {
            int port = (int)m_listener[Options.PORT];
            Debug.Assert(port > 0);
            //m_sslOn = m_ssl;

            ProxySocket proxy = null;
            ProxyType pt = (ProxyType)m_listener[Options.PROXY_TYPE];
            switch (pt)
            {
            case ProxyType.Socks4:
                proxy = new Socks4Proxy(this);
                break;

            case ProxyType.Socks5:
                proxy = new Socks5Proxy(this);
                break;

            case ProxyType.CONNECT:
                proxy = new ShttpProxy(this);
                break;

                /*
            case ProxyType.HTTP_Polling:
                XEP25Socket j25s = new XEP25Socket(this);
                if (m_ProxyHost != null)
                {
                    System.Net.WebProxy wp = new System.Net.WebProxy();
                    wp.Address = new Uri("http://" + m_ProxyHost + ":" + m_ProxyPort);
                    if (m_ProxyUsername != null)
                    {
                        wp.Credentials = new System.Net.NetworkCredential(m_ProxyUsername, m_ProxyPassword);
                    }
                    j25s.Proxy = wp;
                }
                j25s.URL = m_server;
                m_sock = j25s;
                break;
                */
            case ProxyType.None:
                m_sock = new AsyncSocket(null, this, (bool)m_listener[Options.SSL], false);

#if NET20
                ((AsyncSocket)m_sock).LocalCertificate = m_listener[Options.LOCAL_CERTIFICATE] as
                    System.Security.Cryptography.X509Certificates.X509Certificate;

                ((AsyncSocket)m_sock).CertificateGui = (bool)m_listener[Options.CERTIFICATE_GUI];
#endif
                break;

            default:
                throw new ArgumentException("no handler for proxy type: " + pt, "ProxyType");
            }

            if (proxy != null)
            {
                proxy.Socket = new AsyncSocket(null, proxy, (bool)m_listener[Options.SSL], false);
#if NET20
                ((AsyncSocket)proxy.Socket).LocalCertificate = m_listener[Options.LOCAL_CERTIFICATE] as
                    System.Security.Cryptography.X509Certificates.X509Certificate;
#endif

                proxy.Host = m_listener[Options.PROXY_HOST] as string;
                proxy.Port = (int)m_listener[Options.PROXY_PORT];
                proxy.Username = m_listener[Options.PROXY_USER] as string;
                proxy.Password = m_listener[Options.PROXY_PW] as string;
                m_sock = proxy;
            }

            // TODO: SRV lookup
            string to = (string)m_listener[Options.TO];
            Debug.Assert(to != null);

            string host = (string)m_listener[Options.NETWORK_HOST];
            if ((host == null) || (host == ""))
                host = to;

            Address addr = new Address(host, port);
            m_sock.Connect(addr, (string)m_listener[Options.SERVER_ID]);
        }

        /// <summary>
        /// Listen for an inbound connection.
        /// </summary>
        public override void Accept()
        {
            if (m_accept == null)
            {
                m_accept = new AsyncSocket(null, this, (bool)m_listener[Options.SSL], false);
#if NET20
                ((AsyncSocket)m_accept).LocalCertificate = m_listener[Options.LOCAL_CERTIFICATE] as
                    System.Security.Cryptography.X509Certificates.X509Certificate;
#endif
                Address addr = new Address((string)m_listener[Options.NETWORK_HOST],
                    (int)m_listener[Options.PORT]);

                m_accept.Accept(addr);
            }
            m_accept.RequestAccept();
        }

        /// <summary>
        /// Can Accept() be called npw?
        /// </summary>
        public override bool Acceptable
        {
            get
            {
                return (m_accept != null);
            }
        }

        /// <summary>
        /// Write the given string to the socket after UTF-8 encoding.
        /// </summary>
        /// <param name="str"></param>
        public override void Write(string str)
        {
            int keep = (int)m_listener[Options.KEEP_ALIVE];
            if (keep > 0)
                m_timer.Change(keep, keep);
            m_sock.Write(ENC.GetBytes(str));

        }

        /// <summary>
        /// Write a stream:stream
        /// </summary>
        /// <param name="stream"></param>
        public override void WriteStartTag(jabber.protocol.stream.Stream stream)
        {
            Write(stream.StartTag());
        }

        /// <summary>
        /// Write a full stanza
        /// </summary>
        /// <param name="elem"></param>
        public override void Write(XmlElement elem)
        {
            Write(elem.OuterXml);
        }

        /// <summary>
        /// Close the socket
        /// </summary>
        public override void Close(bool clean)
        {
            // Note: socket should still be connected, excepts for races.  Revist.
            if (clean)
                Write("</stream:stream>");
            m_sock.Close();
        }

        private void DoKeepAlive(object state)
        {
            if ((m_sock != null) && this.Connected)
                m_sock.Write(new byte[] { 32 });
        }

#if !NO_SSL
        /// <summary>
        /// Negotiate Start-TLS with the other endpoint.
        /// </summary>
        public override void StartTLS()
        {
            m_sock.StartTLS();
            AsyncSocket s = ASock;

            Debug.Assert(s != null);
            m_listener[Options.REMOTE_CERTIFICATE] = s.RemoteCertificate;
        }
#endif

        #region ElementStream handlers
        private void m_elements_OnDocumentStart(object sender, XmlElement rp)
        {
            m_listener.DocumentStarted(rp);
        }

        private void m_elements_OnDocumentEnd(object sender)
        {
            m_listener.DocumentEnded();
        }

        private void m_elements_OnElement(object sender, XmlElement rp)
        {
            m_listener.StanzaReceived(rp);
        }

        private void m_elements_OnError(object sender, Exception ex)
        {
            // XML parse error.
            m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            m_listener.Errored(ex);
        }
        #endregion

        #region ISocketEventListener Members

        void ISocketEventListener.OnInit(BaseSocket newSock)
        {
        }

        ISocketEventListener ISocketEventListener.GetListener(BaseSocket newSock)
        {
            return this;
        }

        bool ISocketEventListener.OnAccept(BaseSocket newsocket)
        {
            m_sock = newsocket;
            InitializeStream();
            m_listener.Accepted();

            // Don't accept any more connections until this one closes
            // yes, it will look like we're still listening until the old sock is free'd by GC.
            // don't want OnClose() to fire, though, so we can't close the previous sock.
            return false;
        }

        void ISocketEventListener.OnConnect(BaseSocket sock)
        {
#if !NO_SSL
            if ((bool)m_listener[Options.SSL])
            {
                AsyncSocket s = sock as AsyncSocket;
                m_listener[Options.REMOTE_CERTIFICATE] = s.RemoteCertificate;
            }
#endif
            m_listener.Connected();
        }

        void ISocketEventListener.OnClose(BaseSocket sock)
        {
            //System.Windows.Forms.Application.DoEvents();
            //System.Threading.Thread.Sleep(1000);
            m_listener[Options.REMOTE_CERTIFICATE] = null;
            m_elements = null;
            m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            m_listener.Closed();
        }

        void ISocketEventListener.OnError(BaseSocket sock, Exception ex)
        {
            m_listener[Options.REMOTE_CERTIFICATE] = null;
            m_elements = null;
            m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            m_listener.Errored(ex);
        }

        bool ISocketEventListener.OnRead(BaseSocket sock, byte[] buf, int offset, int length)
        {
            int tim = (int)m_listener[Options.KEEP_ALIVE];
            if (tim > 0)
                m_timer.Change(tim, tim);

            m_listener.BytesRead(buf, offset, length);
            try
            {
                m_elements.Push(buf, 0, length);
            }
            catch (Exception e)
            {
                ((ISocketEventListener)this).OnError(sock, e);
                sock.Close();
                return false;
            }
            return true;
        }

        void ISocketEventListener.OnWrite(BaseSocket sock, byte[] buf, int offset, int length)
        {
            int tim = (int)m_listener[Options.KEEP_ALIVE];
            if (tim > 0)
                m_timer.Change(tim, tim);

            m_listener.BytesWritten(buf, offset, length);
        }

        #endregion
    }
}
