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
using System.Xml;

using bedrock.util;
using bedrock.collections;

using jabber.protocol;
using jabber.protocol.client;
using jabber.protocol.iq;

namespace jabber.connection
{
    internal class Ident
    {
        public string name;
        public string category;
        public string type;
        public string GetKey()
        {
            string key = "";
            if (category != null)
                key = category;
            if (type != null)
                key = key + "/" + type;
            return key;
        }
    }

    /// <summary>
    /// A JID/Node combination.
    /// </summary>
    [SVN(@"$Id: DiscoManager.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class JIDNode
    {
        private JID m_jid = null;
        private string m_node = null;

        /// <summary>
        /// A JID/Node combination.
        /// </summary>
        /// <param name="jid"></param>
        /// <param name="node"></param>
        public JIDNode(JID jid, string node)
        {
            if (jid == null)
                throw new ArgumentException("JID may not be null", "jid");
            this.m_jid = jid;
            if ((node != null) && (node != ""))
                this.m_node = node;
        }

        /// <summary>
        /// The JID.
        /// </summary>
        [Category("Identity")]
        public JID JID
        {
            get { return m_jid; }
        }

        /// <summary>
        /// The Node.
        /// </summary>
        [Category("Identity")]
        public string Node
        {
            get { return m_node; }
        }

        /// <summary>
        /// Get a hash key that combines the jid and the node.
        /// </summary>
        /// <param name="jid"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        protected static string GetKey(string jid, string node)
        {
            if ((node == null) || (node == ""))
                return jid.ToString();
            return jid + '\u0000' + node;
        }

        /// <summary>
        /// A JID/Node key for Hash lookup.
        /// </summary>
        [Browsable(false)]
        public string Key
        {
            get { return GetKey(m_jid, m_node); }
        }

        /// <summary>
        /// Are we equal to that other jid/node.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            DiscoNode other = obj as DiscoNode;
            if (other == null)
            {
                return false;
            }

            return (m_jid == other.m_jid) && (m_node == other.m_node);
        }

        /// <summary>
        /// Hash the JID and node together, just in case.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            Debug.Assert(m_jid != null);
            int code = m_jid.GetHashCode();
            if (m_node != null)
                code ^= m_node.GetHashCode();
            return code;
        }
    }


