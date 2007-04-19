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


using bedrock.util;
using jabber.protocol;

namespace jabber.protocol.accept
{
    /// <summary>
    /// A packet factory for the jabber:component:accept namespace.
    /// </summary>
    [SVN(@"$Id: Factory.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Factory : IPacketTypes
    {
        private static QnameType[] s_qnt = new QnameType[]
        {
            new QnameType("handshake", URI.ACCEPT, typeof(Handshake)),
            new QnameType("route",     URI.ACCEPT, typeof(Route)),
            new QnameType("xdb",       URI.ACCEPT, typeof(Xdb)),
            new QnameType("log",       URI.ACCEPT, typeof(Log)),
            new QnameType("handshake", URI.CONNECT, typeof(Handshake)),
            new QnameType("route",     URI.CONNECT, typeof(Route)),
            new QnameType("xdb",       URI.CONNECT, typeof(Xdb)),
            new QnameType("log",       URI.CONNECT, typeof(Log))
        };
        QnameType[] IPacketTypes.Types { get { return s_qnt; } }
    }
}
