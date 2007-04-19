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

using System.Diagnostics;
using System.Security.Cryptography;
using System.Xml;

using bedrock.util;

namespace jabber.protocol.iq
{
    /// <summary>
    /// A GeoLoc IQ.
    /// </summary>
    [SVN(@"$Id: GeoLoc.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class GeoLocIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a GeoLoc IQ.
        /// </summary>
        /// <param name="doc"></param>
        public GeoLocIQ(XmlDocument doc) : base(doc)
        {
            this.AppendChild(new GeoLocIQ(doc));
        }
    }

    /// <summary>
    /// Geographic location.  See http://www.xmpp.org/extensions/xep-0080.html.
    /// </summary>
    [SVN(@"$Id: GeoLoc.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class GeoLoc : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public GeoLoc(XmlDocument doc) :
            base("geoloc", URI.GEOLOC, doc)
        {
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public GeoLoc(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        ///  Altitude above/below sea level, in meters.
        /// </summary>
        public double Altitude

        {
            get { return double.Parse(GetElem("alt")); }
            set { SetElem("alt", value.ToString()); }
        }

        /// <summary>
        /// Assuming decimal degrees to true north.
        /// Note: this is being further specified in the XEP.
        /// </summary>
        public double Bearing
        {
            get { return double.Parse(GetElem("bearing")); }
            set { SetElem("bearing", value.ToString()); }
        }

        /// <summary>
        /// GPS datum, defaults to WGS84.
        /// </summary>
        public string Datum
        {
            get
            {
                string datum = GetElem("datum");
                if ((datum == null) || (datum == ""))
                    datum = "WGS84";
                return datum;
            }
            set { SetElem("datum", value); }
        }

        /// <summary>
        /// A natural-language description of the location.
        /// </summary>
        public string Description
        {
            get { return GetElem("description"); }
            set { SetElem("description", value); }
        }

        /// <summary>
        /// Horizontal GPS error in arc minutes.
        /// </summary>
        public double Error
        {
            get { return double.Parse(GetElem("error")); }
            set { SetElem("error", value.ToString()); }
        }

        /// <summary>
        /// Latitude in decimal degrees North.
        /// </summary>
        public double Latitude
        {
            get { return double.Parse(GetElem("lat")); }
            set { SetElem("lat", value.ToString()); }
        }

        /// <summary>
        /// Longitude in decimal degrees East.
        /// </summary>
        public double Longitude
        {
            get { return double.Parse(GetElem("lon")); }
            set { SetElem("lon", value.ToString()); }
        }

        /// <summary>
        /// UTC timestamp specifying the moment when the reading was taken.
        /// </summary>
        public DateTime Timestamp
        {
            get { return DateTimeProfile(GetElem("timestamp")); }
            set { SetElem("timestamp", DateTimeProfile(value)); }
        }

    }
}