    /// <summary>
    /// The info and children of a given JID/Node combination.
    ///
    /// Note: if you have multiple connections in the same process, they all share the same Disco cache.
    /// This works fine in the real world today, since I don't know of any implementations that return different
    /// disco for different requestors, but it is completely legal protocol to have done so.
    /// </summary>
    [SVN(@"$Id: DiscoManager.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoNode : JIDNode, IEnumerable
    {
        private static Tree m_items = new Tree();

        /// <summary>
        /// Children of this node.
        /// </summary>
        public Set Children = null;
        /// <summary>
        /// Features of this node.
        /// </summary>
        public Set Features = null;
        /// <summary>
        /// Identities of this node.
        /// </summary>
        public Set Identity = null;
        private string m_name = null;
        private bool m_pendingItems = false;
        private bool m_pendingInfo = false;
        private jabber.protocol.x.Data m_extensions;

        /// <summary>
        /// Create a disco node.
        /// </summary>
        /// <param name="jid"></param>
        /// <param name="node"></param>
        public DiscoNode(JID jid, string node)
            : base(jid, node)
        {
        }

        /// <summary>
        /// Features are now available
        /// </summary>
        public event DiscoNodeHandler OnFeatures;
        /// <summary>
        /// New children are now available.
        /// </summary>
        public event DiscoNodeHandler OnItems;
        /// <summary>
        /// New identities are available.
        /// </summary>
        public event DiscoNodeHandler OnIdentities;

        /// <summary>
        /// The human-readable string from the first identity.
        /// </summary>
        [Category("Info")]
        public string Name
        {
            set { m_name = value; }
            get
            {
                if (m_name != null)
                    return m_name;
                if (Identity != null)
                {
                    foreach (Ident id in Identity)
                    {
                        if ((id.name != null) && (id.name != ""))
                            m_name = id.name;
                    }
                    return m_name;
                }
                string n = JID;
                if (Node != null)
                    n += "/" + Node;
                return n;
            }
        }

        /// <summary>
        /// Are we waiting on info to be returned?
        /// </summary>
        [Category("Status")]
        public bool PendingInfo
        {
            get { return m_pendingInfo; }
        }

        /// <summary>
        /// Are we waiting on items to be returned?
        /// </summary>
        [Category("Status")]
        public bool PendingItems
        {
            get { return m_pendingItems; }
        }

        /// <summary>
        /// The features associated with this node.
        /// </summary>
        [Category("Info")]
        public string[] FeatureNames
        {
            get
            {
                if (Features == null)
                    return new string[0];
                string[] names = new string[Features.Count];
                int count = 0;
                foreach (string s in Features)
                {
                    names[count++] = s;
                }
                return names;
            }
        }

        /// <summary>
        /// The disco identities of the node.
        /// </summary>
        [Category("Info")]
        public string[] Identities
        {
            get
            {
                if (Identity == null)
                    return new string[0];
                string[] names = new string[Identity.Count];
                int count = 0;
                foreach (Ident i in Identity)
                {
                    names[count++] = i.GetKey();
                }
                return names;
            }
        }

        /// <summary>
        /// The x:data extensions of the disco information.
        /// </summary>
        public jabber.protocol.x.Data Extensions
        {
            get
            {
                return m_extensions;
            }
            set
            {
                m_extensions = value;
            }
        }

        /// <summary>
        /// Factory to create nodes and ensure that they are cached
        /// </summary>
        /// <param name="jid"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static DiscoNode GetNode(JID jid, string node)
        {
            string key = GetKey(jid, node);
            DiscoNode n = (DiscoNode)m_items[key];
            if (n == null)
            {
                n = new DiscoNode(jid, node);
                m_items.Add(key, n);
            }
            return n;
        }

        /// <summary>
        /// Factory to create nodes, where the node is null, and only the JID is specified.
        /// </summary>
        /// <param name="jid"></param>
        /// <returns></returns>
        public static DiscoNode GetNode(JID jid)
        {
            return GetNode(jid, null);
        }

        /// <summary>
        /// Delete the cache.
        /// </summary>
        public static void Clear()
        {
            m_items.Clear();
        }

        /// <summary>
        /// Does this node have the specified feature?
        /// </summary>
        /// <param name="URI"></param>
        /// <returns></returns>
        public bool HasFeature(string URI)
        {
            if (Features == null)
                return false;
            return Features.Contains(URI);
        }

        /// <summary>
        /// Add these features to the node. Fires OnFeatures.
        /// </summary>
        /// <param name="features"></param>
        public void AddFeatures(DiscoFeature[] features)
        {
            if (Features == null)
                Features = new Set();
            if (features != null)
            {
                foreach (DiscoFeature f in features)
                    Features.Add(f.Var);
            }
            if (OnFeatures != null)
            {
                OnFeatures(this);
                OnFeatures = null;
            }
        }

        /// <summary>
        /// Add these identities to the node.
        /// </summary>
        /// <param name="ids"></param>
        public void AddIdentities(DiscoIdentity[] ids)
        {
            if (Identity == null)
                Identity = new Set();
            if (ids != null)
            {
                foreach (DiscoIdentity id in ids)
                {
                    Ident i = new Ident();
                    i.name = id.Named;
                    i.category = id.Category;
                    i.type = id.Type;
                    Identity.Add(i);
                }
            }
        }

        internal DiscoNode AddItem(DiscoItem di)
        {
            DiscoNode dn = GetNode(di.Jid, di.Node);
            if ((di.Named != null) && (di.Named != ""))
                dn.Name = di.Named;
            Children.Add(dn);
            return dn;
        }

        /// <summary>
        /// Add the given items to the cache.
        /// </summary>
        /// <param name="items"></param>
        public void AddItems(DiscoItem[] items)
        {
            if (Children == null)
                Children = new Set();
            if (items != null)
            {
                foreach (DiscoItem di in items)
                    AddItem(di);
            }
            if (OnItems != null)
            {
                OnItems(this);
                OnItems = null;
            }
        }

        /// <summary>
        /// Create a disco#info IQ.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IQ InfoIQ(System.Xml.XmlDocument doc)
        {
            m_pendingInfo = true;
            DiscoInfoIQ iiq = new DiscoInfoIQ(doc);
            iiq.To = JID;
            iiq.Type = IQType.get;
            if (Node != null)
            {
                DiscoInfo info = (DiscoInfo)iiq.Query;
                info.Node = Node;
            }

            return iiq;
        }

        /// <summary>
        /// Create a disco#items IQ.
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public IQ ItemsIQ(System.Xml.XmlDocument doc)
        {
            m_pendingItems = true;

            DiscoItemsIQ iiq = new DiscoItemsIQ(doc);
            iiq.To = JID;
            iiq.Type = IQType.get;
            if (Node != null)
            {
                DiscoItems items = (DiscoItems)iiq.Query;
                items.Node = Node;
            }
            return iiq;
        }

        /// <summary>
        /// Get all items.
        /// </summary>
        /// <returns></returns>
        public static IEnumerator EnumerateAll()
        {
            return m_items.GetEnumerator();
        }

        #region IEnumerable Members

        /// <summary>
        /// Get an enumerator across all items.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return Children.GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// Callback with a new disco node.
    /// </summary>
    /// <param name="node"></param>
    public delegate void DiscoNodeHandler(DiscoNode node);

    /// <summary>
    /// Disco database.
    /// TODO: once etags are finished, make all of this information cached on disk.
    /// TODO: cache XEP-115 client caps data to disk
    /// TODO: add negative caching
    /// </summary>
    [SVN(@"$Id: DiscoManager.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoManager : System.ComponentModel.Component, IEnumerable
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private XmppStream m_stream = null;
        private DiscoNode m_root = null;

        /// <summary>
        /// Construct a PresenceManager object.
        /// </summary>
        /// <param name="container"></param>
        public DiscoManager(System.ComponentModel.IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        /// <summary>
        /// Construct a PresenceManager object.
        /// </summary>
        public DiscoManager()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The JabberClient to hook up to.
        /// </summary>
        [Description("The JabberClient or JabberService to hook up to.")]
        [Category("Jabber")]
        public virtual XmppStream Stream
        {
            get
            {
                // If we are running in the designer, let's try to get an invoke control
                // from the environment.  VB programmers can't seem to follow directions.
                if ((this.m_stream == null) && DesignMode)
                {
                    IDesignerHost host = (IDesignerHost)base.GetService(typeof(IDesignerHost));
                    if (host != null)
                    {
                        Component root = host.RootComponent as Component;
                        if (root != null)
                        {
                            foreach (Component c in root.Container.Components)
                            {
                                if (c is XmppStream)
                                {
                                    m_stream = (XmppStream)c;
                                    break;
                                }
                            }
                        }
                    }
                }
                return m_stream;
            }
            set
            {
                m_stream = value;
                m_stream.OnDisconnect += new bedrock.ObjectHandler(GotDisconnect);
                if (m_stream is jabber.client.JabberClient)
                {
                    m_stream.OnAuthenticate += new bedrock.ObjectHandler(m_client_OnAuthenticate);
                }
            }
        }

        /// <summary>
        /// The root node.  This is probably the server that you connected to.
        /// If the Children property of this is null, we haven't received an answer to
        /// our disco#items request; register on this node's OnFeatures callback.
        /// </summary>
        public DiscoNode Root
        {
            get { return m_root; }
        }

        private void m_client_OnAuthenticate(object sender)
        {
            m_root = DiscoNode.GetNode(m_stream.Server);
            RequestInfo(m_root);
        }

        private void GotDisconnect(object sender)
        {
            m_root = null;
            DiscoNode.Clear();
        }

        private void RequestInfo(DiscoNode node)
        {
            if (!node.PendingInfo)
            {
                IQ iq = node.InfoIQ(m_stream.Document);
                jabber.server.JabberService js = m_stream as jabber.server.JabberService;
                if (js != null)
                    iq.From = js.ComponentID;
                m_stream.Tracker.BeginIQ(iq,
                                         new jabber.connection.IqCB(GotInfo),
                                         node);
            }
        }

        private void RequestItems(DiscoNode node)
        {
            if (!node.PendingItems)
            {
                IQ iq = node.ItemsIQ(m_stream.Document);
                jabber.server.JabberService js = m_stream as jabber.server.JabberService;
                if (js != null)
                    iq.From = js.ComponentID;
                m_stream.Tracker.BeginIQ(iq,
                                         new jabber.connection.IqCB(GotItems),
                                         node);
            }
        }


        private void GotInfo(object sender, IQ iq, object onode)
        {
            DiscoNode dn = onode as DiscoNode;
            Debug.Assert(dn != null);

            if (iq.Type == IQType.error)
            {
                if (dn == m_root)
                {
                    // root node.
                    // Try agents.
                    if ((iq.Error.Code == ErrorCode.NOT_IMPLEMENTED) ||
                        (iq.Error.Code == ErrorCode.SERVICE_UNAVAILABLE))
                    {
                        IQ aiq = new AgentsIQ(m_stream.Document);
                        m_stream.Tracker.BeginIQ(aiq, new jabber.connection.IqCB(GotAgents), m_root);
                        return;
                    }
                }
            }
            if (iq.Type != IQType.result)
            {
                // protocol error
                dn.AddIdentities(null);
                dn.AddFeatures(null);
                return;
            }

            DiscoInfo info = iq.Query as DiscoInfo;
            if (info == null)
            {
                // protocol error
                dn.AddIdentities(null);
                dn.AddFeatures(null);
                return;
            }

            jabber.protocol.x.Data ext = info["x", URI.XDATA] as jabber.protocol.x.Data;
            if (ext != null)
                dn.Extensions = ext;

            dn.AddIdentities(info.GetIdentities());
            dn.AddFeatures(info.GetFeatures());

            if (dn == m_root)
                RequestItems(m_root);
        }

        private void GotItems(object sender, IQ iq, object onode)
        {
            DiscoNode dn = onode as DiscoNode;
            Debug.Assert(dn != null);

            if (iq.Type != IQType.result)
            {
                // protocol error
                dn.AddItems(null);
                return;
            }

            DiscoItems items = iq.Query as DiscoItems;
            if (items == null)
            {
                // protocol error
                dn.AddItems(null);
                return;
            }

            dn.AddItems(items.GetItems());

            // automatically info everything we get an item for.
            foreach (DiscoNode n in dn.Children)
            {
                if (n.Features == null)
                {
                    RequestInfo(n);
                }
            }
        }

        private void GotAgents(object sender, IQ iq, object onode)
        {
            DiscoNode dn = onode as DiscoNode;
            Debug.Assert(dn != null);

            if (iq.Type != IQType.result)
            {
                dn.AddItems(null);
                return;
            }

            AgentsQuery aq = iq.Query as AgentsQuery;
            if (aq == null)
            {
                dn.AddItems(null);
                return;
            }

            if (dn.Children == null)
                dn.Children = new Set();

            foreach (Agent agent in aq.GetAgents())
            {
                DiscoItem di = new DiscoItem(m_stream.Document);
                di.Jid = agent.JID;
                di.Named = agent.AgentName;

                DiscoNode child = dn.AddItem(di);
                if (child.Features == null)
                    child.Features = new Set();
                if (child.Identity == null)
                    child.Identity = new Set();

                Ident id = new Ident();
                id.name = agent.Description;
                switch (agent.Service)
                {
                case "groupchat":
                    id.category = "conference";
                    id.type = "text";
                    child.Identity.Add(id);
                    break;
                case "jud":
                    id.category = "directory";
                    id.type = "user";
                    child.Identity.Add(id);
                    break;
                case null:
                case "":
                    break;
                default:
                    // guess this is a transport
                    id.category = "gateway";
                    id.type = agent.Service;
                    child.Identity.Add(id);
                    break;
                }

                if (agent.Register)
                    child.Features.Add(URI.REGISTER);
                if (agent.Search)
                    child.Features.Add(URI.SEARCH);
                if (agent.Groupchat)
                    child.Features.Add(URI.MUC);
                if (agent.Transport)
                {
                    if (id.category != "gateway")
                    {
                        Ident tid = new Ident();
                        tid.name = id.name;
                        tid.category = "gateway";
                        child.Identity.Add(tid);
                    }
                }

                foreach (XmlElement ns in agent.GetElementsByTagName("ns"))
                {
                    child.Features.Add(ns.InnerText);
                }
                child.AddItems(null);
                child.AddIdentities(null);
                child.AddFeatures(null);
            }
            dn.AddItems(null);
            dn.AddIdentities(null);
            dn.AddFeatures(null);
        }

        /// <summary>
        /// Make a call to get the feaures to this node, and call back on handler.
        /// If the information is in the cache, handler gets called right now.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="handler"></param>
        public void BeginGetFeatures(DiscoNode node, DiscoNodeHandler handler)
        {
            if (node.Features != null)
            {
                if (handler != null)
                    handler(node);
            }
            else
            {
                if (handler != null)
                    node.OnFeatures += handler;
                RequestInfo(node);
            }
        }

        /// <summary>
        /// Make a call to get the feaures to this node, and call back on handler.
        /// If the information is in the cache, handler gets called right now.
        /// </summary>
        /// <param name="jid"></param>
        /// <param name="node"></param>
        /// <param name="handler"></param>
        public void BeginGetFeatures(JID jid, string node, DiscoNodeHandler handler)
        {
            BeginGetFeatures(DiscoNode.GetNode(jid, node), handler);
        }

        /// <summary>
        /// Make a call to get the child items of this node, and call back on handler.
        /// If the information is in the cache, handler gets called right now.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="handler"></param>
        public void BeginGetItems(DiscoNode node, DiscoNodeHandler handler)
        {
            if (node.Children != null)
            {
                if (handler != null)
                    handler(node);
            }
            else
            {
                if (handler != null)
                    node.OnItems += handler;
                RequestItems(node);
            }
        }

        /// <summary>
        /// Make a call to get the child items of this node, and call back on handler.
        /// If the information is in the cache, handler gets called right now.
        /// </summary>
        /// <param name="jid"></param>
        /// <param name="node"></param>
        /// <param name="handler"></param>
        public void BeginGetItems(JID jid, string node, DiscoNodeHandler handler)
        {
            BeginGetItems(DiscoNode.GetNode(jid, node), handler);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return DiscoNode.EnumerateAll();
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
    }
}
