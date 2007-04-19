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
using System.Text;
using bedrock.util;

namespace bedrock.net
{
    /// <summary>
    /// Proxy object for sockets that want to do SOCKS proxying.
    /// </summary>
    [SVN(@"$Id: Socks5Proxy.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Socks5Proxy : ProxySocket
    {
        private enum States { None, Connecting, GettingMethods, WaitingForAuth, RequestingProxy, Running, Closed }
        private States m_state = States.None;

        /// <summary>
        /// Wrap an existing socket event listener with a Socks5 proxy.  Make SURE to set Socket after this.
        /// </summary>
        /// <param name="chain">Event listener to pass events through to.</param>
        public Socks5Proxy(ISocketEventListener chain) : base(chain)
        {
        }

        /// <summary>
        /// Saves the address passed in, and really connects to ProxyHost:ProxyPort to begin SOCKS5 handshake.
        /// </summary>
        /// <param name="addr"></param>
        public override void Connect(bedrock.net.Address addr)
        {
            m_state = States.Connecting;
            base.Connect(addr);
        }

        #region Socks5 private methods.

        /*
         * The SOCKS request is formed as follows:
         *
         *      +----+-----+-------+------+----------+----------+
         *      |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
         *      +----+-----+-------+------+----------+----------+
         *      | 1  |  1  | X'00' |  1   | Variable |    2     |
         *      +----+-----+-------+------+----------+----------+
         *
         *   Where:
         *
         *        o  VER    protocol version: X'05'
         *        o  CMD
         *           o  CONNECT X'01'
         *           o  BIND X'02'
         *           o  UDP ASSOCIATE X'03'
         *        o  RSV    RESERVED
         *        o  ATYP   address type of following address
         *           o  IP V4 address: X'01'
         *           o  DOMAINNAME: X'03'
         *           o  IP V6 address: X'04'
         *        o  DST.ADDR    desired destination address
         *        o  DST.PORT    desired destination port in network octet order
         */
        private void RequestProxyConnection()
        {
            m_state = States.RequestingProxy;

            byte[] host = Encoding.ASCII.GetBytes(RemoteAddress.Hostname);
            int n = host.Length;
            byte [] buffer = new Byte[7 + n];
            buffer[0] = 5; // protocol version.
            buffer[1] = 1; // connect
            buffer[2] = 0; // reserved.
            buffer[3] = 3; // DOMAINNAME
            buffer[4] = (byte)n;
            host.CopyTo(buffer, 5);
            buffer[5+n] = (byte)(RemoteAddress.Port >> 8);
            buffer[6+n] = (byte)RemoteAddress.Port;
            Debug.WriteLine("sending request to proxy to " + RemoteAddress);
            Write(buffer);
        }

        private bool HandleGetMethodsResponse(int ver, int method)
        {
            if (ver != 5)
            {
                Debug.WriteLine("bogus version  from proxy: " + ver);
                return false;
            }
            if (method == 0xff)
            {
                Debug.WriteLine("no valid method returned from proxy");
                return false;
            }

            Debug.WriteLine("proxy accepted our connection: " + method);
            switch (method)
            {
                case 2:
                    /*
                     * +----+------+----------+------+----------+
                     * |VER | ULEN |  UNAME   | PLEN |  PASSWD  |
                     * +----+------+----------+------+----------+
                     * | 1  |  1   | 1 to 255 |  1   | 1 to 255 |
                     * +----+------+----------+------+----------+
                     */
                    m_state = States.WaitingForAuth;
                    byte [] buffer = new Byte[3 + Username.Length + Password.Length];
                    buffer[0] = 1; // version of this subnegotiation.
                    buffer[1] = (byte)Username.Length;
                    Encoding.ASCII.GetBytes(Username, 0, Username.Length, buffer, 2);
                    int pw_offset = 2 + Username.Length;
                    buffer[pw_offset] = (byte)Password.Length;
                    Encoding.ASCII.GetBytes(Password, 0, Password.Length, buffer, pw_offset + 1);
                    Debug.WriteLine("sending plain auth to proxy");
                    Write(buffer);
                    return true;
                case 0:
                    RequestProxyConnection();
                    return true;
                default:
                    Debug.WriteLine("bogus auth method: " + method);
                    return false;
            }
        }

        private bool HandleAuthResponse(int ver, int status)
        {
            if (ver != 1)
            {
                Debug.WriteLine("bogus subnegotiation version from proxy: " + ver);
                return false;
            }
            if (status != 0)
            {
                Debug.WriteLine("username/password auth failed on proxy");
                return false;
            }

            Debug.WriteLine("proxy accepted our auth handshake");
            RequestProxyConnection();
            return true;
        }

        /*
         * +----+-----+-------+------+----------+----------+
         * |VER | REP |  RSV  | ATYP | BND.ADDR | BND.PORT |
         * +----+-----+-------+------+----------+----------+
         * | 1  |  1  | X'00' |  1   | Variable |    2     |
         * +----+-----+-------+------+----------+----------+
         *
         *     Where:
         *
         *           o  VER    protocol version: X'05'
         *           o  REP    Reply field:
         *              o  X'00' succeeded
         *              o  X'01' general SOCKS server failure
         *              o  X'02' connection not allowed by ruleset
         *              o  X'03' Network unreachable
         *              o  X'04' Host unreachable
         *              o  X'05' Connection refused
         *              o  X'06' TTL expired
         *              o  X'07' Command not supported
         *              o  X'08' Address type not supported
         *              o  X'09' to X'FF' unassigned
         */
        private bool HandleRequestResponse(int ver, int reply)
        {
            if (ver != 5)
            {
                Debug.WriteLine("bogus version in reply from proxy: " + ver);
                return false;
            }
            if (reply != 0)
            {
                Debug.WriteLine("request failed on proxy: " + reply);
                return false;
            }

            Debug.WriteLine("proxy complete");
            m_state = States.Running;
            return true;
        }

        #endregion

        #region Implementation of ISocketEventListener

        /// <summary>
        /// overridden OnConnect to start off Socks5 protocol.
        /// </summary>
        /// <param name="sock"></param>
        public override void OnConnect(bedrock.net.BaseSocket sock)
        {
            if (m_state == States.Connecting)
            {
                byte [] buffer = new Byte[4];
                buffer[0] = 5; // protocol version.
                buffer[1] = 2; // number of methods.
                buffer[2] = 0; // no auth.
                buffer[3] = 2; // username password.
                Debug.WriteLine("sending auth methods to proxy...");
                Write(buffer);
                RequestRead();
                m_state = States.GettingMethods;
            }
        }

        /// <summary>
        /// Overridden OnRead to handle 4 Socks5 states...
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public override bool OnRead(bedrock.net.BaseSocket sock, byte[] buf, int offset, int length)
        {
            switch (m_state)
            {
                case States.GettingMethods:
                    return HandleGetMethodsResponse(buf[offset], buf[offset + 1]);
                case States.WaitingForAuth:
                    return HandleAuthResponse(buf[offset], buf[offset + 1]);
                case States.RequestingProxy:
                    bool ret = HandleRequestResponse(buf[offset], buf[offset + 1]);
                    if (ret)
                    {
                        m_listener.OnConnect(sock); // tell the real listener that we're connected.
                        // they'll call RequestRead(), so we can return false here.
                    }
                    return false;
                default:
                    return base.OnRead(sock, buf, offset, length);
            }
        }

        /// <summary>
        /// Overridden OnWrite to ensure that the base only gets called when in running state.
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public override void OnWrite(bedrock.net.BaseSocket sock, byte[] buf, int offset, int length)
        {
            if (m_state == States.Running)
            {
                base.OnWrite(sock, buf, offset, length);
            }
        }
        #endregion
    }
}
