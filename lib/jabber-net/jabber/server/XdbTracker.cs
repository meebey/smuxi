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
using System.Xml;

using bedrock.util;
using jabber.protocol.accept;

namespace jabber.server
{
    /// <summary>
    /// Received a response to an XDB request.
    /// </summary>
    public delegate void XdbCB(object sender, Xdb xdb, object data);

    /// <summary>
    /// Track outstanding XDB requests.
    /// </summary>
    [SVN(@"$Id: XdbTracker.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class XdbTracker
    {
        // this hash doesn't need concurrency control, i don't think,
        // since no id will be re-used.
        private Hashtable       m_pending = new Hashtable();
        private JabberService   m_comp    = null;

        /// <summary>
        /// Create a new XDB tracker
        /// </summary>
        /// <param name="comp">The component to send/receive on</param>
        public XdbTracker(JabberService comp)
        {
            m_comp = comp;
            m_comp.OnXdb += new XdbHandler(OnXdb);
        }

        /// <summary>
        /// Received an XDB element on Component.
        /// Is this a response to a tracked request?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="xdb"></param>
        private void OnXdb(object sender, Xdb xdb)
        {
            string id = xdb.ID;
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

            // don't need to check for null.  protected by assert below.
            td.cb(this, xdb, td.data);
        }

        /// <summary>
        /// Start an XDB request.
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="ns"></param>
        /// <param name="cb"></param>
        /// <param name="cbArg"></param>
        public void BeginXdbGet(string owner, string ns,
            XdbCB cb, object cbArg)
        {
            BeginXdb(null, XdbType.get, owner, ns, XdbAction.NONE, cb, cbArg);
        }

        /// <summary>
        /// Start an XDB request.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="owner"></param>
        /// <param name="ns"></param>
        /// <param name="cb"></param>
        /// <param name="cbArg"></param>
        public void BeginXdbSet(XmlElement root, string owner, string ns,
            XdbCB cb, object cbArg)
        {
            BeginXdb(root, XdbType.set, owner, ns, XdbAction.NONE, cb, cbArg);
        }

        /// <summary>
        /// Start an XDB request.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="xtype"></param>
        /// <param name="owner"></param>
        /// <param name="ns"></param>
        /// <param name="cb"></param>
        /// <param name="cbArg"></param>
        public void BeginXdb(XmlElement root, XdbType xtype,
            string owner, string ns,
            XdbCB cb, object cbArg)
        {
            BeginXdb(root, xtype, owner, ns, XdbAction.NONE, cb, cbArg);
        }

        /// <summary>
        /// Start an XDB request.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="xtype"></param>
        /// <param name="owner"></param>
        /// <param name="ns"></param>
        /// <param name="action"></param>
        /// <param name="cb"></param>
        /// <param name="cbArg"></param>
        public void BeginXdb(XmlElement root, XdbType xtype,
            string owner, string ns, XdbAction action,
            XdbCB cb, object cbArg)
        {
            Debug.Assert(owner != null);
            Debug.Assert(ns    != null);
            Xdb xdb  = new Xdb(m_comp.Document);
            xdb.NS   = ns;
            xdb.Type = xtype;
            xdb.To   = owner;
            xdb.From = m_comp.ComponentID;
            if (action != XdbAction.NONE)
                xdb.Action = action;
            if (root != null)
                xdb.AddChild(root);
            // if no callback, ignore response.
            if (cb != null)
            {
                TrackerData td = new TrackerData();
                td.cb   = cb;
                td.data = cbArg;
                lock (m_pending)
                {
                    m_pending[xdb.ID] = td;
                }
            }
            m_comp.Write(xdb);
        }

        private class TrackerData
        {
            public XdbCB  cb;
            public object data;
        }
    }
}
