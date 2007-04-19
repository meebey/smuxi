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
using System.Text;
using bedrock.util;

namespace bedrock.net
{
    /// <summary>
    /// Proxy object for sockets.
    /// </summary>
    [SVN(@"$Id: ProxySocket.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class ProxySocket : BaseSocket, ISocketEventListener
    {
        private BaseSocket     m_sock = null;
        private string         m_host = null;
        private int            m_port = 0;
        private string         m_username = null;
        private string         m_password = null;
        private Address        m_remote_addr = null;
        private bool           m_ssl = false;

        /// <summary>
        /// Wrap an existing socket event listener with a proxy.  Make SURE to set Socket after this.
        /// </summary>
        /// <param name="chain">Event listener to pass events through to.</param>
        public ProxySocket(ISocketEventListener chain) : base(chain)
        {
        }

        /// <summary>
        /// The address that the proxy should connect to.
        /// </summary>
        public Address RemoteAddress
        {
            get { return m_remote_addr; }
            set { m_remote_addr = value; }
        }

        /// <summary>
        /// The lower level socket
        /// </summary>
        public BaseSocket Socket
        {
            get { return m_sock; }
            set { m_sock = value; }
        }

        /// <summary>
        /// the host running the proxy
        /// </summary>
        public string Host
        {
            get { return m_host; }
            set { m_host = value; }
        }

        /// <summary>
        /// the port to talk to the proxy host on
        /// </summary>
        public int Port
        {
            get { return m_port; }
            set { m_port = value; }
        }

        /// <summary>
        /// Do SSL **after** connected through the proxy.
        /// </summary>
        public bool SSL
        {
            get { return m_ssl; }
            set { m_ssl = value; }
        }

        /// <summary>
        /// the auth username for the proxy
        /// </summary>
        public string Username
        {
            get { return m_username; }
            set { m_username = value; }
        }

        /// <summary>
        /// the auth password for the proxy
        /// </summary>
        public string Password
        {
            get { return m_password; }
            set { m_password = value; }
        }

        /// <summary>
        /// Prepare to start accepting inbound requests.  Call RequestAccept() to start the async process.
        /// </summary>
        /// <param name="addr">Address to listen on</param>
        /// <param name="backlog">The Maximum length of the queue of pending connections</param>
        public override void Accept(bedrock.net.Address addr, int backlog)
        {
            m_sock.Accept(addr, backlog);
        }

        /// <summary>
        /// Close the socket.  This is NOT async.  .Net doesn't have async closes.
        /// But, it can be *called* async, particularly from GotData.
        /// Attempts to do a shutdown() first.
        /// </summary>
        public override void Close()
        {
            m_sock.Close();
        }

        /// <summary>
        /// Saves the address passed in, and really connects to m_host:m_port.
        /// </summary>
        /// <param name="addr"></param>
        public override void Connect(bedrock.net.Address addr)
        {
            m_remote_addr = addr; // save this till we are ready for it...
            Debug.Assert(m_host != null);
            Debug.Assert(m_port != 0);
            // connect to the proxy.
            Address proxy_addr = new Address(m_host, m_port);
            m_sock.Connect(proxy_addr, m_hostid);
            // we'll end up in OnConnected below.
        }

#if !NO_SSL
        /// <summary>
        /// Start TLS processing on an open socket.
        /// </summary>
        public override void StartTLS()
        {
            m_sock.StartTLS();
        }
#endif

        /// <summary>
        /// Start the flow of async accepts.  Flow will continue while
        /// Listener.OnAccept() returns true.  Otherwise, call RequestAccept() again
        /// to continue.
        /// </summary>
        public override void RequestAccept()
        {
            m_sock.RequestAccept();
        }

        /// <summary>
        /// Start an async read from the socket.  Listener.OnRead() is eventually called
        /// when data arrives.
        /// </summary>
        public override void RequestRead()
        {
            m_sock.RequestRead();
        }

        /// <summary>
        /// Async write to the socket.  Listener.OnWrite will be called eventually
        /// when the data has been written.  A trimmed copy is made of the data, internally.
        /// </summary>
        /// <param name="buf">Buffer to output</param>
        /// <param name="offset">Offset into buffer</param>
        /// <param name="len">Number of bytes to output</param>
        public override void Write(byte[] buf, int offset, int len)
        {
            m_sock.Write(buf, offset, len);
        }

        #region Implementation of ISocketEventListener

        /// <summary>
        ///
        /// </summary>
        /// <param name="newSock"></param>
        public virtual void OnInit(BaseSocket newSock)
        {
            m_listener.OnInit(newSock);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="newSock"></param>
        /// <returns></returns>
        public virtual ISocketEventListener GetListener(BaseSocket newSock)
        {
            return m_listener.GetListener(newSock);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="newsocket"></param>
        /// <returns></returns>
        public virtual bool OnAccept(BaseSocket newsocket)
        {
            return m_listener.OnAccept(newsocket);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sock"></param>
        public virtual void OnConnect(BaseSocket sock)
        {
            if (m_ssl)
            {
#if !NO_SSL
                m_sock.StartTLS();
#else
                throw new NotImplementedException("SSL not compiled in");
#endif
            }
            m_listener.OnConnect(sock);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sock"></param>
        public virtual void OnClose(BaseSocket sock)
        {
            m_listener.OnClose(sock);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="ex"></param>
        public virtual void OnError(BaseSocket sock, System.Exception ex)
        {
            m_listener.OnError(sock, ex);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public virtual bool OnRead(BaseSocket sock, byte[] buf, int offset, int length)
        {
            return m_listener.OnRead(sock, buf, offset, length);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public virtual void OnWrite(BaseSocket sock, byte[] buf, int offset, int length)
        {
            m_listener.OnWrite(sock, buf, offset, length);
        }

        #endregion

        /// <summary>
        /// String representation of the proxy socket.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Proxy connection to: " + RemoteAddress.ToString();
        }

    }
}
