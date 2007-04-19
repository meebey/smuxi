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
using System.Xml;

using bedrock.util;

namespace jabber.protocol.iq
{
    /*
     * <iq
     *     type='result'
     *     from='shakespeare.lit'
     *     to='romeo@montague.net/orchard'
     *     id='items1'>
     *   <query xmlns='http://jabber.org/protocol/disco#items' node='music'>
     *     <item
     *         jid='people.shakespeare.lit'
     *         name='Directory of Characters'/>
     *     <item
     *         jid='plays.shakespeare.lit'
     *         name='Play-Specific Chatrooms'/>
     * </iq>
     */
    /// <summary>
    /// IQ packet with a disco#items query element inside.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoItemsIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a disco#items IQ
        /// </summary>
        /// <param name="doc"></param>
        public DiscoItemsIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new DiscoItems(doc);
        }

        /// <summary>
        /// The node on the query.
        /// </summary>
        public string Node
        {
            get
            {
                return ((DiscoItems)this.Query).Node;
            }
            set
            {
                ((DiscoItems)this.Query).Node = value;
            }
        }
    }

    /// <summary>
    /// IQ packet with a disco#info query element inside.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoInfoIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a disco#items IQ
        /// </summary>
        /// <param name="doc"></param>
        public DiscoInfoIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new DiscoInfo(doc);
        }

        /// <summary>
        /// The node on the query.
        /// </summary>
        public string Node
        {
            get
            {
                return ((DiscoInfo)this.Query).Node;
            }
            set
            {
                ((DiscoInfo)this.Query).Node = value;
            }
        }
    }

    /*
     * <iq
     *     type='result'
     *     from='plays.shakespeare.lit'
     *     to='romeo@montague.net/orchard'
     *     id='info1'>
     *   <query xmlns='http://jabber.org/protocol/disco#info'>
     *     <identity
     *         category='conference'
     *         type='text'
     *         name='Play-Specific Chatrooms'/>
     *     <identity
     *         category='directory'
     *         type='room'
     *         name='Play-Specific Chatrooms'/>
     *     <feature var='gc-1.0'/>
     *     <feature var='http://jabber.org/protocol/muc'/>
     *     <feature var='jabber:iq:register'/>
     *     <feature var='jabber:iq:search'/>
     *     <feature var='jabber:iq:time'/>
     *     <feature var='jabber:iq:version'/>
     *   </query>
     * </iq>
     */


    /// <summary>
    /// A disco#items query element.
    /// See <a href="http://www.xmpp.org/extensions/xep-0030.html">XEP-0030</a> for more information.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoItems : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public DiscoItems(XmlDocument doc) : base("query", URI.DISCO_ITEMS, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public DiscoItems(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The sub-address of the discovered entity.
        /// </summary>
        public string Node
        {
            get { return GetAttribute("node"); }
            set { SetAttribute("node", value); }
        }

        /// <summary>
        /// Add a disco item
        /// </summary>
        /// <returns></returns>
        public DiscoItem AddItem()
        {
            DiscoItem i = new DiscoItem(this.OwnerDocument);
            AddChild(i);
            return i;
        }

        /// <summary>
        /// List of disco items
        /// </summary>
        /// <returns></returns>
        public DiscoItem[] GetItems()
        {
            XmlNodeList nl = GetElementsByTagName("item", URI.DISCO_ITEMS);
            DiscoItem[] items = new DiscoItem[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                items[i] = (DiscoItem) n;
                i++;
            }
            return items;
        }
    }

    /// <summary>
    /// Actions for iq/set in the disco#items namespace.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public enum DiscoAction
    {
        /// <summary>
        /// None specified
        /// </summary>
        NONE = -1,
        /// <summary>
        /// Remove this item
        /// </summary>
        remove,
        /// <summary>
        /// Update this item
        /// </summary>
        update
    }

    /// <summary>
    /// An item inside a disco#items result.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoItem : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public DiscoItem(XmlDocument doc) : base("item", URI.DISCO_ITEMS, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public DiscoItem(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The Jabber ID associated with the item.
        /// </summary>
        public JID Jid
        {
            get { return GetAttribute("jid"); }
            set { SetAttribute("jid", value); }
        }

        /// <summary>
        /// The user-visible name of this node
        /// </summary>
        public string Named
        {
            get { return GetAttribute("name"); }
            set { SetAttribute("name", value); }
        }

        /// <summary>
        /// The sub-node associated with this item.
        /// </summary>
        public string Node
        {
            get { return GetAttribute("node"); }
            set { SetAttribute("node", value); }
        }

        /// <summary>
        /// Actions for iq/set in the disco#items namespace.
        /// </summary>
        public DiscoAction Action
        {
            get { return (DiscoAction) GetEnumAttr("action", typeof(DiscoAction)); }
            set
            {
                if (value == DiscoAction.NONE)
                    RemoveAttribute("action");
                else
                    SetAttribute("action", value.ToString());
            }
        }
    }

