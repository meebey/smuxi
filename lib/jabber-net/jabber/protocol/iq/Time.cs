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
     * <iq type="result" to="romeo@montague.net/orchard"
     *                   from="juliet@capulet.com/balcony"
     *                   id="i_time_001">
     *   <query xmlns="jabber:iq:time">
     *     <utc>20020214T23:55:06</utc>
     *     <tz>WET</tz>
     *     <display>14 Feb 2002 11:55:06 PM</display>
     *   </query>
     * </iq>
     */
    /// <summary>
    /// IQ packet with an time query element inside.
    /// </summary>
    [SVN(@"$Id: Time.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class TimeIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a time IQ
        /// </summary>
        /// <param name="doc"></param>
        public TimeIQ(XmlDocument doc) : base(doc)
        {
            this.Query = new Time(doc);
        }
    }

    /// <summary>
    /// A time query element.
    /// </summary>
    [SVN(@"$Id: Time.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Time : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public Time(XmlDocument doc) : base("query", URI.TIME, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public Time(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Set the current UTC, TZ, and Display based on the machine's current settings/locale.
        /// </summary>
        public void SetCurrentTime()
        {
            DateTime dt = DateTime.Now;
            UTC = dt.ToUniversalTime();
            TZ = TimeZone.CurrentTimeZone.IsDaylightSavingTime(dt) ?
                TimeZone.CurrentTimeZone.DaylightName : TimeZone.CurrentTimeZone.StandardName;
            Display = dt.ToLongDateString() + " " + dt.ToLongTimeString();
        }

        /// <summary>
        /// Universal coordinated time.  (More or less GMT).
        /// </summary>
        public DateTime UTC
        {
            get { return JabberDate(GetElem("utc")); }
            set { SetElem("utc", JabberDate(value)); }
        }

        /// <summary>
        /// Timezone
        /// </summary>
        //TODO: return System.TimeZone?
        public string TZ
        {
            get { return GetElem("tz"); }
            set { SetElem("tz", value); }
        }

        /// <summary>
        /// Human-readable date/time.
        /// </summary>
        public string Display
        {
            get { return GetElem("display"); }
            set { SetElem("display", value); }
        }
    }
}
