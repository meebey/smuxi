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
    /// <summary>
    /// IQ packet with a roster query element inside.
    /// </summary>
    [SVN(@"$Id: Roster.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class RosterIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a roster IQ.
        /// </summary>
        /// <param name="doc"></param>
        public RosterIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new Roster(doc);
        }
    }

    /// <summary>
    /// A roster query element.
    /// </summary>
    [SVN(@"$Id: Roster.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Roster : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Roster(XmlDocument doc) : base("query", URI.ROSTER, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Roster(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Add a roster item
        /// </summary>
        /// <returns></returns>
        public Item AddItem()
        {
            Item i = new Item(this.OwnerDocument);
            AddChild(i);
            return i;
        }

        /// <summary>
        /// List of roster items
        /// </summary>
        /// <returns></returns>
        public Item[] GetItems()
        {
            XmlNodeList nl = GetElementsByTagName("item", URI.ROSTER);
            Item[] items = new Item[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                items[i] = (Item) n;
                i++;
            }
            return items;
        }
    }

    /// <summary>
    /// The current status of the subscription related to this item.
    /// </summary>
    [SVN(@"$Id: Roster.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum Subscription
    {
        /// <summary>
        /// Subscription to this person.  They are a lurkee.
        /// </summary>
        to,
        /// <summary>
        /// Subscription from this person.  They are a lurker.
        /// </summary>
        from,
        /// <summary>
        /// subscriptions in both ways.
        /// </summary>
        both,
        /// <summary>
        /// No subscription yet.  Often an Ask on this item.
        /// </summary>
        none,
        /// <summary>
        /// Remove this subscription from the local roster.
        /// </summary>
        remove,
    }

    /// <summary>
    /// An optional attribute specifying the current status of a request to this contact.
    /// </summary>
    [SVN(@"$Id: Roster.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public enum Ask
    {
        /// <summary>
        /// No Ask specified.
        /// </summary>
        NONE = -1,
        /// <summary>
        /// this entity is asking to subscribe to that contact's presence
        /// </summary>
        subscribe,
        /// <summary>
        /// this entity is asking unsubscribe from that contact's presence
        /// </summary>
        unsubscribe
    }

    /// <summary>
    /// Roster items.
    /// </summary>
    [SVN(@"$Id: Roster.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Item : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Item(XmlDocument doc) : base("item", URI.ROSTER, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Item(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Item JID
        /// </summary>
        public JID JID
        {
            get { return new JID(GetAttribute("jid")); }
            set { this.SetAttribute("jid", value.ToString()); }
        }

        /// <summary>
        /// The user's nick
        /// </summary>
        public string Nickname
        {
            get { return GetAttribute("name"); }
            set { SetAttribute("name", value); }
        }

        /// <summary>
        /// How are we subscribed?
        /// </summary>
        public Subscription Subscription
        {
            get { return (Subscription) GetEnumAttr("subscription", typeof(Subscription)); }
            set { SetAttribute("subscription", value.ToString()); }
        }

        /// <summary>
        /// Pending?
        /// </summary>
        public Ask Ask
        {
            get { return (Ask) GetEnumAttr("ask", typeof(Ask)); }
            set
            {
                if (value == Ask.NONE)
                    RemoveAttribute("ask");
                else
                    SetAttribute("ask", value.ToString());
            }
        }

        /// <summary>
        /// Add an item group
        /// </summary>
        /// <returns></returns>
        public Group AddGroup(string name)
        {
            Group g = GetGroup(name);
            if (g == null)
            {
                g = new Group(this.OwnerDocument);
                g.GroupName = name;
                AddChild(g);
            }
            return g;
        }

        /// <summary>
        /// Remove a group of the given name.  Does nothing if that group is not found.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveGroup(string name)
        {
            XmlNodeList nl = GetElementsByTagName("group", URI.ROSTER);
            foreach (Group g in nl)
            {
                if (g.GroupName == name)
                {
                    this.RemoveChild(g);
                    return;
                }
            }
        }

        /// <summary>
        /// List of item groups
        /// </summary>
        /// <returns></returns>
        public Group[] GetGroups()
        {
            XmlNodeList nl = GetElementsByTagName("group", URI.ROSTER);
            Group[] groups = new Group[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                groups[i] = (Group) n;
                i++;
            }
            return groups;
        }

        /// <summary>
        /// Is this item in the specified group?
        /// </summary>
        /// <param name="name">The name of the group to check</param>
        /// <returns></returns>
        public bool HasGroup(string name)
        {
            Group[] gl = GetGroups();
            foreach (Group g in gl)
            {
                if (g.GroupName == name)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the group object of the given name in this item.
        /// If there is no group of that name, returns null.
        /// </summary>
        /// <param name="name">The name of the group to return</param>
        /// <returns>null if none found.</returns>
        public Group GetGroup(string name)
        {
            Group[] gl = GetGroups();
            foreach (Group g in gl)
            {
                if (g.GroupName == name)
                    return g;
            }
            return null;
        }
    }
    /// <summary>
    /// Roster item groups.  &lt;group&gt;GroupName&lt;/group&gt;
    /// </summary>
    [SVN(@"$Id: Roster.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Group : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Group(XmlDocument doc) : base("group", URI.ROSTER, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Group(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Name of the group.
        /// </summary>
        public string GroupName
        {
            get { return this.InnerText; }
            set { this.InnerText = value; }
        }
    }
}
