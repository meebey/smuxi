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
using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

using bedrock.io;
using bedrock.util;

namespace bedrock.net
{


#if !NET20

    /// <summary>
    /// Certificate policy that trusts all
    /// </summary>
    [SVN(@"$Id: XEP25Socket.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class TrustAllCertificatePolicy : System.Net.ICertificatePolicy
    {
        /// <summary>
        /// Construction
        /// </summary>
        public TrustAllCertificatePolicy()
        {}

        /// <summary>
        /// Check validation callback
        /// </summary>
        public bool CheckValidationResult(System.Net.ServicePoint sp,
            System.Security.Cryptography.X509Certificates.X509Certificate cert,System.Net.WebRequest req, int problem)
        {
            return true;
        }
    }

#endif


    /// <summary>
    /// XEP25 Error conditions
    /// </summary>
    [SVN(@"$Id: XEP25Socket.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class XEP25Exception : WebException
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="reason"></param>
        public XEP25Exception(string reason) : base(reason)
        {
        }
    }

    /// <summary>
    /// Make a XEP-25 (http://www.xmpp.org/extensions/xep-0025.html) polling "connection" look like a socket.
    /// TODO: get rid of the PipeStream, if possible.
    /// </summary>
    [SVN(@"$Id: XEP25Socket.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class XEP25Socket : BaseSocket
    {
        private const string CONTENT_TYPE = "application/x-www-form-urlencoded";
        private const string METHOD       = "POST";

        RandomNumberGenerator s_rng = RNGCryptoServiceProvider.Create();

        private Queue      m_writeQ  = new Queue();
        private Object     m_lock    = new Object();
        private Thread     m_thread  = null;
        private int        m_maxPoll = 30;
        private int        m_minPoll = 1;
        private double     m_curPoll = 1.0;
        private string     m_url     = null;
        private string[]   m_keys    = null;
        private int        m_numKeys = 512;
        private int        m_curKey  = 511;
        private bool       m_running = false;
        private string     m_id      = null;
        private WebProxy   m_proxy   = null;
        private X509Certificate m_cert = null;
        private X509Certificate m_remote_cert = null;

        /// <summary>
        /// Do trust all server sertificates?
        /// </summary>
        public static bool UntrustedRootOK = false;

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="listener"></param>
        public XEP25Socket(ISocketEventListener listener)
        {
            Debug.Assert(listener != null);
            m_listener = listener;
        }

        /// <summary>
        /// Maximum time between polls, in seconds
        /// </summary>
        public int MaxPoll
        {
            get { return m_maxPoll; }
            set { m_maxPoll = value; }
        }

        /// <summary>
        /// Minimum time between polls, in seconds
        /// </summary>
        public int MinPoll
        {
            get { return m_minPoll; }
            set { m_minPoll = value; }
        }

        /// <summary>
        /// The URL to poll
        /// </summary>
        public string URL
        {
            get { return m_url; }
            set { m_url = value; }
        }


        /// <summary>
        /// The number of keys to generate at a time.  Higher numbers use more memory,
        /// and more CPU to generate keys, less often.  Defaults to 512.
        /// </summary>
        public int NumKeys
        {
            get { return m_numKeys; }
            set { m_numKeys = value; }
        }

        /// <summary>
        /// Proxy information.  My guess is if you leave this null, the IE proxy
        /// info may be used.  Not tested.
        /// </summary>
        public WebProxy Proxy
        {
            get { return m_proxy; }
            set { m_proxy = value; }
        }

        /// <summary>
        /// The local certificate of the socket.
        /// </summary>
        public X509Certificate LocalCertificate
        {
            get { return m_cert; }
            set { m_cert = value; }
        }

        /// <summary>
        /// The remote certificate.
        /// </summary>
        public X509Certificate RemoteCertificate
        {
            get { return m_remote_cert; }
            set { m_remote_cert = value; }
        }

        /// <summary>
        /// Accept a socket.  Not implemented.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="backlog"></param>
        public override void Accept(Address addr, int backlog)
        {
            throw new NotImplementedException("HTTP polling server not implemented yet");
        }

        /// <summary>
        /// Stop polling.
        /// </summary>
        public override void Close()
        {
            lock (m_lock)
            {
                m_running = false;
                Monitor.Pulse(m_lock);
            }
            m_listener.OnClose(this);
        }



        /// <summary>
        /// Start polling
        /// </summary>
        /// <param name="addr"></param>
        public override void Connect(Address addr)
        {
            Debug.Assert(m_url != null);
            m_running = true;
            m_curKey = -1;

            if (m_thread == null)
            {
                m_thread = new Thread(new ThreadStart(PollThread));
                m_thread.IsBackground = true;
                m_thread.Start();
            }

            m_listener.OnConnect(this);
        }

        /// <summary>
        /// Not implemented
        /// </summary>
        public override void RequestAccept()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start reading.
        /// </summary>
        public override void RequestRead()
        {
            if (!m_running)
                throw new InvalidOperationException("Call Connect() first");
        }

#if !NO_SSL

        /// <summary>
        /// Start TLS over this connection.  Not implemented.
        /// </summary>
        public override void StartTLS()
        {
            throw new NotImplementedException();
        }
#endif

        /// <summary>
        /// Send bytes to the jabber server
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="len"></param>
        public override void Write(byte[] buf, int offset, int len)
        {
            if (!m_running)
                throw new InvalidOperationException("Call Connect() first");

            lock (m_lock)
            {
                //if (m_thread == null)
                //{
                //    // first write
                //    m_thread = new Thread(new ThreadStart(PollThread));
                //    m_thread.IsBackground = true;
                //    m_thread.Start();
                //}
                m_writeQ.Enqueue(new WriteBuf(buf, offset, len));
                Monitor.Pulse(m_lock);
            }
        }

        private void GenKeys()
        {
            byte[] seed = new byte[32];
            SHA1 sha = SHA1.Create();
            Encoding ENC = Encoding.ASCII; // All US-ASCII.  No need for UTF8.
            string prev;

            // K(n, seed) = Base64Encode(SHA1(K(n - 1, seed))), for n > 0
            // K(0, seed) = seed, which is client-determined

            s_rng.GetBytes(seed);
            prev = Convert.ToBase64String(seed);
            m_keys = new string[m_numKeys];
            for (int i=0; i<m_numKeys; i++)
            {
                m_keys[i] = Convert.ToBase64String(sha.ComputeHash(ENC.GetBytes(prev)));
                prev = m_keys[i];
            }
            m_curKey = m_numKeys - 1;
        }

#if NET20

        private bool ValidateRemoteCertificate(Object sender,
                                               X509Certificate certificate,
                                               X509Chain chain,
                                               System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return UntrustedRootOK;
        }

#endif


        /// <summary>
        /// Keep polling until
        /// </summary>
        private void PollThread()
        {
            m_curPoll = m_minPoll;
            m_id = null;

            MemoryStream ms = new MemoryStream();
            CookieContainer cookies = new CookieContainer(5);
            byte[] readbuf = new byte[1024];

            Stream rs;
            byte[] buf;
            int readlen;
            HttpWebResponse resp;
            HttpWebRequest req;
            Stream s;
            WriteBuf start;

            int count;
            while (m_running)
            {
                lock (m_lock)
                {
                    if (m_writeQ.Count == 0)
                    {
                        Monitor.Wait(m_lock, (int)(m_curPoll * 1000.0));
                    }
                }
                // did we get closed?
                if (!m_running)
                    break;


                if (m_id == null)
                {
                    GenKeys();
                    start = new WriteBuf(string.Format("0;{0},", m_keys[m_curKey]));
                }
                else
                {
                    if (m_curKey == 0)
                    {
                        string k = m_keys[0];
                        GenKeys();
                        start = new WriteBuf(string.Format("{0};{1};{2},", m_id, k, m_keys[m_curKey]));
                    }
                    else
                    {
                        start = new WriteBuf(string.Format("{0};{1},", m_id, m_keys[m_curKey]));
                    }
                }
                m_curKey--;

                ms.SetLength(0);
                count = start.len;
                while (m_writeQ.Count > 0)
                {
                    WriteBuf b = (WriteBuf) m_writeQ.Dequeue();
                    count += b.len;
                    ms.Write(b.buf, b.offset, b.len);
                }

            POLL:
                req = (HttpWebRequest)WebRequest.Create(m_url);
                req.CookieContainer = cookies;
                req.ContentType     = CONTENT_TYPE;
                req.Method          = METHOD;

                if (m_cert != null)
                    req.ClientCertificates.Add(m_cert);

                req.KeepAlive       = false;

#if NET20
                req.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.NoCacheNoStore);

#endif


                if (m_proxy != null)
                    req.Proxy = m_proxy;
                req.ContentLength = count;



                resp = null;
                try
                {
#if NET20
                    ServicePointManager.ServerCertificateValidationCallback =
                        new System.Net.Security.RemoteCertificateValidationCallback(ValidateRemoteCertificate);
#else
                    if (UntrustedRootOK)
                        ServicePointManager.CertificatePolicy = new TrustAllCertificatePolicy();
                    else
                        ServicePointManager.CertificatePolicy = null;

#endif
                    s = req.GetRequestStream();
                    s.Write(start.buf, start.offset, start.len);

                    m_remote_cert = req.ServicePoint.Certificate;

                    buf = ms.ToArray();
                    s.Write(buf, 0, buf.Length);
                    s.Close();

                    resp = (HttpWebResponse) req.GetResponse();
                }
                catch (WebException ex)
                {
                    if (ex.Status != WebExceptionStatus.KeepAliveFailure)
                    {
                        m_listener.OnError(this, ex);
                        return;
                    }
                    goto POLL;
                }



                if (resp.StatusCode != HttpStatusCode.OK)
                {
                    m_listener.OnError(this, new WebException("Invalid HTTP return code: " + resp.StatusCode));
                    return;
                }

                CookieCollection cc = resp.Cookies;
                Debug.Assert(cc != null);

                Cookie c = cc["ID"];
                if ((c == null) || (c.Value == null))
                {
                    m_listener.OnError(this, new WebException("No ID cookie returned"));
                    return;
                }

                if (m_id == null)
                {
                    // if ID ends in :0, it's an error
                    if (!c.Value.EndsWith(":0"))
                        m_id = c.Value;
                }

                if (m_id != c.Value)
                {
                    switch (c.Value)
                    {
                        case "0:0":
                            m_listener.OnError(this, new XEP25Exception("Unknown XEP25 error"));
                            return;
                        case "-1:0":
                            m_listener.OnError(this, new XEP25Exception("Server error"));
                            return;
                        case "-2:0":
                            m_listener.OnError(this, new XEP25Exception("Bad request"));
                            return;
                        case "-3:0":
                            m_listener.OnError(this, new XEP25Exception("Key sequence error"));
                            return;
                        default:
                            m_listener.OnError(this, new WebException("ID cookie changed"));
                            return;
                    }
                }

                if (ms.Length > 0)
                {
                    m_listener.OnWrite(this, buf, 0, buf.Length);
                }

                ms.SetLength(0);
                rs = resp.GetResponseStream();




                while ((readlen = rs.Read(readbuf, 0, readbuf.Length)) > 0)
                {
                    ms.Write(readbuf, 0, readlen);
                }
                rs.Close();
                if (ms.Length > 0)
                {
                    buf = ms.ToArray();

                    try
                    {
                        if (!m_listener.OnRead(this, buf, 0, buf.Length))
                        {
                            Close();
                            return;
                        }
                    } catch (NullReferenceException)
                    {}
                    buf = null;
                    m_curPoll = m_minPoll;
                }
                else
                {
                    buf = null;
                    m_curPoll *= 1.25;
                    if (m_curPoll > m_maxPoll)
                        m_curPoll = m_maxPoll;
                }
            }
        }

        /// <summary>
        /// Is socket connected.
        /// </summary>
        public bool Connected
        {
            get
            { return m_running; }
        }

        /// <summary>
        /// Descripton, including poll URL.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "XEP-0025 Polling socket: " + m_url;
        }

        private class WriteBuf
        {
            public byte[] buf;
            public int offset;
            public int len;

            public WriteBuf(byte[] buf, int offset, int len)
            {
                this.buf = buf;
                this.offset = offset;
                this.len = len;
            }

            public WriteBuf(string b)
            {
                this.buf = Encoding.UTF8.GetBytes(b);
                this.offset = 0;
                this.len = buf.Length;
            }
        }
    }}
