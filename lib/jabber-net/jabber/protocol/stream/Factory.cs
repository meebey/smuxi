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

namespace jabber.protocol.stream
{
    /// <summary>
    /// ElementFactory for http://etherx.jabber.org/streams
    /// </summary>
    [SVN(@"$Id: Factory.cs 339 2007-03-02 19:40:49Z hildjj $")]
    public class Factory : jabber.protocol.IPacketTypes
    {
        private static QnameType[] s_qnt = new QnameType[]
        {
            new QnameType("stream",     URI.STREAM,    typeof(Stream)),
            new QnameType("error",      URI.STREAM,    typeof(Error)),
            new QnameType("features",   URI.STREAM,    typeof(Features)),
            new QnameType("starttls",   URI.START_TLS, typeof(StartTLS)),
            new QnameType("proceed",    URI.START_TLS, typeof(Proceed)),
            new QnameType("failure",    URI.START_TLS, typeof(TLSFailure)),
            new QnameType("mechanisms", URI.SASL,      typeof(Mechanisms)),
            new QnameType("mechanism",  URI.SASL,      typeof(Mechanism)),
            new QnameType("auth",       URI.SASL,      typeof(Auth)),
            new QnameType("challenge",  URI.SASL,      typeof(Challenge)),
            new QnameType("response",   URI.SASL,      typeof(Response)),
            new QnameType("failure",    URI.SASL,      typeof(SASLFailure)),
            new QnameType("abort",      URI.SASL,      typeof(Abort)),
            new QnameType("success",    URI.SASL,      typeof(Success)),
            new QnameType("session",    URI.SESSION,   typeof(Session)),
            new QnameType("bind",       URI.BIND,      typeof(Bind)),
        };
        QnameType[] IPacketTypes.Types { get { return s_qnt; } }
    }
}
