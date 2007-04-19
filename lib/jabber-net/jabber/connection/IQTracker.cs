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
using System.Threading;
using System.Xml;

using bedrock.util;
using jabber.protocol.client;

namespace jabber.connection
{
    /// <summary>
    /// Received a response to an IQ request.
    /// </summary>
    public delegate void IqCB(object sender, IQ iq, object data);

    /// <summary>
    /// An IQ has timed out.
    /// </summary>
    public class IQTimeoutException : Exception
    {
        /// <summary>
        /// Create a timeout exception
        /// </summary>
        /// <param name="message"></param>
        public IQTimeoutException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Track outstanding IQ requests.
    /// </summary>
    [SVN(@"$Id: IQTracker.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class IQTracker
    {
        private Hashtable  m_pending = new Hashtable();
        private XmppStream m_cli     = null;

        /// <summary>
        /// Create a new IQ tracker
        /// </summary>
        /// <param name="stream">The client to send/receive on</param>
        public IQTracker(XmppStream stream)
        {
            m_cli = stream;
            m_cli.OnProtocol += new jabber.protocol.ProtocolHandler(OnIQ);
        }

        private void OnIQ(object sender, XmlElement elem)
        {
            IQ iq = elem as IQ;
            if (iq == null)
                return;

            string id = iq.ID;
            TrackerData td;

            lock (m_pending)
            {
                td = (TrackerData) m_pending[id];

                // this wasn't one that was being tracked.
                if (td == null)
                {
                    return;
                }
                m_pending.Remove(id);
            }

            // don't need to check for null.  protected by check below.
            td.cb(this, iq, td.data);
        }

        /// <summary>
        /// Start an IQ request.
        /// </summary>
        public void BeginIQ(IQ iq, IqCB cb, object cbArg)
        {
            // if no callback, ignore response.
            if (cb != null)
            {
                TrackerData td = new TrackerData();
                td.cb   = cb;
                td.data = cbArg;
                lock (m_pending)
                {
                    m_pending[iq.ID] = td;
                }
            }
            m_cli.Write(iq);
        }

        /// <summary>
        /// Do a synchronous IQ request, which waits for a response.
        /// </summary>
        /// <param name="iqp">An IQ packet to send, and wait for.</param>
        /// <param name="millisecondsTimeout">Time to wait for response, in milliseconds</param>
        public IQ IQ(IQ iqp, int millisecondsTimeout)
        {
            TrackerData td = new TrackerData();
            td.cb   = new IqCB(SignalEvent);
            AutoResetEvent are = new AutoResetEvent(false);
            td.data = are;
            string id = iqp.ID;
            lock (m_pending)
            {
                m_pending[id] = td;
            }
            m_cli.Write(iqp);

            if (!are.WaitOne(millisecondsTimeout, true))
            {
                throw new Exception("Timeout waiting for IQ response");
            }

            lock (m_pending)
            {
                IQ resp = (IQ) m_pending[id];
                m_pending.Remove(id);
                return resp;
            }
        }

        private void SignalEvent(object sender, IQ iq, object data)
        {
            m_pending[iq.ID] = iq;
            ((AutoResetEvent)data).Set();
        }

        private class TrackerData
        {
            public IqCB  cb;
            public object data;
        }
    }
}
