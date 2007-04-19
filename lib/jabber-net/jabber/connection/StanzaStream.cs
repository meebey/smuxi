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
using System.Xml;

using bedrock.net;
using bedrock.util;
using jabber.protocol;

namespace jabber.connection
{
    /// <summary>
    /// How to connect?  Socket?  Polling?
    /// </summary>
    [SVN(@"$Id: StanzaStream.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public enum ConnectionType
    {
        /// <summary>
        /// "Normal" XMPP socket
        /// </summary>
        Socket,
        /// <summary>
        /// HTTP Polling, as in http://www.xmpp.org/extensions/xep-0025.html
        /// </summary>
        HTTP_Polling,
        /// <summary>
        /// HTTP Binding, as in http://www.xmpp.org/extensions/xep-0124.html
        /// </summary>
        HTTP_Binding
    }

    /// <summary>
    /// Listen for stanza and connection events
    /// </summary>
    [SVN(@"$Id: StanzaStream.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public interface IStanzaEventListener
    {
        /// <summary>
        /// Get or set properties on the listener.
        /// </summary>
        /// <param name="prop">Property name.  Look at the Options class for some ideas.</param>
        /// <returns></returns>
        object this[string prop]
        {
            get;
            set;
        }

        /// <summary>
        /// One of the properties has changed.
        /// </summary>
        event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// We are connected to the server.  Time to send stream:stream.
        /// </summary>
        void Connected();

        /// <summary>
        /// We accepted a new connection from the server.  Wait for a stream:stream.
        /// </summary>
        void Accepted();

        /// <summary>
        /// Text was read from the server.  Use for debugging only.
        /// Will NOT be complete nodes at a time.
        /// </summary>
        void BytesRead(byte[] buf, int offset, int len);

        /// <summary>
        /// Text was written to the server.  Use for debugging only.
        /// Will NOT be complete nodes at a time.
        /// </summary>
        void BytesWritten(byte[] buf, int offset, int len);

        /// <summary>
        /// A new stream was initialized.  Add your packet factories to it.
        /// </summary>
        void StreamInit(ElementStream stream);

        /// <summary>
        /// An error has occurred.
        /// </summary>
        /// <param name="e"></param>
        void Errored(Exception e);

        /// <summary>
        /// The stream has been closed.
        /// </summary>
        void Closed();

        /// <summary>
        /// Received a doc start tag.  This may be "synthetic" for some backends.
        /// </summary>
        /// <param name="elem"></param>
        void DocumentStarted(XmlElement elem);

        /// <summary>
        /// The closing stream:stream was received.  Probably mostly equivalent to Closed(), except
        /// if the stream is still open, you should close it at this point.
        /// May not be called for some backends.
        /// </summary>
        void DocumentEnded();

        /// <summary>
        /// We've gotten a full stanza, stream:features, etc.
        /// </summary>
        /// <param name="elem"></param>
        void StanzaReceived(XmlElement elem);
    }

    /// <summary>
    /// Base stream for reading and writing full stanzas.
    /// </summary>
    [SVN(@"$Id: StanzaStream.cs 358 2007-03-31 18:45:33Z hildjj $")]
    public abstract class StanzaStream
        {
        /// <summary>
        /// Text encoding.  Always UTF-8 for XMPP.
        /// </summary>
        protected readonly Encoding ENC = Encoding.UTF8;

        /// <summary>
        /// Where to fire events.
        /// </summary>
        protected IStanzaEventListener m_listener = null;

        /// <summary>
        /// Factory to create StanzaStream's.
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="listener"></param>
        /// <returns></returns>
        public static StanzaStream Create(ConnectionType kind, IStanzaEventListener listener)
        {
            switch (kind)
            {
            case ConnectionType.Socket:
                return new SocketStanzaStream(listener);
            case ConnectionType.HTTP_Polling:
                return new PollingStanzaStream(listener);
// TODO: Fix broken build.
//            case ConnectionType.HTTP_Binding:
//                return new BindingStanzaStream(listener);
            default:
                throw new NotImplementedException("Proxy type not implemented yet: " + kind.ToString());
            }
        }

        /// <summary>
        /// Create a new stanza stream.
        /// </summary>
        /// <param name="listener"></param>
        protected StanzaStream(IStanzaEventListener listener)
        {
            Debug.Assert(listener != null);
            m_listener = listener;
        }

        /// <summary>
        /// Start the ball rolling.
        /// </summary>
        abstract public void Connect();

        /// <summary>
        /// Listen for an inbound connection.  Only implemented by socket types for now.
        /// </summary>
        virtual public void Accept()
        {
            throw new NotImplementedException("Accept not implemented on this stream type");
        }

        /// <summary>
        /// Is it legal to call Accept() at the moment?
        /// </summary>
        virtual public bool Acceptable
        {
            get { return false; }
        }

        /// <summary>
        /// Handshake TLS now.
        /// </summary>
        virtual public void StartTLS()
        {
            throw new NotImplementedException("Start-TLS not implemented on this stream type");
        }

        /// <summary>
        /// New stream:stream.
        /// </summary>
        virtual public void InitializeStream()
        {
        }

        /// <summary>
        /// Write a stream:stream.  Some underlying implementations will ignore this,
        /// but may pull out pertinent data.
        /// </summary>
        /// <param name="stream"></param>
        abstract public void WriteStartTag(jabber.protocol.stream.Stream stream);

        /// <summary>
        /// Write an entire element.
        /// </summary>
        /// <param name="elem"></param>
        abstract public void Write(XmlElement elem);

        /// <summary>
        /// Write raw string.
        /// </summary>
        /// <param name="str"></param>
        abstract public void Write(string str);

        /// <summary>
        /// Close the stream.
        /// </summary>
        abstract public void Close(bool clean);

        /// <summary>
        /// Is the stream connected?  (for some loose value of "connected")
        /// </summary>
        abstract public bool Connected
        {
            get;
        }

        /// <summary>
        /// Does this stream support start-tls?
        /// </summary>
        virtual public bool SupportsTLS
        {
            get { return false; }
        }
        }

    /// <summary>
    /// Something happened on a StanzaStream.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="stream"></param>
    public delegate void StanzaStreamHandler(object sender, StanzaStream stream);
}
