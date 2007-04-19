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
using System.Diagnostics;
using System.IO;

using bedrock.util;
using bedrock.collections;

#if NET20 || __MonoCS__
using System.Security.Cryptography.X509Certificates;
#elif !NO_SSL
using Org.Mentalis.Security.Certificates;
#endif

namespace bedrock.net
{
    /// <summary>
    /// A collection of sockets.  This makes a lot more sense in the poll() version (Unix/C) since
    /// you need to have a place to collect all of the sockets and call poll().  Here, it's just
    /// convenience functions.
    /// </summary>
    [SVN(@"$Id: SocketWatcher.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class SocketWatcher : IDisposable
    {
        private enum State
        {
            Running,
            Shutdown,
            Stopped
        };

        private ISet        m_pending = new Set(SetImplementation.SkipList);
        private ISet        m_socks   = new Set(SetImplementation.SkipList);
        private object      m_lock = new object();
        private int         m_maxSocks;
        private bool        m_synch = false;

#if NET20 || __MonoCS__
        private X509Certificate m_cert = null;
        private bool m_requireClientCert = false;
#elif !NO_SSL
        private Certificate m_cert = null;
#endif

        /// <summary>
        /// Create a new instance, which will manage an unlimited number of sockets.
        /// </summary>
        public SocketWatcher()
        {
            m_maxSocks = -1;
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="maxsockets">Maximum number of sockets to watch.  In this version,
        /// this is mostly for rate-limiting purposes.</param>
        public SocketWatcher(int maxsockets)
        {
            m_maxSocks = maxsockets;
        }

        /// <summary>
        /// Synchronous operation
        /// </summary>
        public bool Synchronous
        {
            get { return m_synch; }
            set { m_synch = value; }
        }

        /// <summary>
        /// The maximum number of sockets watched.  Throws
        /// InvalidOperationException if the new value is fewer than the number
        /// currently open.  -1 means no limit.
        /// </summary>
        public int MaxSockets
        {
            get { return m_maxSocks; }
            set
            {
                lock(m_lock)
                {
                    if ((value >= 0) && (m_socks.Count >= value))
                        throw new InvalidOperationException("Too many sockets: " + m_socks.Count);

                    m_maxSocks = value;
                }
            }
        }

#if NET20 || __MonoCS__
        /// <summary>
        /// The certificate to be used for the local side of sockets, with SSL on.
        /// </summary>
        public X509Certificate LocalCertificate
        {
            get { return m_cert; }
            set { m_cert = value; }
        }

        /// <summary>
        /// Does the server require a client cert?  If not, the client cert won't be sent.
        /// </summary>
        public bool RequireClientCert
        {
            get { return m_requireClientCert; }
            set { m_requireClientCert = value; }
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
        /// If you leave the certificate null, and you are doing Accept, the SSL class will try to find a
        /// default server cert on your box.  If you have IIS installed with a cert, this might just go...
        /// </summary>
        /// <param name="filename">A .pfx or .cer file</param>
        /// <param name="password">The password, if this is a .pfx file, null if .cer file.</param>
#if NET20
        public void SetCertificateFile(string filename,
                                       string password)
        {
            m_cert = new X509Certificate2(filename, password);
            // TODO: check cert for validity
        }
#else
        public void SetCertificateFile(string filename,
                                       string password)
        {
            byte[] data = null;
            using (FileStream fs = new FileStream(filename, FileMode.Open)) {
                data = new byte[fs.Length];
                fs.Read(data, 0, data.Length);
                fs.Close ();
            }

            Mono.Security.X509.PKCS12 pfx =
                new Mono.Security.X509.PKCS12(data, password);
            if (pfx.Certificates.Count > 0)
                m_cert = new X509Certificate(pfx.Certificates[0].RawData);
            // TODO: check cert for validity
        }
#endif

 #if NET20
        /// <summary>
        /// Set the certificate from a system store.  Try "MY" for the ones listed in IE.
        /// </summary>
        /// <param name="storeName"></param>
        public void SetCertificateStore(StoreName storeName)
        {
            throw new NotImplementedException("Not implemented yet.  Need to figure out how to search for 'server' certs.");
        }
#endif

#elif !NO_SSL
        /// <summary>
        /// The certificate to be used for the local side of sockets, with SSL on.
        /// </summary>
        public Certificate LocalCertificate
        {
            get { return m_cert; }
            set { m_cert = value; }
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
        /// If you leave the certificate null, and you are doing Accept, the SSL class will try to find a
        /// default server cert on your box.  If you have IIS installed with a cert, this might just go...
        /// </summary>
        /// <param name="filename">A .pfx or .cer file</param>
        /// <param name="password">The password, if this is a .pfx file, null if .cer file.</param>
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
            m_cert = CertUtil.FindServerCert(store);
            if (m_cert == null)
                throw new CertificateException("The certificate file does not contain a server authentication certificate.");
        }

        /// <summary>
        /// Set the certificate from a system store.  Try "MY" for the ones listed in IE.
        /// </summary>
        /// <param name="storeName"></param>
        public void SetCertificateStore(string storeName)
        {
            CertificateStore store = new CertificateStore(storeName);

            m_cert = CertUtil.FindServerCert(store);
            if (m_cert == null)
                throw new CertificateException("The certificate file does not contain a server authentication certificate.");
        }
#endif

        /// <summary>
        /// Create a socket that is listening for inbound connections.
        /// </summary>
        /// <param name="listener">Where to send notifications</param>
        /// <param name="addr">Address to connect to</param>
        /// <param name="backlog">The maximum length of the queue of pending connections</param>
        /// <param name="SSL">Do SSL3/TLS1 on connect</param>
        /// <returns>A socket that is ready for calling RequestAccept()</returns>
        public AsyncSocket CreateListenSocket(ISocketEventListener listener,
                                              Address              addr,
                                              int                  backlog,
                                              bool                 SSL)
        {
            //Debug.Assert(m_maxSocks > 1);
            AsyncSocket result = new AsyncSocket(this, listener, SSL, m_synch);
            if (SSL)
            {
#if !NO_SSL
                result.LocalCertificate = m_cert;
#if NET20
                result.RequireClientCert = m_requireClientCert;
#endif
#else
                throw new NotImplementedException("SSL not compiled in");
#endif
            }
            result.Accept(addr, backlog);
            return result;
        }

        /// <summary>
        /// Create a socket that is listening for inbound connections.
        /// </summary>
        /// <param name="listener">Where to send notifications</param>
        /// <param name="addr">Address to connect to</param>
        /// <param name="SSL">Do SSL3/TLS1 on connect</param>
        /// <returns>A socket that is ready for calling RequestAccept()</returns>
        public AsyncSocket CreateListenSocket(ISocketEventListener listener,
                                              Address              addr,
                                              bool                 SSL)
        {
            return CreateListenSocket(listener, addr, 5, SSL);
        }

        /// <summary>
        /// Create a socket that is listening for inbound connections, with no SSL/TLS.
        /// </summary>
        /// <param name="listener">Where to send notifications</param>
        /// <param name="addr">Address to connect to</param>
        /// <returns>A socket that is ready for calling RequestAccept()</returns>
        public AsyncSocket CreateListenSocket(ISocketEventListener listener,
            Address              addr)
        {
            return CreateListenSocket(listener, addr, 5, false);
        }

        /// <summary>
        /// Create a socket that is listening for inbound connections, with no SSL/TLS.
        /// </summary>
        /// <param name="listener">Where to send notifications</param>
        /// <param name="addr">Address to connect to</param>
        /// <param name="backlog">The maximum length of the queue of pending connections</param>
        /// <returns>A socket that is ready for calling RequestAccept()</returns>
        public AsyncSocket CreateListenSocket(ISocketEventListener listener,
                                              Address              addr,
                                              int                  backlog)
        {
            return CreateListenSocket(listener, addr, backlog, false);
        }

        /// <summary>
        /// Create an outbound socket.
        /// </summary>
        /// <param name="listener">Where to send notifications</param>
        /// <param name="addr">Address to connect to</param>
        /// <returns>Socket that is in the process of connecting</returns>
        public AsyncSocket CreateConnectSocket(ISocketEventListener listener,
                                               Address              addr)
        {
            return CreateConnectSocket(listener, addr, false, null);
        }

        /// <summary>
        /// Create an outbound socket.
        /// </summary>
        /// <param name="listener">Where to send notifications</param>
        /// <param name="addr">Address to connect to</param>
        /// <param name="SSL">Do SSL3/TLS1 on startup</param>
        /// <param name="hostId">The logical name of the host to connect to, for SSL/TLS purposes.</param>
        /// <returns>Socket that is in the process of connecting</returns>
        public AsyncSocket CreateConnectSocket(ISocketEventListener listener,
                                               Address              addr,
                                               bool                 SSL,
                                               string               hostId)
        {
            AsyncSocket result;

            // Create the socket:
            result = new AsyncSocket(this, listener, SSL, m_synch);
            if (SSL)
            {
#if !NO_SSL && !__MonoCS__
                result.LocalCertificate = m_cert;
#else
                throw new NotImplementedException("SSL not compiled in");
#endif
            }
            // Start the connect process:
            result.Connect(addr, hostId);
            return result;
        }

        /// <summary>
        /// Called by AsyncSocket when a new connection is received on a listen socket.
        /// </summary>
        /// <param name="s">New socket connection</param>
        public void RegisterSocket(AsyncSocket s)
        {

            lock (m_lock)
            {
                if ((m_maxSocks >= 0) && (m_socks.Count >= m_maxSocks))
                    throw new InvalidOperationException("Too many sockets: " + m_socks.Count);
                m_socks.Add(s);
            }
        }

        /// <summary>
        /// Called by AsyncSocket when a socket is closed.
        /// </summary>
        /// <param name="s">Closed socket</param>
        public void CleanupSocket(AsyncSocket s)
        {
            lock (m_lock)
            {
                m_socks.Remove(s);

                if (m_pending.Contains(s))
                {
                    m_pending.Remove(s);
                }
                else
                {
                    foreach (AsyncSocket sock in m_pending)
                    {
                        sock.RequestAccept();
                    }
                    m_pending.Clear();
                }
            }
        }

        /// <summary>
        /// Called by AsyncSocket when this class is full, and the listening AsyncSocket
        /// socket would like to be restarted when there are slots free.
        /// </summary>
        /// <param name="s">Listening socket</param>
        public void PendingAccept(AsyncSocket s)
        {
            lock (m_lock)
            {
                m_pending.Add(s);
            }
        }

        /// <summary>
        /// Or close.  Potato, tomato.  This is useful if you want to use using().
        /// </summary>
        public void Dispose()
        {
            lock (m_lock)
            {
                m_pending.Clear();
                foreach (AsyncSocket s in m_socks)
                {
                    s.Close();
                }
                m_socks.Clear();
            }
        }
    }
}
