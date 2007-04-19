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
using System.ComponentModel.Design;
using System.Collections;
using System.Diagnostics;

using bedrock.util;
using bedrock.collections;

using jabber.protocol.client;

namespace jabber.client
{

    /// <summary>
    /// A change of derived primary session for a user
    /// </summary>
    /// <param name="sender">The PresenceManager object that sent the update</param>
    /// <param name="bare">The bare JID (node@domain) of the user whose presence changed</param>
    public delegate void PrimarySessionHandler(object sender, JID bare);

    /// <summary>
    /// Presence proxy database.
    /// </summary>
    [SVN(@"$Id: PresenceManager.cs 340 2007-03-02 20:35:59Z hildjj $")]
    public class PresenceManager : System.ComponentModel.Component, IEnumerable
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private JabberClient m_client = null;
        private Tree m_items = new Tree();

        /// <summary>
        /// Construct a PresenceManager object.
        /// </summary>
        /// <param name="container"></param>
        public PresenceManager(System.ComponentModel.IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        /// <summary>
        /// Construct a PresenceManager object.
        /// </summary>
        public PresenceManager()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The JabberClient to hook up to.
        /// </summary>
        [Description("The JabberClient to hook up to.")]
        [Category("Jabber")]
        public JabberClient Client
        {
            get
            {
                // If we are running in the designer, let's try to get an invoke control
                // from the environment.  VB programmers can't seem to follow directions.
                if ((this.m_client == null) && DesignMode)
                {
                    IDesignerHost host = (IDesignerHost) base.GetService(typeof(IDesignerHost));
                    if (host != null)
                    {
                        Component root = host.RootComponent as Component;
                        if (root != null)
                        {
                            foreach (Component c in root.Container.Components)
                            {
                                if (c is JabberClient)
                                {
                                    m_client = (JabberClient) c;
                                    break;
                                }
                            }
                        }
                    }
                }
                return m_client;
            }
            set
            {
                m_client = value;
                m_client.OnPresence += new PresenceHandler(GotPresence);
                m_client.OnDisconnect += new bedrock.ObjectHandler(GotDisconnect);
            }
        }

        /// <summary>
        /// The primary session has changed for a user.
        /// </summary>
        public event PrimarySessionHandler OnPrimarySessionChange;

        private void GotDisconnect(object sender)
        {
            lock(this)
                m_items.Clear();
        }

        /// <summary>
        /// Add a new available or unavailable presence packet to the database.
        /// </summary>
        /// <param name="p"></param>
        public void AddPresence(Presence p)
        {
            // can't use .From, since that will cause a JID parse.
            Debug.Assert(p.GetAttribute("from") != "",
                "Do not call AddPresence by hand.  I can tell you are doing that because you didn't put a from address on your presence packet, and all presences from the server have a from address.");
            GotPresence(this, p);
        }

        private void GotPresence(object sender, Presence p)
        {
            PresenceType t = p.Type;
            if ((t != PresenceType.available) &&
                (t != PresenceType.unavailable))
                return;

            JID f = p.From;
            lock (this)
            {
                UserPresenceManager upm = (UserPresenceManager)m_items[f.Bare];

                if (t == PresenceType.available)
                {
                    if (upm == null)
                    {
                        upm = new UserPresenceManager(f.Bare);
                        m_items[f.Bare] = upm;
                    }

                    upm.AddPresence(p, this);
                }
                else
                {
                    if (upm != null)
                    {
                        upm.RemovePresence(p, this);
                        if (upm.Count == 0)
                        {
                            m_items.Remove(f.Bare);
                        }
                    }
                }
            }
        }

        private void FireOnPrimarySessionChange(JID from)
        {
            if (OnPrimarySessionChange != null)
                OnPrimarySessionChange(this, from);
        }

        /// <summary>
        /// Is this user online with any resource?  This performs better than retrieving
        /// the particular associated presence packet.
        /// </summary>
        /// <param name="jid">The JID to look up.</param>
        /// <returns></returns>
        public bool IsAvailable(JID jid)
        {
            lock (this)
            {
                return (m_items[jid.Bare] != null);
            }
        }

        /// <summary>
        /// If given a bare JID, get the primary presence.
        /// If given a FQJ, return the associated presence.
        /// </summary>
        public Presence this[JID jid]
        {
            get
            {
                lock (this)
                {
                    UserPresenceManager upm = (UserPresenceManager)m_items[jid.Bare];
                    if (upm == null)
                        return null;
                    return upm[jid.Resource];
                }
            }
        }

        /// <summary>
        /// Get all of the current presence stanzas for the given user.
        /// </summary>
        /// <param name="jid"></param>
        /// <returns></returns>
        public Presence[] GetAll(JID jid)
        {
            UserPresenceManager upm = (UserPresenceManager)m_items[jid.Bare];
            if (upm == null)
                return new Presence[0];
            return upm.GetAll();
        }


        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_items.GetEnumerator();
        }

