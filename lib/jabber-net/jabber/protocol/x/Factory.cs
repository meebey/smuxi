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

namespace jabber.protocol.x
{
    /// <summary>
    /// ElementFactory for all currently supported IQ namespaces.
    /// </summary>
    [SVN(@"$Id: Factory.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Factory : IPacketTypes
    {
        private static QnameType[] s_qnt = new QnameType[]
                {
                    new QnameType("x",     URI.XDELAY,    typeof(jabber.protocol.x.Delay)),
                    new QnameType("x",     URI.XEVENT,    typeof(jabber.protocol.x.Event)),
                    new QnameType("x",     URI.XOOB,      typeof(jabber.protocol.iq.OOB)),
                    new QnameType("x",     URI.XROSTER,   typeof(jabber.protocol.iq.Roster)),
                    new QnameType("item",  URI.XROSTER,   typeof(jabber.protocol.iq.Item)),
                    new QnameType("group", URI.XROSTER,   typeof(jabber.protocol.iq.Group)),

                    new QnameType("x",     URI.XDATA,     typeof(jabber.protocol.x.Data)),
                    new QnameType("field", URI.XDATA,     typeof(jabber.protocol.x.Field)),
                    new QnameType("option",URI.XDATA,     typeof(jabber.protocol.x.Option)),
        };
        QnameType[] IPacketTypes.Types { get { return s_qnt; } }
    }
}