/*
<iq
    type='result'
    from='balconyscene@plays.shakespeare.lit'
    to='juliet@capulet.com/balcony'
    id='info2'>
  <query xmlns='http://jabber.org/protocol/disco#info'>
    <identity
        category='conference'
        type='text'
        name='Romeo and Juliet, Act II, Scene II'/>
    <feature var='gc-1.0'/>
    <feature var='http://jabber.org/protocol/muc'/>
    <feature var='http://jabber.org/protocol/feature-neg'/>
    <feature var='muc-password'/>
    <feature var='muc-hidden'/>
    <feature var='muc-temporary'/>
    <feature var='muc-open'/>
    <feature var='muc-unmoderated'/>
    <feature var='muc-nonanonymous'/>
  </query>
</iq>
*/
    /// <summary>
    /// The information associated with a disco node.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoInfo : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public DiscoInfo(XmlDocument doc) : base("query", URI.DISCO_INFO, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public DiscoInfo(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The sub-node associated with this item.
        /// </summary>
        public string Node
        {
            get { return GetAttribute("node"); }
            set { SetAttribute("node", value); }
        }
        /// <summary>
        /// Add an identity
        /// </summary>
        /// <returns></returns>
        public DiscoIdentity AddIdentity(string category, string discoType, string name)
        {
            DiscoIdentity i = new DiscoIdentity(this.OwnerDocument);
            AddChild(i);
            i.Category = category;
            i.Type = discoType;
            i.Named = name;
            return i;
        }

        /// <summary>
        /// List of identities
        /// </summary>
        /// <returns></returns>
        public DiscoIdentity[] GetIdentities()
        {
            XmlNodeList nl = GetElementsByTagName("identity", URI.DISCO_INFO);
            DiscoIdentity[] items = new DiscoIdentity[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                items[i] = (DiscoIdentity) n;
                i++;
            }
            return items;
        }

        /// <summary>
        /// Add a feature
        /// </summary>
        /// <returns></returns>
        public DiscoFeature AddFeature(string featureURI)
        {
            DiscoFeature i = new DiscoFeature(this.OwnerDocument);
            AddChild(i);
            i.Var = featureURI;
            return i;
        }

        /// <summary>
        /// List of features
        /// </summary>
        /// <returns></returns>
        public DiscoFeature[] GetFeatures()
        {
            XmlNodeList nl = GetElementsByTagName("feature", URI.DISCO_INFO);
            DiscoFeature[] items = new DiscoFeature[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                items[i] = (DiscoFeature) n;
                i++;
            }
            return items;
        }

        /// <summary>
        /// Is the given feature URI supported by this entity?
        /// </summary>
        /// <param name="featureURI">The URI to check</param>
        /// <returns></returns>
        public bool HasFeature(string featureURI)
        {
            XmlNodeList nl = GetElementsByTagName("feature", URI.DISCO_INFO);
            foreach (DiscoFeature feat in nl)
            {
                if (feat.Var == featureURI)
                    return true;
            }
            return false;
        }
    }

    /// <summary>
    /// The identitiy associated with a disco node.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoIdentity : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public DiscoIdentity(XmlDocument doc) : base("identity", URI.DISCO_INFO, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public DiscoIdentity(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The user-visible name of this node
        /// </summary>
        public string Named
        {
            get { return GetAttribute("name"); }
            set { SetAttribute("name", value); }
        }

        /// <summary>
        /// The category of the node
        /// </summary>
        public string Category
        {
            get { return GetAttribute("category"); }
            set { SetAttribute("category", value); }
        }

        /// <summary>
        /// The type of the node
        /// </summary>
        public string Type
        {
            get { return GetAttribute("type"); }
            set { SetAttribute("type", value); }
        }

    }

    /// <summary>
    /// A feature associated with a disco node.
    /// </summary>
    [SVN(@"$Id: Disco.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class DiscoFeature : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public DiscoFeature(XmlDocument doc) : base("feature", URI.DISCO_ITEMS, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public DiscoFeature(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The namespace name or feature name.
        /// </summary>
        public string Var
        {
            get { return GetAttribute("var"); }
            set { SetAttribute("var", value); }
        }
    }
}