        /// <summary>
        /// Manage the presence for all of the resources of a user.  No locking is performed,
        /// since PresenceManager is already doing locking.
        /// 
        /// The intent of this class is to be able to deliver the last presence stanza 
        /// from the "most available" resource. 
        /// Note that negative priority sessions are never the most available.
        /// </summary>
        private class UserPresenceManager
        {
            private Tree m_items = new Tree();
            private Presence m_pres = null;
            private JID m_jid = null;

            public UserPresenceManager(JID jid)
            {
                Debug.Assert(jid.Resource == null);
                m_jid = jid;
            }

            private void Primary(Presence p, PresenceManager handler)
            {
                Debug.Assert((p == null) ? true : (p.IntPriority >= 0), "Primary presence is always positive priority");
                m_pres = p;
                handler.FireOnPrimarySessionChange(m_jid);
            }

            public void AddPresence(Presence p, PresenceManager handler)
            {
                JID from = p.From;
                string res = from.Resource;
                Debug.Assert(p.Type == PresenceType.available);

                // this is probably a service of some kind.  presumably, there will
                // only ever be one resource.
                if (res == null)
                {
                    if (p.IntPriority >= 0)
                        Primary(p, handler);
                    return;
                }

                // Tree can't overwrite. Have to delete first.
                m_items.Remove(res);
                m_items[res] = p;
    
                // first one is always highest
                if (m_pres == null)
                {
                    if (p.IntPriority >= 0)
                        Primary(p, handler);
                    return;
                }

                if (m_pres.From == p.From)
                {
                    // replacing.  If we're going up, or staying the same, no need to recalc.
                    if (!(p < m_pres))
                    {
                        // can't be negative priority here, since m_pres is always >= 0.
                        Primary(p, handler);
                        return;
                    }
                }

                // Otherwise, recalc
                SetHighest(handler);
            }

            public void RemovePresence(Presence p, PresenceManager handler)
            {
                JID from = p.From;
                string res = from.Resource;
                Debug.Assert(p.Type == PresenceType.unavailable);

                if (res != null)
                    m_items.Remove(res);

                if (m_pres == null)
                    return;

                if (m_pres.From.Resource == res)
                {
                    SetHighest(handler);
                }
            }

            private void SetHighest(PresenceManager handler)
            {
                Presence p = null;
                foreach (DictionaryEntry de in m_items)
                {
                    Presence tp = (Presence)de.Value;
                    if (tp.IntPriority < 0)
                        continue;

                    if (p == null)
                        p = tp;
                    else
                    {
                        if (tp > p)
                            p = tp;
                    }
                }
                Primary(p, handler);
            }

            public int Count
            {
                get
                {
                    if (m_items.Count > 0)
                        return m_items.Count;
                    if (m_pres == null)
                        return 0;
                    return 1;
                }
            }

            public Presence this[string Resource]
            {
                get
                {
                    if (Resource == null)
                        return m_pres;
                    return (Presence) m_items[Resource];
                }
            }

            public Presence[] GetAll()
            {
                Presence[] all; 
                if (m_items.Count > 0)
                    all = new Presence[m_items.Count];
                else if (m_pres == null)
                    return new Presence[0];
                else
                    return new Presence[] {m_pres};

                m_items.CopyTo(all, 0);
                return all;
            }
        }
    }
}
