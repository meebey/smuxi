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

// http://www.xmpp.org/extensions/xep-0054.html

namespace jabber.protocol.iq
{
    /// <summary>
    /// Type of telephone number.
    /// </summary>
    public enum TelephoneType
    {
        /// <summary>
        /// None specified
        /// </summary>
        unknown = -1,
        /// <summary>
        /// voice
        /// </summary>
        voice,
        /// <summary>
        /// fax
        /// </summary>
        fax,
        /// <summary>
        /// pager
        /// </summary>
        pager,
        /// <summary>
        /// voice mail
        /// </summary>
        msg,
        /// <summary>
        /// mobile
        /// </summary>
        cell,
        /// <summary>
        /// video phone
        /// </summary>
        video,
        /// <summary>
        /// Bulletin Board System
        /// </summary>
        bbs,
        /// <summary>
        /// Modem
        /// </summary>
        modem,
        /// <summary>
        /// ISDN
        /// </summary>
        isdn,
        /// <summary>
        /// dunno.
        /// </summary>
        pcs
    };

    /// <summary>
    /// Telephone location
    /// </summary>
    public enum TelephoneLocation
    {
        /// <summary>
        /// Home
        /// </summary>
        home,
        /// <summary>
        /// Work
        /// </summary>
        work,
        /// <summary>
        /// Unknown
        /// </summary>
        unknown
    }

    /// <summary>
    /// Address location
    /// </summary>
    public enum AddressLocation
    {
        /// <summary>
        /// Home
        /// </summary>
        home,
        /// <summary>
        /// Work
        /// </summary>
        work,
        /// <summary>
        /// Unknown
        /// </summary>
        unknown
    }

    /// <summary>
    /// Email type attribute
    /// </summary>
    public enum EmailType
    {
        /// <summary>
        /// None specified
        /// </summary>
        NONE = -1,
        /// <summary>
        /// Home
        /// </summary>
        home,
        /// <summary>
        /// Work
        /// </summary>
        work,
        /// <summary>
        /// Internet
        /// </summary>
        internet,
        /// <summary>
        /// x400
        /// </summary>
        x400
    }

