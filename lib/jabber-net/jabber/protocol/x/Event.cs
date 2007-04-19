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

namespace jabber.protocol.x
{
    /// <summary>
    /// Types of events
    /// </summary>
    [Flags]
    [SVN(@"$Id: Event.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public enum EventType
    {
        /// <summary>
        /// No event type specified.
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Indicates that the message has been stored offline by the server, because the
        /// intended recipient is not available. This event is to be raised by the Jabber server.
        /// </summary>
        offline = 1,
        /// <summary>
        /// Indicates that the message has been delivered to the recipient. This signifies
        /// that the message has reached the Jabber client, but does not necessarily mean
        /// that the message has been displayed. This event is to be raised by the Jabber client.
        /// </summary>
        delivered = 2,
        /// <summary>
        /// Once the message has been received by the Jabber client, it may be displayed
        /// to the user. This event indicates that the message has been displayed, and is
        /// to be raised by the Jabber client. Even if a message is displayed multiple times,
        /// this event should only be raised once.
        /// </summary>
        displayed = 4,
        /// <summary>
        /// In threaded chat conversations, this indicates that the recipient is composing
        /// a reply to a message that was just sent. The event is to be raised by the Jabber
        /// client. A Jabber client is allowed to raise this event multiple times in response
        /// to the same request, providing that a specific sequence is followed.
        /// </summary>
        composing = 8
    }

    /// <summary>
    /// A event x element, described by XEP-0022.
    /// </summary>
    [SVN(@"$Id: Event.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class Event : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Event(XmlDocument doc) : base("x", URI.XEVENT, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Event(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// The message to which this event refers.
        /// </summary>
        public string ID
        {
            get { return GetElem("id"); }
            set { SetElem("id", value); }
        }

        /// <summary>
        /// The type of the event.
        /// </summary>
        public EventType Type
        {
            get
            {
                EventType res = EventType.NONE;
                if (IsOffline) res |= EventType.offline;
                if (IsDelivered) res |= EventType.delivered;
                if (IsDisplayed) res |= EventType.displayed;
                if (IsComposing) res |= EventType.composing;
                return res;
            }
            set
            {
                IsOffline = ((value & EventType.offline) == EventType.offline);
                IsDelivered = ((value & EventType.delivered) == EventType.delivered);
                IsDisplayed = ((value & EventType.displayed) == EventType.displayed);
                IsComposing = ((value & EventType.composing) == EventType.composing);
            }
        }

        /// <summary>
        /// Indicates that the message has been stored offline by the server, because the
        /// intended recipient is not available. This event is to be raised by the Jabber server.
        /// </summary>
        public bool IsOffline
        {
            get { return this["offline"] != null; }
            set
            {
                if (value)
                    this.SetElem("offline", null);
                else
                    this.RemoveElem("offline");
            }
        }
        /// <summary>
        /// Indicates that the message has been delivered to the recipient. This signifies
        /// that the message has reached the Jabber client, but does not necessarily mean
        /// that the message has been displayed. This event is to be raised by the Jabber client.
        /// </summary>
        public bool IsDelivered
        {
            get { return this["delivered"] != null; }
            set
            {
                if (value)
                    this.SetElem("delivered", null);
                else
                    this.RemoveElem("delivered");
            }
        }
        /// <summary>
        /// Once the message has been received by the Jabber client, it may be displayed
        /// to the user. This event indicates that the message has been displayed, and is
        /// to be raised by the Jabber client. Even if a message is displayed multiple times,
        /// this event should only be raised once.
        /// </summary>
        public bool IsDisplayed
        {
            get { return this["displayed"] != null; }
            set
            {
                if (value)
                    this.SetElem("displayed", null);
                else
                    this.RemoveElem("displayed");
            }
        }
        /// <summary>
        /// In threaded chat conversations, this indicates that the recipient is composing
        /// a reply to a message that was just sent. The event is to be raised by the Jabber
        /// client. A Jabber client is allowed to raise this event multiple times in response
        /// to the same request, providing that a specific sequence is followed.
        /// </summary>
        public bool IsComposing
        {
            get { return this["composing"] != null; }
            set
            {
                if (value)
                    this.SetElem("composing", null);
                else
                    this.RemoveElem("composing");
            }
        }
    }
}
