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
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.Globalization;
using bedrock.util;
namespace bedrock.net
{
    /// <summary>
    /// Callback for async DNS lookups.
    /// </summary>
    public delegate void AddressResolved(Address addr);
    /// <summary>
    /// Encapsulation and caching of IP address information.  Very similar to System.Net.IPEndPoint,
    /// but adds async DNS lookups.
    /// TODO: add SRV?
    /// </summary>
    [SVN(@"$Id: Address.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Address
    {
        private string    m_hostname = null;
        private int       m_port     = -1;
        private IPAddress m_ip       = IPAddress.Any;
        /// <summary>
        /// Address for a server, corresponding to IPAddress.Any.
        /// </summary>
        /// <param name="port"></param>
        public Address(int port)
        {
            m_port = port;
        }
        /// <summary>
        /// New connection endpoint.
        /// </summary>
        /// <param name="hostname">Host name or dotted-quad IP address</param>
        /// <param name="port">Port number</param>
        public Address(string hostname, int port) : this(port)
        {
            Debug.Assert(hostname != null, "must supply a host name");
            this.Hostname = hostname;
        }
        /// <summary>
        /// Create a new connection endpoint, where the IP address is already known.
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public Address(IPAddress ip, int port) : this(port)
        {
            this.IP = ip;
        }
        /// <summary>
        /// The host name.  When set, checks for dotted-quad representation, to avoid
        /// async DNS call when possible.
        /// </summary>
        public string Hostname
        {
            get { return m_hostname; }
            set
            {
                if ((value == null) || (value == ""))
                {
                    m_hostname = null;
                    m_ip = IPAddress.Any;
                    return;
                }
                if (m_hostname != value)
                {
                    m_hostname = value;

                    try
                    {
                        m_ip = IPAddress.Parse(m_hostname);
                    }
                    catch (FormatException)
                    {
                        m_ip = null;
                    }
                }
            }
        }

        /// <summary>
        /// Port number.
        /// TODO: add string version that looks in /etc/services (or equiv)?
        /// </summary>
        public int Port
        {
            get { return m_port; }
            set
            {
                Debug.Assert(value > 0);
                m_port = value;
            }
        }
        /// <summary>
        /// The binary IP address.  Gives IPAddress.Any if resolution hasn't occured, and
        /// null if resolution failed.
        /// </summary>
        public IPAddress IP
        {
            get { return m_ip; }
            set
            {
                m_ip = value;
                m_hostname = m_ip.ToString();
            }
        }
        /// <summary>
        /// Not implemented yet.
        /// </summary>
        public string Service
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }
        /// <summary>
        /// An IPEndPoint for making socket connections with.
        /// </summary>
        public IPEndPoint Endpoint
        {
            get
            {
                if (m_ip == null)
                    return null;
                return new IPEndPoint(m_ip, Port);
            }
            set
            {
                m_ip = value.Address;
                Port = value.Port;
            }
        }
        /// <summary>
        /// Async DNS lookup.  IP will be null in callback on failure.  Callback will
        /// be called immediately if IP is already known (e.g. dotted-quad).
        /// </summary>
        /// <param name="callback">Called when resolution complete.</param>
        public void Resolve(AddressResolved callback)
        {
            if ((m_ip != null) && (m_ip != IPAddress.Any)
#if !OLD_CLR
                && (m_ip != IPAddress.IPv6Any)
#endif
                )
            {
                callback(this);
            }
            else
            {
// hm. this seems to work now, but I'm leaving the comments here for now,
// just in case.

// #if MONO
//                 Resolve();
//                 callback(this);
// #else
#if NET20
                Dns.BeginGetHostEntry(m_hostname, new AsyncCallback(OnResolved), callback);
#else
                Dns.BeginResolve(m_hostname, new AsyncCallback(OnResolved), callback);
#endif
            }
        }
        /// <summary>
        /// Synchronous DNS lookup.
        /// </summary>
        public void Resolve()
        {
            if ((m_ip != null) && (m_ip != IPAddress.Any)
#if !OLD_CLR
                && (m_ip != IPAddress.IPv6Any)
#endif
                )
            {
                return;
            }
            Debug.Assert(m_hostname != null, "Must set hostname first");
#if NET20
            IPHostEntry iph = Dns.GetHostEntry(m_hostname);
#else
            IPHostEntry iph = Dns.Resolve(m_hostname);
#endif
            // TODO: what happens here on error?
            m_ip = iph.AddressList[0];
        }
        /// <summary>
        /// Handle the async DNS response.
        /// </summary>
        /// <param name="ar"></param>
        private void OnResolved(IAsyncResult ar)
        {
            try
            {
#if NET20
                IPHostEntry ent = Dns.EndGetHostEntry(ar);
#else
                IPHostEntry ent = Dns.EndResolve(ar);
#endif
                if (ent.AddressList.Length <= 0)
                {
                    m_ip = null;
                }
                else
                {
                    // From docs:
                    // When hostName is a DNS-style host name associated with multiple IP addresses,
                    // only the first IP address that resolves to that host name is returned.
                    m_ip = ent.AddressList[0];
                }
            }
            catch (SocketException e)
            {
                Debug.WriteLine(e.ToString());
                m_ip = null;
            }
            AddressResolved callback = (AddressResolved) ar.AsyncState;
            if (callback != null)
                callback(this);
        }
        /// <summary>
        /// Readable representation of the address.
        /// Host (IP):port
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}({1}):{2}", m_hostname, m_ip, m_port);
        }
    }
}