    /// <summary>
    /// IQ packet with a version query element inside.
    /// </summary>
    [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class VCardIQ : jabber.protocol.client.IQ
    {
        /// <summary>
        /// Create a vCard IQ
        /// </summary>
        /// <param name="doc"></param>
        public VCardIQ(XmlDocument doc) : base(doc)
        {
            AddChild(new VCard(doc));
        }

        /// <summary>
        /// returns the vCard element for this iq.
        /// </summary>
        public VCard VCard
        {
            get { return (VCard)this["vCard"]; }
        }
    }

    /// <summary>
    /// A vCard element.
    /// </summary>
    [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public class VCard : Element
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="doc"></param>
        public VCard(XmlDocument doc) : base("vCard", URI.VCARD, doc)
        {
        //  SetElem("PRODID", "jabber-net: " + this.GetType().Assembly.FullName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="qname"></param>
        /// <param name="doc"></param>
        public VCard(string prefix, XmlQualifiedName qname, XmlDocument doc) :
            base(prefix, qname, doc)
        {
        }

        /// <summary>
        /// Full name of the individual, as a single string
        /// </summary>
        public string FullName
        {
            get { return GetElem("FN"); }
            set { SetElem("FN", value); }
        }

        /// <summary>
        /// Pieces of the name, split apart
        /// </summary>
        public VName ComplexName
        {
            get { return this["N"] as VName; }
            set { ReplaceChild(value); }
        }

        /// <summary>
        /// Person's nick name.  This might be a good choice for a default roster nick,
        /// for instance.
        /// </summary>
        public string Nickname
        {
            get { return GetElem("NICKNAME"); }
            set { SetElem("NICKNAME", value); }
        }

        /// <summary>
        /// User's photograph
        /// </summary>
        public VPhoto Photo
        {
            get { return this["PHOTO"] as VPhoto; }
            set { ReplaceChild(value); }
        }

        /// <summary>
        /// Date of birth
        /// </summary>
        public DateTime Birthday
        {
            get { return DateTime.Parse(GetElem("BDAY")); }
            set { SetElem("BDAY", string.Format("yyyy-MM-dd", value)); }
        }

        /// <summary>
        /// Associated URL
        /// </summary>
        public System.Uri Url
        {
            get
            {
                string url = GetElem("URL");
                if ((url == null) || (url == ""))
                    return null;
                try
                {
                    Uri uri = new Uri(url);
                    return uri;
                }
                catch (UriFormatException)
                {
                    return null;
                }
            }
            set { SetElem("URL", value.ToString()); }
        }

        /// <summary>
        ///
        /// </summary>
        public VOrganization Organization
        {
            get { return this["ORG"] as VOrganization; }
            set { this.ReplaceChild(value); }
        }

        /// <summary>
        ///
        /// </summary>
        public string Title
        {
            get { return GetElem("TITLE"); }
            set { SetElem("TITLE", value); }
        }

        /// <summary>
        ///
        /// </summary>
        public string Role
        {
            get { return GetElem("ROLE"); }
            set { SetElem("ROLE", value); }
        }

        /// <summary>
        /// Jabber ID
        /// </summary>
        public JID JabberId
        {
            get { return GetElem("JABBERID"); }
            set { SetElem("JABBERID", value); }
        }

        /// <summary>
        ///
        /// </summary>
        public string Description
        {
            get { return GetElem("DESC"); }
            set { SetElem("DESC", value); }
        }

        /// <summary>
        /// List of telephone numbers
        /// </summary>
        /// <returns></returns>
        public VTelephone[] GetTelephoneList()
        {
            XmlNodeList nl = GetElementsByTagName("TEL", URI.VCARD);
            VTelephone[] numbers = new VTelephone[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                numbers[i] = (VTelephone) n;
                i++;
            }
            return numbers;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="type"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        public VTelephone GetTelephone(TelephoneType type, TelephoneLocation location)
        {
            foreach (VTelephone tel in GetTelephoneList())
            {
                if ((tel.Location == location) && (tel.Type == type))
                    return tel;
            }
            return null;
        }

        /// <summary>
        /// List of addresses
        /// </summary>
        /// <returns></returns>
        public VAddress[] GetAddressList()
        {
            XmlNodeList nl = GetElementsByTagName("ADR", URI.VCARD);
            VAddress[] addresses = new VAddress[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                addresses[i] = (VAddress) n;
                i++;
            }
            return addresses;
        }

        /// <summary>
        /// Get the address for the given location.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public VAddress GetAddress(AddressLocation location)
        {
            foreach (VAddress adr in GetAddressList())
            {
                if (adr.Location == location)
                    return adr;
            }
            return null;
        }

        /// <summary>
        /// List of Email addresses
        /// </summary>
        /// <returns></returns>
        public VEmail[] GetEmailList()
        {
            XmlNodeList nl = GetElementsByTagName("EMAIL", URI.VCARD);
            VEmail[] emails = new VEmail[nl.Count];
            int i=0;
            foreach (XmlNode n in nl)
            {
                emails[i] = (VEmail)n;
                i++;
            }
            return emails;
        }

        /// <summary>
        /// Get the email address for the given type.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public VEmail GetEmail(EmailType type)
        {
            foreach (VEmail email in GetEmailList())
            {
                if (email.Type == type)
                    return email;
            }
            return null;
        }

        /// <summary>
        ///  Sets the email address for the given type.
        /// </summary>
        /// <param name="email"></param>
        public void SetEmail(VEmail email)
        {
            VEmail existing = GetEmail(email.Type);
            if (existing == null)
            {
                AddChild(email);
            }
            else
            {
                existing.UserId = email.UserId;
            }
        }

        /// <summary>
        /// Get the internet email address (default)
        /// </summary>
        /// <returns></returns>
        public string Email
        {
            get
            {
                VEmail vemail = GetEmail(EmailType.internet);
                return vemail == null ? null : vemail.UserId;
            }
        }

        /// <summary>
        ///
        /// </summary>
        [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
        public class VName : Element
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="doc"></param>
            public VName(XmlDocument doc) : base("N", URI.VCARD, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="qname"></param>
            /// <param name="doc"></param>
            public VName(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                base(prefix, qname, doc)
            {
            }

            /// <summary>
            /// Given (first) name
            /// </summary>
            public string Given
            {
                get { return GetElem("GIVEN"); }
                set { SetElem("GIVEN", value); }
            }

            /// <summary>
            /// Family (last) name
            /// </summary>
            public string Family
            {
                get { return GetElem("FAMILY"); }
                set { SetElem("FAMILY", value); }
            }

            /// <summary>
            /// Middle name
            /// </summary>
            public string Middle
            {
                get { return GetElem("MIDDLE"); }
                set { SetElem("MIDDLE", value); }
            }
        }

        /// <summary>
        /// vCard Org Element
        /// </summary>
        [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
        public class VOrganization : Element
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="doc"></param>
            public VOrganization(XmlDocument doc) : base("ORG", URI.VCARD, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="qname"></param>
            /// <param name="doc"></param>
            public VOrganization(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                base(prefix, qname, doc)
            {
            }

            /// <summary>
            /// Orginization Name
            /// </summary>
            public string OrgName
            {
                get { return GetElem("ORGNAME"); }
                set { SetElem("ORGNAME", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public string Unit
            {
                get { return GetElem("ORGUNIT"); }
                set { SetElem("ORGUNIT", value); }
            }
        }

        /// <summary>
        /// vCard Telephone Element
        /// </summary>
        [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
        public class VTelephone : Element
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="doc"></param>
            public VTelephone(XmlDocument doc) : base("TEL", URI.VCARD, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="qname"></param>
            /// <param name="doc"></param>
            public VTelephone(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                base(prefix, qname, doc)
            {
            }

            /// <summary>
            /// Phone number
            /// </summary>
            public string Number
            {
                get { return GetElem("NUMBER"); }
                set { SetElem("NUMBER", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public TelephoneType Type
            {
                get
                {
                    if (this["VOICE"] != null) return TelephoneType.voice;
                    else if (this["FAX"] != null) return TelephoneType.fax;
                    else if (this["MSG"] != null) return TelephoneType.msg;
                    else return TelephoneType.unknown;
                }
                set
                {
                    RemoveElem("VOICE");
                    RemoveElem("FAX");
                    RemoveElem("MSG");

                    switch (value)
                    {
                        case TelephoneType.voice:
                            SetElem("VOICE", null);
                            break;
                        case TelephoneType.fax:
                            SetElem("FAX", null);
                            break;
                        case TelephoneType.msg:
                            SetElem("MSG", null);
                            break;
                    }
                }
            }

            /// <summary>
            ///
            /// </summary>
            public TelephoneLocation Location
            {
                get
                {
                    if (this["WORK"] != null) return TelephoneLocation.work;
                    else if (this["HOME"] != null) return TelephoneLocation.home;
                    else return TelephoneLocation.unknown;
                }
                set
                {
                    this.RemoveElem("WORK");
                    this.RemoveElem("HOME");

                    switch (value)
                    {
                        case TelephoneLocation.work:
                            SetElem("WORK", null);
                            break;
                        case TelephoneLocation.home:
                            SetElem("HOME", null);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// vCard Address Element
        /// </summary>
        [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
        public class VAddress : Element
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="doc"></param>
            public VAddress(XmlDocument doc) : base("ADR", URI.VCARD, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="qname"></param>
            /// <param name="doc"></param>
            public VAddress(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                base(prefix, qname, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            public string Street
            {
                get { return GetElem("STREET"); }
                set { SetElem("STREET", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public string Locality
            {
                get { return GetElem("LOCALITY"); }
                set { SetElem("LOCALITY", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public string Region
            {
                get { return GetElem("REGION"); }
                set { SetElem("REGION", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public string PostalCode
            {
                get { return GetElem("PCODE"); }
                set { SetElem("PCODE", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public string Country
            {
                get { return GetElem("CTRY"); }
                set { SetElem("CTRY", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public string Extra
            {
                get { return GetElem("EXTADD"); }
                set { SetElem("EXTADD", value); }
            }

            /// <summary>
            ///
            /// </summary>
            public AddressLocation Location
            {
                get
                {
                    if (this["WORK"] != null) return AddressLocation.work;
                    else if (this["HOME"] != null) return AddressLocation.home;
                    else return AddressLocation.unknown;
                }
                set
                {
                    this.RemoveElem("WORK");
                    this.RemoveElem("HOME");

                    switch (value)
                    {
                        case AddressLocation.work:
                            SetElem("WORK", null);
                            break;
                        case AddressLocation.home:
                            SetElem("HOME", null);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// vCard Email Element
        /// </summary>
        [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
        public class VEmail : Element
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="doc"></param>
            public VEmail(XmlDocument doc) : base("EMAIL", URI.VCARD, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="qname"></param>
            /// <param name="doc"></param>
            public VEmail(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                base(prefix, qname, doc)
            {
            }

            /// <summary>
            /// The e-mail address
            /// </summary>
            public string UserId
            {
                get { return GetElem("USERID"); }
                set { SetElem("USERID", value); }
            }

            /// <summary>
            /// Is this the preferred e-mail address?
            /// </summary>
            public bool IsPreferred
            {
                get { return (this["PREF"] != null); }
                set
                {
                    if (value)
                        SetElem("PREF", null);
                    else
                        RemoveElem("PREF");
                }
            }

            /// <summary>
            /// What kind of address is this?
            /// </summary>
            public EmailType Type
            {
                get
                {
                    if (this["HOME"] != null) return EmailType.home;
                    else if (this["WORK"] != null) return EmailType.work;
                    else if (this["INTERNET"] != null) return EmailType.internet;
                    else if (this["X400"] != null) return EmailType.x400;
                    else return EmailType.NONE;
                }
                set
                {
                    RemoveElem("HOME");
                    RemoveElem("WORK");
                    RemoveElem("INTERNET");
                    RemoveElem("X400");

                    switch (value)
                    {
                        case EmailType.home:
                            SetElem("HOME", null);
                            break;
                        case EmailType.work:
                            SetElem("WORK", null);
                            break;
                        case EmailType.internet:
                            SetElem("INTERNET", null);
                            break;
                        case EmailType.x400:
                            SetElem("X400", null);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Geographic location
        /// </summary>
        [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
        public class VGeo : Element
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="doc"></param>
            public VGeo(XmlDocument doc) : base("GEO", URI.VCARD, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="qname"></param>
            /// <param name="doc"></param>
            public VGeo(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                base(prefix, qname, doc)
            {
            }

            /// <summary>
            /// Latitude
            /// </summary>
            public double Lat
            {
                get { return double.Parse(GetElem("LAT")); }
                set { SetElem("LAT", string.Format("{0:6f}", value)); }
            }

            /// <summary>
            /// Longitude
            /// </summary>
            public double Lon
            {
                get { return double.Parse(GetElem("LON")); }
                set { SetElem("LON", string.Format("{0:6f}", value)); }
            }
        }

        /// <summary>
        ///
        /// </summary>
        [SVN(@"$Id: VCard.cs 358 2007-03-31 18:45:33Z hildjj $")]
        public class VPhoto : Element
        {
            /// <summary>
            ///
            /// </summary>
            /// <param name="doc"></param>
            public VPhoto(XmlDocument doc) : base("PHOTO", URI.VCARD, doc)
            {
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="qname"></param>
            /// <param name="doc"></param>
            public VPhoto(string prefix, XmlQualifiedName qname, XmlDocument doc) :
                base(prefix, qname, doc)
            {
            }

            /*
            /// <summary>
            ///
            /// </summary>
            public System.Drawing.Image Bitmap
            {
                get
                {
                    XmlElement ext = this["EXTVAL"];
                    if (ext != null)
                    {
                        System.Net.WebRequest req = System.Net.WebRequest.Create(ext.InnerText);
                        System.Net.WebResponse resp = req.GetResponse();
                        return new System.Drawing.Bitmap(resp.GetResponseStream());
                    }
                    XmlElement binv = this["BINVAL"];
                    if (binv != null)
                    {

                    }
                    return null;
                }
            }
            */
        }

    }
}
