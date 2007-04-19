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
using System.Security.Cryptography;

namespace jabber.connection
{
    /// <summary>
    /// Http Polling XMPP stream.
    /// </summary>
    [SVN(@"$Id: PollingStanzaStream.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class PollingStanzaStream : StanzaStream, ISocketEventListener
    {
        private AsynchElementStream m_elements = null;
        private BaseSocket m_sock = null;

        /// <summary>
        /// Create a new one.
        /// </summary>
        /// <param name="listener"></param>
        internal PollingStanzaStream(IStanzaEventListener listener)
            : base(listener)
        {
        }

        /// <summary>
        /// Is the socket connected?
        /// </summary>
        public override bool Connected
        {
            get { return Sock.Connected; }
        }

        /// <summary>
        /// If SSL is on, we support Start-TLS.
        /// </summary>
        public override bool SupportsTLS
        {
            get { return false; }
        }

        private XEP25Socket Sock
        {
            get { return m_sock as XEP25Socket; }
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

            XEP25Socket j25s = new XEP25Socket(this);
            //if (m_ProxyHost != null)
            //{
            //    System.Net.WebProxy wp = new System.Net.WebProxy();
            //    wp.Address = new Uri("http://" + m_ProxyHost + ":" + m_ProxyPort);
            //    if (m_ProxyUsername != null)
            //    {
            //        wp.Credentials = new System.Net.NetworkCredential(m_ProxyUsername, m_ProxyPassword);
            //    }
            //    j25s.Proxy = wp;
            //}

            m_sock = j25s;


            string to = (string)m_listener[Options.TO];
            Debug.Assert(to != null);

            string host = (string)m_listener[Options.NETWORK_HOST];
            if ((host == null) || (host == ""))
                host = to;

            bool ssl = (bool)m_listener[Options.SSL];
            string url = (string)m_listener[Options.POLL_URL];

            j25s.URL = ((ssl)?"https://":"http://") + host + ":" + port.ToString() + "/" + url;


            Address addr = new Address(host, port);
            m_sock.Connect(addr, (string)m_listener[Options.SERVER_ID]);
        }

        /// <summary>
        /// Listen for an inbound connection.
        /// </summary>
        public override void Accept()
        {
            m_sock = new AsyncSocket(null, this, (bool)m_listener[Options.SSL], false);
#if NET20
            ((AsyncSocket)m_sock).LocalCertificate = m_listener[Options.LOCAL_CERTIFICATE] as
                System.Security.Cryptography.X509Certificates.X509Certificate;
#endif
            m_sock.Accept(new Address((int)m_listener[Options.PORT]));
            m_sock.RequestAccept();
        }

        /// <summary>
        /// Write a string to the stream.
        /// </summary>
        /// <param name="str">The string to write; this will be transcoded to UTF-8.</param>
        public override void Write(string str)
        {
            //int keep = (int)m_listener[Options.KEEP_ALIVE];
            //m_timer.Change(keep, keep);
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
            //m_sock.StartTLS();
            //XEP25Socket s = Sock;

            //Debug.Assert(s != null);
            //m_listener[Options.REMOTE_CERTIFICATE] = s.RemoteCertificate;
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
            //m_timer.Change(Timeout.Infinite, Timeout.Infinite);
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
                XEP25Socket s = sock as XEP25Socket;

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
            //m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            m_listener.Closed();
        }

        void ISocketEventListener.OnError(BaseSocket sock, Exception ex)
        {
            m_listener[Options.REMOTE_CERTIFICATE] = null;
            m_elements = null;
            //m_timer.Change(Timeout.Infinite, Timeout.Infinite);
            m_listener.Errored(ex);
        }

        bool ISocketEventListener.OnRead(BaseSocket sock, byte[] buf, int offset, int length)
        {
            m_listener.BytesRead(buf, offset, length);
            m_elements.Push(buf, 0, length);
            return true;
        }

        void ISocketEventListener.OnWrite(BaseSocket sock, byte[] buf, int offset, int length)
        {
            m_listener.BytesWritten(buf, offset, length);
        }

        #endregion
    }
}

