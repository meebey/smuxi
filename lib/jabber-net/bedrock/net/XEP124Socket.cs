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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Schema;


using bedrock.io;
using bedrock.util;

namespace bedrock.net
{
    /// <summary>
    /// XEP-0124 Error conditions
    /// </summary>
    [SVN(@"$Id: XEP124Socket.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class XEP124Exception : WebException
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="reason"></param>
        public XEP124Exception(string reason) : base(reason)
        {
        }
    }

    /// <summary>
    /// Make a XEP-124 (http://www.xmpp.org/extensions/xep-0124.html) polling "connection" look like a socket.
    /// TODO: get rid of the PipeStream, if possible.
    /// </summary>
    [SVN(@"$Id: XEP124Socket.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class XEP124Socket : BaseSocket
    {
        private const string CONTENT_TYPE = "text/xml; charset=utf-8";
        private const string METHOD       = "POST";

        RandomNumberGenerator s_rng = RNGCryptoServiceProvider.Create();

        private Queue      m_writeQ  = new Queue();
        private Object     m_lock    = new Object();
        private Thread     m_thread  = null;
        private int        m_hold    = 5;
        private int        m_wait    = 60;
        private int        m_requests = 0;
        private int        m_polling = 1;
        private int        m_awaiting = 0;
        private int        m_maxPoll = 30;
        private int        m_minPoll = 1;
        private string     m_url     = null;
        private string[]   m_keys    = null;
        private int        m_numKeys = 512;
        private int        m_curKey  = 511;
        private bool       m_running = false;
        private string     m_rid     = null;
        private string     m_sid     = null;
        private WebProxy   m_proxy   = null;
        private X509Certificate m_remote_cert = null;

        /// <summary>
        /// Create an instance
        /// </summary>
        /// <param name="listener"></param>
        public XEP124Socket(ISocketEventListener listener)
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
        /// Accept a socket.  Not implemented.
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="backlog"></param>
        public override void Accept(Address addr, int backlog)
        {
            throw new NotImplementedException("HTTP binding server not implemented yet");
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
            m_curKey = -1;

            if (m_thread == null)
            {
                m_running = true;
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
            lock (m_lock)
            {
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

        // increments integer representated in string
        private string Increment(string value)
        {
            StringBuilder sb = new StringBuilder(value);
            int l = sb.Length - 1;
            bool carry = true;
            while (carry && l >= 0)
            {
                int d = int.Parse(sb[l].ToString());
                carry = ((d++ == 9) ? 0 : d) == 0;
                sb[l--] = d.ToString()[0];
            }
            if (carry)
                sb.Insert(0, '1');

            return sb.ToString();
        }


        /// <summary>
        /// Keep polling until
        /// </summary>
        private void PollThread()
        {
            while (m_running)
            {

                if ((m_rid == null) || (m_awaiting < m_requests-1))
                {

                    string body = "<body content='" + CONTENT_TYPE + "' xmlns='http://jabber.org/protocol/httpbind'";
                    // lock this
                    if (m_rid == null)
                    {
                        Random rnd = new Random();
                        m_rid = rnd.Next().ToString();

                        Uri uri = new Uri(m_url);

                        body += " to='" + uri.Host + "'";
                        body += " wait='" + m_wait + "'";
                        body += " hold='" + m_hold + "'";
                        body += " xml:lang='en'";
                    }
                    else
                    {
                        m_rid = Increment(m_rid);

                        if (m_sid == null)
                        {
                        }
                        else
                        {
                            body += " sid='" + m_sid + "'";
                        }

                    }
                    body += " rid='" + m_rid + "'";


                    body += ">";


                    HttpWebRequest req;
                    Stream s;
                    WriteBuf b = new WriteBuf(body);



                    req = (HttpWebRequest)WebRequest.Create(m_url + "/webclient");
                    req.ContentType = CONTENT_TYPE;
                    req.Method = METHOD;
                    req.KeepAlive = false;



#if NET20
                    req.CachePolicy = new System.Net.Cache.HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel.NoCacheNoStore);
#endif

                    if (m_proxy != null)
                        req.Proxy = m_proxy;

                    // try-catch
                    s = req.GetRequestStream();
                    s.Write(b.buf, b.offset, b.len);

                    while (m_writeQ.Count > 0)
                    {
                        b = (WriteBuf)m_writeQ.Dequeue();
                        s.Write(b.buf, b.offset, b.len);
                    }

                    b = new WriteBuf("</body>");
                    s.Write(b.buf, b.offset, b.len);

                    s.Close();

                    ++m_awaiting;
                    req.BeginGetResponse(new AsyncCallback(GotResponse), req);

                }

                lock (m_lock)
                {
                    Monitor.Wait(m_lock, m_polling*1000);
                }
            }
        }

        /// <summary>
        /// Descripton, including URL.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "XEP-0124 socket: " + m_url;
        }


        private void GotResponse(IAsyncResult result)
        {

            HttpWebRequest request = (HttpWebRequest)result.AsyncState;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);
                --m_awaiting;

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    m_listener.OnError(this, new WebException("Invalid HTTP return code: " + response.StatusCode));
                    return;
                }

                StreamReader reader = new StreamReader(response.GetResponseStream());

                string xml = reader.ReadToEnd();
                reader.Close();
                /*
                string[] tok = {"</body>"};
                string[] tokens = xml.Split(tok, StringSplitOptions.None);
                string body;
                string content = null;
                if (tokens.Length == 2)
                {
                    body = tokens[0].Substring(0, xml.IndexOf('>')) + "/>";
                    content = tokens[0].Substring(body.Length - 1);
                }
                else
                    body = xml;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(body);

                if (doc.DocumentElement.Attributes["sid"] != null)
                    m_sid = doc.DocumentElement.Attributes["sid"].Value;

                if (doc.DocumentElement.Attributes["authid"] != null)
                    m_authid = doc.DocumentElement.Attributes["authid"].Value;

                if (doc.DocumentElement.Attributes["requests"] != null)
                    m_requests = int.Parse(doc.DocumentElement.Attributes["requests"].Value);

                if (doc.DocumentElement.Attributes["wait"] != null)
                    m_wait = int.Parse(doc.DocumentElement.Attributes["wait"].Value);

                if (doc.DocumentElement.Attributes["polling"] != null)
                    m_polling = int.Parse(doc.DocumentElement.Attributes["polling"].Value);

                if (doc.DocumentElement.Attributes["inactivity"] != null)
                    m_inactivity = int.Parse(doc.DocumentElement.Attributes["inactivity"].Value);

                if (content != null)
                {
                    WriteBuf buf = new WriteBuf(content);

                    if (!m_listener.OnRead(this, buf.buf, 0, buf.len))
                    {
                        Close();
                        return;
                    }
                }*/
                if (xml != null)
                {
                    WriteBuf buf = new WriteBuf(xml);

                    if (!m_listener.OnRead(this, buf.buf, 0, buf.len))
                    {
                        Close();
                        return;
                    }
                }


            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.KeepAliveFailure)
                    m_listener.OnError(this, ex);
            }


        }

        /// <summary>
        /// Are we connected?
        /// </summary>
        public bool Connected
        {
            get
            { return m_running; }
        }

        /// <summary>
        /// The certificate from the server.
        /// </summary>
        public X509Certificate RemoteCertificate
        {
            get { return m_remote_cert; }
            set { m_remote_cert = value; }
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
    }
}
